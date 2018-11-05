// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

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
    public class LodDataMgrAnimWaves : LodDataMgr
    {
        public override LodData.SimType LodDataType { get { return LodData.SimType.AnimatedWaves; } }
        // shape format. i tried RGB111110Float but error becomes visible. one option would be to use a UNORM setup.
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.ARGBHalf; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Color; } }

        [Tooltip("Read shape textures back to the CPU for collision purposes.")]
        public bool _readbackShapeForCollision = true;

        /// <summary>
        /// Turn shape combine pass on/off. Debug only - ifdef'd out in standalone
        /// </summary>
        public static bool _shapeCombinePass = true;

        Material[] _combineMaterial;
        public Material GetMaterial(int lodIdx) { return _combineMaterial[lodIdx]; }

        [SerializeField] SimSettingsAnimatedWaves _settings;
        public override void UseSettings(SimSettingsBase settings) { _settings = settings as SimSettingsAnimatedWaves; }
        public SimSettingsAnimatedWaves Settings { get { return _settings as SimSettingsAnimatedWaves; } }
        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsAnimatedWaves>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        protected override void InitData()
        {
            base.InitData();

            _combineMaterial = new Material[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _combineMaterial.Length; i++)
            {
                _combineMaterial[i] = new Material(Shader.Find("Ocean/Shape/Combine"));
            }
        }

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            var lodCount = OceanRenderer.Instance.CurrentLodCount;

            // lod-dependent data
            var gerstner = FindObjectOfType<ShapeGerstnerBatched>();
            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(DataTexture(lodIdx));
                buf.ClearRenderTarget(false, true, Color.black);

                if (gerstner != null)
                {
                    gerstner.BuildCommandBuffer(lodIdx, ocean, buf);
                }
            }

            // combine pass
            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                BindResultData(lodIdx, 0, _combineMaterial[lodIdx]);

                if (lodIdx > 0)
                {
                    BindResultData(lodIdx - 1, 1, _combineMaterial[lodIdx - 1]);
                }
            }
            for (int lodIdx = lodCount - 2; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(DataTexture(lodIdx));

                // accumulate shape data down the LOD chain - combine L+1 into L
                var mat = _combineMaterial[lodIdx];
                buf.Blit(DataTexture(lodIdx + 1), DataTexture(lodIdx), mat);
            }

            // lod-independent data
            for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(DataTexture(lodIdx));

                SubmitDraws(lodIdx, buf);
            }
        }

        public float MaxWavelength(int lodIndex)
        {
            float oceanBaseScale = OceanRenderer.Instance.transform.lossyScale.x;
            float maxDiameter = 4f * oceanBaseScale * Mathf.Pow(2f, lodIndex);
            float maxTexelSize = maxDiameter / (4f * OceanRenderer.Instance._baseVertDensity);
            return 2f * maxTexelSize * OceanRenderer.Instance._minTexelsPerWave;
        }

        protected override void BindData(int lodIdx, int shapeSlot, IPropertyWrapper properties, Texture applyData, bool blendOut, ref LodTransform.RenderData renderData)
        {
            base.BindData(lodIdx, shapeSlot, properties, applyData, blendOut, ref renderData);

            var lt = OceanRenderer.Instance._lods[lodIdx];

            // need to blend out shape if this is the largest lod, and the ocean might get scaled down later (so the largest lod will disappear)
            bool needToBlendOutShape = lodIdx == OceanRenderer.Instance.CurrentLodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease && blendOut;
            float shapeWeight = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            properties.SetVector(_paramsOceanParams[shapeSlot], new Vector4(
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
                var minWL = OceanRenderer.Instance._lodDataAnimWaves.MaxWavelength(lod) / 2f;
                if (minWL < minSpatialLength / 2f && lod < lodCount - 1)
                    continue;

                return lod;
            }

            return -1;
        }
    }
}
