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

        [Tooltip("Read shape textures back to the CPU for collision purposes.")]
        public bool _readbackShapeForCollision = true;

        /// <summary>
        /// Turn shape combine pass on/off. Debug only - ifdef'd out in standalone
        /// </summary>
        public static bool _shapeCombinePass = true;

        List<ShapeGerstnerBatched> _gerstnerComponents = new List<ShapeGerstnerBatched>();

        RenderTexture[] _waveBuffers;

        Material _combineMaterial;
        PropertyWrapperMPB[] _combineProperties;

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

            _combineMaterial = new Material(Shader.Find("Hidden/Crest/Simulation/Combine Animated Wave LODs"));
            _combineProperties = new PropertyWrapperMPB[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _combineProperties.Length; i++)
            {
                _combineProperties[i] = new PropertyWrapperMPB();
            }

            Debug.Assert(SystemInfo.SupportsRenderTextureFormat(TextureFormat), "The graphics device does not support the render texture format " + TextureFormat.ToString());

            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormat, 0);

            _waveBuffers = new RenderTexture[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _waveBuffers.Length; i++)
            {
                _waveBuffers[i] = new RenderTexture(desc);
                _waveBuffers[i].wrapMode = TextureWrapMode.Clamp;
                _waveBuffers[i].antiAliasing = 1;
                _waveBuffers[i].filterMode = FilterMode.Bilinear;
                _waveBuffers[i].anisoLevel = 0;
                _waveBuffers[i].useMipMap = false;
                _waveBuffers[i].name = "WaveBuffer_" + i + "_1";
            }
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
                buf.SetRenderTarget(_waveBuffers[lodIdx]);
                buf.ClearRenderTarget(false, true, new Color(0f, 0f, 0f, 0f));

                foreach (var gerstner in _gerstnerComponents)
                {
                    gerstner.BuildCommandBuffer(lodIdx, ocean, buf);
                }

                // draw any data with lod preference
                _filterWavelength._lodIdx = lodIdx;
                _filterWavelength._lodMaxWavelength = OceanRenderer.Instance._lods[lodIdx].MaxWavelength();
                _filterWavelength._lodMinWavelength = _filterWavelength._lodMaxWavelength / 2f;
                SubmitDrawsFiltered(lodIdx, buf, _filterWavelength);
            }

            // combine waves
            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                // this lod data
                BindWaveBuffer(lodIdx, 0, _combineProperties[lodIdx], false);

                // combine data from next larger lod into this one
                if (lodIdx < lodCount - 1 && _shapeCombinePass)
                {
                    BindResultData(lodIdx + 1, 1, _combineProperties[lodIdx]);
                }
                else
                {
                    // this binds black texture
                    BindWaveBuffer(lodIdx, 1, _combineProperties[lodIdx], true);
                }

                // dynamic waves
                if (OceanRenderer.Instance._lodDataDynWaves)
                {
                    OceanRenderer.Instance._lodDataDynWaves.BindCopySettings(_combineProperties[lodIdx]);
                    OceanRenderer.Instance._lodDataDynWaves.BindResultData(lodIdx, 0, _combineProperties[lodIdx]);
                }
                else
                {
                    LodDataMgrDynWaves.BindNull(0, _combineProperties[lodIdx]);
                }

                // flow
                if (OceanRenderer.Instance._lodDataFlow)
                {
                    OceanRenderer.Instance._lodDataFlow.BindResultData(lodIdx, 0, _combineProperties[lodIdx]);
                }
                else
                {
                    LodDataMgrFlow.BindNull(0, _combineProperties[lodIdx]);
                }

                buf.SetRenderTarget(DataTexture(lodIdx));
                buf.DrawProcedural(Matrix4x4.identity, _combineMaterial, 0, MeshTopology.Triangles, 3, 1, _combineProperties[lodIdx].materialPropertyBlock);
            }

            // lod-independent data
            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(DataTexture(lodIdx));

                // draw any data that did not express a preference for one lod or another
                SubmitDrawsFiltered(lodIdx, buf, _filterNoLodPreference);
            }
        }

        public void BindWaveBuffer(int lodIdx, int shapeSlot, IPropertyWrapper properties, bool paramsOnly)
        {
            var rd = OceanRenderer.Instance._lods[lodIdx]._renderData.Validate(0, this);
            BindData(lodIdx, shapeSlot, properties, paramsOnly ? Texture2D.blackTexture : (Texture)_waveBuffers[lodIdx], true, ref rd);
        }

        protected override void BindData(int lodIdx, int shapeSlot, IPropertyWrapper properties, Texture applyData, bool blendOut, ref LodTransform.RenderData renderData)
        {
            base.BindData(lodIdx, shapeSlot, properties, applyData, blendOut, ref renderData);

            var lt = OceanRenderer.Instance._lods[lodIdx];

            // need to blend out shape if this is the largest lod, and the ocean might get scaled down later (so the largest lod will disappear)
            bool needToBlendOutShape = lodIdx == OceanRenderer.Instance.CurrentLodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease && blendOut;
            float shapeWeight = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            properties.SetVector(LodTransform.ParamIdOcean(shapeSlot), new Vector4(
                lt._renderData._texelWidth,
                lt._renderData._textureRes, shapeWeight,
                1f / lt._renderData._textureRes));
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
            for (int lod = 0; lod < lodCount; lod++)
            {
                var lt = OceanRenderer.Instance._lods[lod];

                // Shape texture needs to completely contain sample area
                var lodRect = lt._renderData.RectXZ;
                // Shrink rect by 1 texel border - this is to make finite differences fit as well
                lodRect.x += lt._renderData._texelWidth; lodRect.y += lt._renderData._texelWidth;
                lodRect.width -= 2f * lt._renderData._texelWidth; lodRect.height -= 2f * lt._renderData._texelWidth;
                if (!lodRect.Contains(sampleAreaXZ.min) || !lodRect.Contains(sampleAreaXZ.max))
                    continue;

                // The smallest wavelengths should repeat no more than twice across the smaller spatial length. Unless we're
                // in the last LOD - then this is the best we can do.
                var minWL = OceanRenderer.Instance._lods[lod].MaxWavelength() / 2f;
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

        static int[] _paramsSampler;
        public static int ParamIdSampler(int slot)
        {
            if (_paramsSampler == null)
                LodTransform.CreateParamIDs(ref _paramsSampler, "_LD_Sampler_AnimatedWaves_");
            return _paramsSampler[slot];
        }
        protected override int GetParamIdSampler(int slot)
        {
            return ParamIdSampler(slot);
        }
        public static void BindNull(int shapeSlot, IPropertyWrapper properties)
        {
            properties.SetTexture(ParamIdSampler(shapeSlot), Texture2D.blackTexture);
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
