// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Captures waves/shape that is drawn kinematically - there is no frame-to-frame state. The Gerstner
    /// waves are drawn in this way. There are two special features of this particular LodData.
    ///
    ///  * A combine pass is done which combines downwards from low detail LODs down into the high detail LODs (see OceanScheduler).
    ///  * The textures from this LodData are passed to the ocean material when the surface is drawn (by OceanChunkRenderer).
    ///  * LodDataDynamicWaves adds its results into this LodData. The dynamic waves piggy back off the combine
    ///    pass and subsequent assignment to the ocean material (see OceanScheduler).
    ///  * The LodDataSeaFloorDepth sits on this same GameObject and borrows the camera. This could be a model for the other sim types..
    /// </summary>
    public class LodDataMgrAnimWaves : LodDataMgr, IFloatingOrigin
    {
        public override string SimName { get { return "AnimatedWaves"; } }
        // shape format. i tried RGB111110Float but error becomes visible. one option would be to use a UNORM setup.
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.ARGBHalf; } }
        protected override bool NeedToReadWriteTextureData { get { return true; } }

        [Tooltip("Read shape textures back to the CPU for collision purposes.")]
        public bool _readbackShapeForCollision = true;

        /// <summary>
        /// Turn shape combine pass on/off. Debug only - ifdef'd out in standalone
        /// </summary>
        public static bool _shapeCombinePass = true;

        List<ShapeGerstnerBatched> _gerstnerComponents = new List<ShapeGerstnerBatched>();

        RenderTexture _waveBuffers;

        const string ShaderName = "ShapeCombine";

        static int krnl_ShapeCombine = -1;
        static int krnl_ShapeCombine_DISABLE_COMBINE = -1;
        static int krnl_ShapeCombine_FLOW_ON = -1;
        static int krnl_ShapeCombine_FLOW_ON_DISABLE_COMBINE = -1;
        static int krnl_ShapeCombine_DYNAMIC_WAVE_SIM_ON = -1;
        static int krnl_ShapeCombine_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE = -1;
        static int krnl_ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON = -1;
        static int krnl_ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE = -1;

        ComputeShader _combineShader;
        PropertyWrapperCompute _combineProperties;

        static int sp_LD_TexArray_AnimatedWaves_Compute = Shader.PropertyToID("_LD_TexArray_AnimatedWaves_Compute");

        public override void UseSettings(SimSettingsBase settings) { OceanRenderer.Instance._simSettingsAnimatedWaves = settings as SimSettingsAnimatedWaves; }
        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsAnimatedWaves>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        protected override void InitData()
        {
            base.InitData();

            // Setup the RenderTexture and compute shader for combining
            // different animated wave LODs. As we use a single texture array
            // for all LODs, we employ a compute shader as only they can
            // read and write to the same texture.
            _combineShader = Resources.Load<ComputeShader>(ShaderName);
            krnl_ShapeCombine = _combineShader.FindKernel("ShapeCombine");
            krnl_ShapeCombine_DISABLE_COMBINE = _combineShader.FindKernel("ShapeCombine_DISABLE_COMBINE");
            krnl_ShapeCombine_FLOW_ON = _combineShader.FindKernel("ShapeCombine_FLOW_ON");
            krnl_ShapeCombine_FLOW_ON_DISABLE_COMBINE = _combineShader.FindKernel("ShapeCombine_FLOW_ON_DISABLE_COMBINE");
            krnl_ShapeCombine_DYNAMIC_WAVE_SIM_ON = _combineShader.FindKernel("ShapeCombine_DYNAMIC_WAVE_SIM_ON");
            krnl_ShapeCombine_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE = _combineShader.FindKernel("ShapeCombine_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE");
            krnl_ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON = _combineShader.FindKernel("ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON");
            krnl_ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE = _combineShader.FindKernel("ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE");
            _combineProperties = new PropertyWrapperCompute();

            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormat, 0);

            _waveBuffers = new RenderTexture(desc);
            _waveBuffers.wrapMode = TextureWrapMode.Clamp;
            _waveBuffers.antiAliasing = 1;
            _waveBuffers.filterMode = FilterMode.Bilinear;
            _waveBuffers.anisoLevel = 0;
            _waveBuffers.useMipMap = false;
            _waveBuffers.name = "WaveBuffer";
            _waveBuffers.dimension = TextureDimension.Tex2DArray;
            _waveBuffers.volumeDepth = OceanRenderer.Instance.CurrentLodCount;
            _waveBuffers.Create();
        }

        // Filter object for assigning shapes to LODs. This was much more elegant with a lambda but it generated garbage.
        public class FilterWavelength : IDrawFilter
        {
            public float _lodMinWavelength;
            public float _lodMaxWavelength;
            public int _lodIdx;
            public int _lodCount;

            public bool Filter(RegisterLodDataInputBase data)
            {
                var drawOctaveWavelength = (data as RegisterAnimWavesInput).OctaveWavelength;
                return (_lodMinWavelength <= drawOctaveWavelength) && (drawOctaveWavelength < _lodMaxWavelength || _lodIdx == _lodCount - 1);
            }
        }
        FilterWavelength _filterWavelength = new FilterWavelength();

        public class FilterNoLodPreference : IDrawFilter
        {
            public bool Filter(RegisterLodDataInputBase data)
            {
                return (data as RegisterAnimWavesInput).OctaveWavelength == 0f;
            }
        }
        FilterNoLodPreference _filterNoLodPreference = new FilterNoLodPreference();

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            var lodCount = OceanRenderer.Instance.CurrentLodCount;

            // lod-dependent data
            _filterWavelength._lodCount = lodCount;

            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(_waveBuffers, 0, CubemapFace.Unknown, lodIdx);
                buf.ClearRenderTarget(false, true, Color.black);

                foreach (var gerstner in _gerstnerComponents)
                {
                    gerstner.BuildCommandBuffer(lodIdx, ocean, buf);
                }

                // draw any data with lod preference
                _filterWavelength._lodIdx = lodIdx;
                _filterWavelength._lodMaxWavelength = OceanRenderer.Instance._lodTransform.MaxWavelength(lodIdx);
                _filterWavelength._lodMinWavelength = _filterWavelength._lodMaxWavelength / 2f;
                SubmitDrawsFiltered(lodIdx, buf, _filterWavelength);
            }

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

                BindWaveBuffer(_combineProperties);
                BindResultData(_combineProperties);

                // dynamic waves
                if (OceanRenderer.Instance._lodDataDynWaves)
                {
                    OceanRenderer.Instance._lodDataDynWaves.BindCopySettings(_combineProperties);
                    OceanRenderer.Instance._lodDataDynWaves.BindResultData(_combineProperties);
                }
                else
                {
                    LodDataMgrDynWaves.BindNull(_combineProperties);
                }

                // flow
                if (OceanRenderer.Instance._lodDataFlow)
                {
                    OceanRenderer.Instance._lodDataFlow.BindResultData(_combineProperties);
                }
                else
                {
                    LodDataMgrFlow.BindNull(_combineProperties);
                }

                // Set the animated waves texture where the results will be combined.
                _combineProperties.SetTexture(
                    sp_LD_TexArray_AnimatedWaves_Compute,
                    DataTexture
                );

                _combineProperties.SetFloat(OceanRenderer.sp_LD_SliceIndex, lodIdx);
                _combineProperties.DispatchShader();
            }

            // lod-independent data
            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(_targets, 0, CubemapFace.Unknown, lodIdx);

                // draw any data that did not express a preference for one lod or another
                SubmitDrawsFiltered(lodIdx, buf, _filterNoLodPreference);
            }
        }

        public void BindWaveBuffer(IPropertyWrapper properties, bool sourceLod = false)
        {
            var lt = OceanRenderer.Instance._lodTransform;
            for (int lodIdx = 0; lodIdx < OceanRenderer.Instance.CurrentLodCount; lodIdx++)
            {
                lt._renderData[lodIdx].Validate(0, this);
            }
            properties.SetTexture(Shader.PropertyToID("_LD_TexArray_WaveBuffer"), _waveBuffers);
            BindData(properties, null, true, ref lt._renderData, sourceLod);
        }

        protected override void BindData(IPropertyWrapper properties, Texture applyData, bool blendOut, ref LodTransform.RenderData[] renderData, bool sourceLod = false)
        {
            base.BindData(properties, applyData, blendOut, ref renderData, sourceLod);

            var lt = OceanRenderer.Instance._lodTransform;

            for (int lodIdx = 0; lodIdx < OceanRenderer.Instance.CurrentLodCount; lodIdx++)
            {
                // need to blend out shape if this is the largest lod, and the ocean might get scaled down later (so the largest lod will disappear)
                bool needToBlendOutShape = lodIdx == OceanRenderer.Instance.CurrentLodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease && blendOut;
                float shapeWeight = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
                _BindData_paramIdOceans[lodIdx] = new Vector4(
                    lt._renderData[lodIdx]._texelWidth,
                    lt._renderData[lodIdx]._textureRes, shapeWeight,
                    1f / lt._renderData[lodIdx]._textureRes);
            }
            properties.SetVectorArray(LodTransform.ParamIdOcean(sourceLod), _BindData_paramIdOceans);
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
                var lodRect = lt._renderData[lod].RectXZ;
                // Shrink rect by 1 texel border - this is to make finite differences fit as well
                lodRect.x += lt._renderData[lod]._texelWidth; lodRect.y += lt._renderData[lod]._texelWidth;
                lodRect.width -= 2f * lt._renderData[lod]._texelWidth; lodRect.height -= 2f * lt._renderData[lod]._texelWidth;
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

        public void AddGerstnerComponent(ShapeGerstnerBatched gerstner)
        {
            if (OceanRenderer.Instance == null)
            {
                // Ocean has unloaded, clear out
                _gerstnerComponents.Clear();
                return;
            }

            _gerstnerComponents.Add(gerstner);
        }

        public void RemoveGerstnerComponent(ShapeGerstnerBatched gerstner)
        {
            if (OceanRenderer.Instance == null)
            {
                // Ocean has unloaded, clear out
                _gerstnerComponents.Clear();
                return;
            }

            _gerstnerComponents.Remove(gerstner);
        }

        public static string TextureArrayName = "_LD_TexArray_AnimatedWaves";
        private static TextureArrayParamIds textureArrayParamIds = new TextureArrayParamIds(TextureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) { return textureArrayParamIds.GetId(sourceLod); }
        protected override int GetParamIdSampler(bool sourceLod = false)
        {
            return ParamIdSampler(sourceLod);
        }
        public static void BindNull(IPropertyWrapper properties, bool sourceLod = false)
        {
            properties.SetTexture(ParamIdSampler(sourceLod), TextureArrayHelpers.BlackTextureArray);
        }

        public void SetOrigin(Vector3 newOrigin)
        {
            foreach (var gerstner in _gerstnerComponents)
            {
                gerstner.SetOrigin(newOrigin);
            }
        }
    }
}
