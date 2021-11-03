// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Crest
{
    using SettingsType = SimSettingsAnimatedWaves;

    /// <summary>
    /// Captures waves/shape that is drawn kinematically - there is no frame-to-frame state. The Gerstner
    /// waves are drawn in this way. There are two special features of this particular LodData.
    ///
    ///  * A combine pass is done which combines downwards from low detail LODs down into the high detail LODs (see OceanScheduler).
    ///  * The textures from this LodData are passed to the ocean material when the surface is drawn (by OceanChunkRenderer).
    ///  * LodDataDynamicWaves adds its results into this LodData. The dynamic waves piggy back off the combine
    ///    pass and subsequent assignment to the ocean material (see OceanScheduler).
    ///
    /// The RGB channels are the XYZ displacement from a rest plane at sea level to the corresponding displaced position on the
    /// surface. The A channel holds the variance/energy in all the smaller wavelengths that are too small to go into the cascade
    /// slice. This is used as a statistical measure for the missing waves and is used to ensure foam is generated everywhere.
    /// </summary>
    public class LodDataMgrAnimWaves : LodDataMgr
    {
        public override string SimName => "AnimatedWaves";
        // shape format. i tried RGB111110Float but error becomes visible. one option would be to use a UNORM setup.
        protected override GraphicsFormat RequestedTextureFormat => Settings._renderTextureGraphicsFormat;
        protected override bool NeedToReadWriteTextureData => true;
        public override int BufferCount => _bufferCount;

        [Tooltip("Read shape textures back to the CPU for collision purposes.")]
        public bool _readbackShapeForCollision = true;

        /// <summary>
        /// Turn shape combine pass on/off. Debug only - ifdef'd out in standalone
        /// </summary>
        public static bool _shapeCombinePass = true;

        RenderTexture _waveBuffers;
        RenderTexture _combineBuffer;

        const string ShaderName = "ShapeCombine";

        int krnl_ShapeCombine = -1;
        int krnl_ShapeCombine_DISABLE_COMBINE = -1;
        int krnl_ShapeCombine_FLOW_ON = -1;
        int krnl_ShapeCombine_FLOW_ON_DISABLE_COMBINE = -1;
        int krnl_ShapeCombine_DYNAMIC_WAVE_SIM_ON = -1;
        int krnl_ShapeCombine_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE = -1;
        int krnl_ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON = -1;
        int krnl_ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE = -1;

        ComputeShader _combineShader;
        PropertyWrapperCompute _combineProperties;
        PropertyWrapperMaterial[] _combineMaterial;

        readonly int sp_LD_TexArray_AnimatedWaves_Compute = Shader.PropertyToID("_LD_TexArray_AnimatedWaves_Compute");
        readonly int sp_LD_TexArray_WaveBuffer = Shader.PropertyToID("_LD_TexArray_WaveBuffer");
        public static readonly int sp_AttenuationInShallows = Shader.PropertyToID("_AttenuationInShallows");
        const string s_textureArrayName = "_LD_TexArray_AnimatedWaves";

        public interface IShapeUpdatable { void CrestUpdate(CommandBuffer buf); }
        static List<IShapeUpdatable> _updatables = new List<IShapeUpdatable>();
        public static void RegisterUpdatable(IShapeUpdatable updatable) => _updatables.Add(updatable);
        public static void DeregisterUpdatable(IShapeUpdatable updatable) => _updatables.RemoveAll(candidate => candidate == updatable);

        public override SimSettingsBase SettingsBase => Settings;
        public SettingsType Settings => _ocean._simSettingsAnimatedWaves != null ? _ocean._simSettingsAnimatedWaves : GetDefaultSettings<SettingsType>();

        readonly int _bufferCount = 1;

        public LodDataMgrAnimWaves(OceanRenderer ocean) : base(ocean)
        {
            _bufferCount = Helpers.IsMotionVectorsEnabled() ? 2 : 1;

            Start();
        }

        protected override void InitData()
        {
            base.InitData();

            // Setup the RenderTexture and compute shader for combining
            // different animated wave LODs. As we use a single texture array
            // for all LODs, we employ a compute shader as only they can
            // read and write to the same texture.

            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, CompatibleTextureFormat, 0);

            _waveBuffers = CreateLodDataTextures(desc, "WaveBuffer", false);

            _combineBuffer = CreateCombineBuffer(desc);

            // Combine graphics shader - for 'ping pong' approach (legacy hardware)
            var combineShaderNameGraphics = "Hidden/Crest/Simulation/Combine Animated Wave LODs";
            var combineShaderGraphics = Shader.Find(combineShaderNameGraphics);
            Debug.Assert(combineShaderGraphics != null,
                $"Could not load shader {combineShaderNameGraphics}. Try right clicking the Crest folder in the Project view and selecting Reimport, and checking for errors.",
                OceanRenderer.Instance);
            if (combineShaderGraphics != null)
            {
                _combineMaterial = new PropertyWrapperMaterial[OceanRenderer.Instance.CurrentLodCount];
                for (int i = 0; i < _combineMaterial.Length; i++)
                {
                    var mat = new Material(combineShaderGraphics);
                    _combineMaterial[i] = new PropertyWrapperMaterial(mat);
                }
            }

            // Combine compute shader - modern hardware
            _combineShader = ComputeShaderHelpers.LoadShader(ShaderName);
            if (_combineShader == null)
            {
                enabled = false;
                return;
            }
            krnl_ShapeCombine = _combineShader.FindKernel("ShapeCombine");
            krnl_ShapeCombine_DISABLE_COMBINE = _combineShader.FindKernel("ShapeCombine_DISABLE_COMBINE");
            krnl_ShapeCombine_FLOW_ON = _combineShader.FindKernel("ShapeCombine_FLOW_ON");
            krnl_ShapeCombine_FLOW_ON_DISABLE_COMBINE = _combineShader.FindKernel("ShapeCombine_FLOW_ON_DISABLE_COMBINE");
            krnl_ShapeCombine_DYNAMIC_WAVE_SIM_ON = _combineShader.FindKernel("ShapeCombine_DYNAMIC_WAVE_SIM_ON");
            krnl_ShapeCombine_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE = _combineShader.FindKernel("ShapeCombine_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE");
            krnl_ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON = _combineShader.FindKernel("ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON");
            krnl_ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE = _combineShader.FindKernel("ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE");
            _combineProperties = new PropertyWrapperCompute();
        }

        RenderTexture CreateCombineBuffer(RenderTextureDescriptor desc)
        {
            RenderTexture result = new RenderTexture(desc);
            result.wrapMode = TextureWrapMode.Clamp;
            result.antiAliasing = 1;
            result.filterMode = FilterMode.Bilinear;
            result.anisoLevel = 0;
            result.useMipMap = false;
            result.name = "CombineBuffer";
            result.dimension = TextureDimension.Tex2D;
            result.volumeDepth = 1;
            result.enableRandomWrite = false;
            result.Create();
            return result;
        }

        // Filter object for assigning shapes to LODs. This was much more elegant with a lambda but it generated garbage.
        public class FilterWavelength : IDrawFilter
        {
            public float _lodMinWavelength;
            public float _lodMaxWavelength;
            public int _lodIdx;
            public int _lodCount;
            public float _globalMaxWavelength;

            public float Filter(ILodDataInput data, out int isTransition)
            {
                var drawOctaveWavelength = data.Wavelength;
                isTransition = 0;

                // No wavelength preference - don't draw per-lod
                if (drawOctaveWavelength == 0f)
                {
                    return 0f;
                }

                // Too small for this lod
                if (drawOctaveWavelength < _lodMinWavelength)
                {
                    return 0f;
                }

                // If approaching end of lod chain, start smoothly transitioning any large wavelengths across last two lods
                if (drawOctaveWavelength >= _globalMaxWavelength / 2f)
                {
                    if (_lodIdx == _lodCount - 2)
                    {
                        isTransition = 1;
                        return 1f - OceanRenderer.Instance.ViewerAltitudeLevelAlpha;
                    }

                    if (_lodIdx == _lodCount - 1)
                    {
                        return OceanRenderer.Instance.ViewerAltitudeLevelAlpha;
                    }
                }
                else if (drawOctaveWavelength < _lodMaxWavelength)
                {
                    // Fits in this lod
                    return 1f;
                }

                return 0f;
            }
        }
        FilterWavelength _filterWavelength = new FilterWavelength();

        public class FilterNoLodPreference : IDrawFilter
        {
            public float Filter(ILodDataInput data, out int isTransition)
            {
                isTransition = 0;
                return data.Wavelength == 0f ? 1f : 0f;
            }
        }
        FilterNoLodPreference _filterNoLodPreference = new FilterNoLodPreference();

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            Shader.SetGlobalFloat(sp_AttenuationInShallows, Settings.AttenuationInShallows);

            var lodCount = OceanRenderer.Instance.CurrentLodCount;

            // Validation
            for (int lodIdx = 0; lodIdx < OceanRenderer.Instance.CurrentLodCount; lodIdx++)
            {
                OceanRenderer.Instance._lodTransform._renderData[lodIdx].Current.Validate(0, SimName);
            }

            foreach (var gerstner in _updatables)
            {
                gerstner.CrestUpdate(buf);
            }

            // lod-dependent data
            _filterWavelength._lodCount = lodCount;
            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(_waveBuffers, 0, CubemapFace.Unknown, lodIdx);
                buf.ClearRenderTarget(false, true, new Color(0f, 0f, 0f, 0f));

                // draw any data with lod preference
                _filterWavelength._lodIdx = lodIdx;
                _filterWavelength._lodMaxWavelength = OceanRenderer.Instance._lodTransform.MaxWavelength(lodIdx);
                _filterWavelength._lodMinWavelength = _filterWavelength._lodMaxWavelength / 2f;
                _filterWavelength._globalMaxWavelength = OceanRenderer.Instance._lodTransform.MaxWavelength(OceanRenderer.Instance.CurrentLodCount - 1);
                SubmitDrawsFiltered(lodIdx, buf, _filterWavelength);
            }

            // Combine the LODs - copy results from biggest LOD down to LOD 0
            if (Settings.PingPongCombinePass)
            {
                CombinePassPingPong(buf);
            }
            else
            {
                CombinePassCompute(buf);
            }

            // lod-independent data
            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(_targets.Current, 0, CubemapFace.Unknown, lodIdx);

                // draw any data that did not express a preference for one lod or another
                SubmitDrawsFiltered(lodIdx, buf, _filterNoLodPreference);
            }

            if (BufferCount > 1)
            {
                // Update current and previous. Latter for MVs and/or VFX.
                Shader.SetGlobalTexture(GetParamIdSampler(true), _targets.Previous(1));
                Shader.SetGlobalTexture(GetParamIdSampler(), _targets.Current);
            }
        }

        void CombinePassPingPong(CommandBuffer buf)
        {
            if (_combineMaterial == null) return;

            var lodCount = OceanRenderer.Instance.CurrentLodCount;
            const int shaderPassCombineIntoAux = 0, shaderPassCopyResultBack = 1;

            // combine waves
            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                // The per-octave wave buffers
                BindWaveBuffer(_combineMaterial[lodIdx]);

                // Bind this LOD data (displacements). Option to disable the combine pass - very useful debugging feature.
                if (_shapeCombinePass)
                {
                    Bind(_combineMaterial[lodIdx]);
                }
                else
                {
                    _combineMaterial[lodIdx].SetTexture(GetParamIdSampler(), TextureArrayHelpers.BlackTextureArray);
                }

                // Dynamic waves
                if (OceanRenderer.Instance._lodDataDynWaves != null)
                {
                    OceanRenderer.Instance._lodDataDynWaves.BindCopySettings(_combineMaterial[lodIdx]);
                }

                _combineMaterial[lodIdx].SetInt(sp_LD_SliceIndex, lodIdx);

                _combineMaterial[lodIdx].SetBuffer(OceanRenderer.sp_cascadeData, OceanRenderer.Instance._bufCascadeDataTgt);
                _combineMaterial[lodIdx].SetBuffer(OceanRenderer.sp_perCascadeInstanceData, OceanRenderer.Instance._bufPerCascadeInstanceData);

                // Combine this LOD's waves with waves from the LODs above into auxiliary combine buffer
                buf.SetRenderTarget(_combineBuffer);
                buf.DrawProcedural(Matrix4x4.identity, _combineMaterial[lodIdx].material, shaderPassCombineIntoAux, MeshTopology.Triangles, 3);

                // Copy combine buffer back to lod texture array
                buf.SetRenderTarget(_targets.Current, 0, CubemapFace.Unknown, lodIdx);
                _combineMaterial[lodIdx].SetTexture(Shader.PropertyToID("_CombineBuffer"), _combineBuffer);
                buf.DrawProcedural(Matrix4x4.identity, _combineMaterial[lodIdx].material, shaderPassCopyResultBack, MeshTopology.Triangles, 3);
            }
        }

        void CombinePassCompute(CommandBuffer buf)
        {
            var lodCount = OceanRenderer.Instance.CurrentLodCount;

            int combineShaderKernel = krnl_ShapeCombine;
            int combineShaderKernel_lastLOD = krnl_ShapeCombine_DISABLE_COMBINE;
            {
                bool isFlowOn = OceanRenderer.Instance._lodDataFlow != null;
                bool isDynWavesOn = OceanRenderer.Instance._lodDataDynWaves != null;
                // set the shader kernels that we will use.
                if (isFlowOn && isDynWavesOn)
                {
                    combineShaderKernel = krnl_ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON;
                    combineShaderKernel_lastLOD = krnl_ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE;
                }
                else if (isFlowOn)
                {
                    combineShaderKernel = krnl_ShapeCombine_FLOW_ON;
                    combineShaderKernel_lastLOD = krnl_ShapeCombine_FLOW_ON_DISABLE_COMBINE;
                }
                else if (isDynWavesOn)
                {
                    combineShaderKernel = krnl_ShapeCombine_DYNAMIC_WAVE_SIM_ON;
                    combineShaderKernel_lastLOD = krnl_ShapeCombine_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE;
                }
            }

            // combine waves
            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                int selectedShaderKernel;
                if (lodIdx < lodCount - 1 && _shapeCombinePass)
                {
                    selectedShaderKernel = combineShaderKernel;
                }
                else
                {
                    selectedShaderKernel = combineShaderKernel_lastLOD;
                }

                _combineProperties.Initialise(buf, _combineShader, selectedShaderKernel);

                // The per-octave wave buffers
                BindWaveBuffer(_combineProperties);
                // Bind this LOD data (displacements)
                Bind(_combineProperties);

                // Dynamic waves
                LodDataMgrDynWaves.Bind(_combineProperties);
                if (OceanRenderer.Instance._lodDataDynWaves != null)
                {
                    OceanRenderer.Instance._lodDataDynWaves.BindCopySettings(_combineProperties);
                }

                // Flow
                LodDataMgrFlow.Bind((_combineProperties));

                // Set the animated waves texture where the results will be combined.
                _combineProperties.SetTexture(
                    sp_LD_TexArray_AnimatedWaves_Compute,
                    DataTexture
                );

                _combineProperties.SetInt(sp_LD_SliceIndex, lodIdx);

                buf.DispatchCompute(_combineShader, selectedShaderKernel,
                    OceanRenderer.Instance.LodDataResolution / THREAD_GROUP_SIZE_X,
                    OceanRenderer.Instance.LodDataResolution / THREAD_GROUP_SIZE_Y,
                    1);
            }
        }

        public void BindWaveBuffer(IPropertyWrapper properties)
        {
            properties.SetTexture(sp_LD_TexArray_WaveBuffer, _waveBuffers);
        }

        /// <summary>
        /// Returns index of lod that completely covers the sample area, and contains wavelengths that repeat no more than twice across the smaller
        /// spatial length. If no such lod available, returns -1. This means high frequency wavelengths are filtered out, and the lod index can
        /// be used for each sample in the sample area.
        /// </summary>
        public static int SuggestDataLOD(Rect sampleAreaXZ)
        {
            return SuggestDataLOD(sampleAreaXZ, Mathf.Min(sampleAreaXZ.width, sampleAreaXZ.height));
        }
        public static int SuggestDataLOD(Rect sampleAreaXZ, float minSpatialLength)
        {
            var lodCount = OceanRenderer.Instance.CurrentLodCount;
            var lt = OceanRenderer.Instance._lodTransform;

            for (int lod = 0; lod < lodCount; lod++)
            {

                // Shape texture needs to completely contain sample area
                var lodRect = lt._renderData[lod].Current.RectXZ;
                // Shrink rect by 1 texel border - this is to make finite differences fit as well
                lodRect.x += lt._renderData[lod].Current._texelWidth; lodRect.y += lt._renderData[lod].Current._texelWidth;
                lodRect.width -= 2f * lt._renderData[lod].Current._texelWidth; lodRect.height -= 2f * lt._renderData[lod].Current._texelWidth;
                if (!lodRect.Contains(sampleAreaXZ.min) || !lodRect.Contains(sampleAreaXZ.max))
                    continue;

                // The smallest wavelengths should repeat no more than twice across the smaller spatial length. Unless we're
                // in the last LOD - then this is the best we can do.
                var minWL = lt.MaxWavelength(lod) / 2f;
                if (minWL < minSpatialLength / 2f && lod < lodCount - 1)
                    continue;

                return lod;
            }

            return -1;
        }

        private static TextureArrayParamIds s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) => s_textureArrayParamIds.GetId(sourceLod);
        protected override int GetParamIdSampler(bool sourceLod = false) => ParamIdSampler(sourceLod);

        public static void Bind(IPropertyWrapper properties)
        {
            if (OceanRenderer.Instance._lodDataAnimWaves != null)
            {
                properties.SetTexture(OceanRenderer.Instance._lodDataAnimWaves.GetParamIdSampler(), OceanRenderer.Instance._lodDataAnimWaves.DataTexture);
            }
            else
            {
                properties.SetTexture(ParamIdSampler(), TextureArrayHelpers.BlackTextureArray);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
            _updatables.Clear();
        }
    }
}
