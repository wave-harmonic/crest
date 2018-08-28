// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Captures waves/shape that is drawn kinematically - there is no frame-to-frame state. The gerstner
    /// waves are drawn in this way. There are two special features of this particular LodData.
    /// 
    ///  * A combine pass is done which combines downwards from low detail lods down into the high detail lods (see OceanScheduler).
    ///  * The textures from this LodData are passed to the ocean material when the surface is drawn (by OceanChunkRenderer).
    ///  * LodDataDynamicWaves adds its results into this LodData. The dynamic waves piggy back off the combine
    ///    pass and subsequent assignment to the ocean material (see OceanScheduler).
    ///  * The LodDataSeaFloorDepth sits on this same GameObject and borrows the camera. This could be a model for the other sim types..
    /// </summary>
    public class LodDataAnimatedWaves : LodData
    {
        public override SimType LodDataType { get { return SimType.AnimatedWaves; } }
        public override SimSettingsBase CreateDefaultSettings() { return null; }
        public override void UseSettings(SimSettingsBase settings) {}
        // shape format. i tried RGB111110Float but error becomes visible. one option would be to use a UNORM setup.
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.ARGBHalf; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Color; } }
        public override RenderTexture DataTexture { get { return Cam.targetTexture; } }

        [Tooltip("Read shape textures back to the CPU for collision purposes.")]
        public bool _readbackShapeForCollision = true;

        /// <summary>
        /// Turn shape combine pass on/off. Debug only - idef'd out in standalone
        /// </summary>
        public static bool _shapeCombinePass = true;

        CommandBuffer _bufCombineShapes = null;
        CameraEvent _combineEvent = 0;
        Camera _combineCamera = null;
        Material _combineMaterial;
        Material CombineMaterial { get { return _combineMaterial ?? (_combineMaterial = new Material(Shader.Find("Ocean/Shape/Combine"))); } }

        protected override void Start()
        {
            base.Start();

            _dataReadback = GetComponent<ReadbackLodData>();
        }
        public void HookCombinePass(Camera camera, CameraEvent onEvent)
        {
            _combineCamera = camera;
            _combineEvent = onEvent;

            if (_bufCombineShapes == null)
            {
                _bufCombineShapes = new CommandBuffer();
                _bufCombineShapes.name = "Combine Displacements";

                var cams = OceanRenderer.Instance.Builder._camsAnimWaves;
                for (int L = cams.Length - 2; L >= 0; L--)
                {
                    // accumulate shape data down the LOD chain - combine L+1 into L
                    var mat = OceanRenderer.Instance.Builder._lodDataAnimWaves[L].CombineMaterial;
                    _bufCombineShapes.Blit(cams[L + 1].targetTexture, cams[L].targetTexture, mat);
                }
            }

            _combineCamera.AddCommandBuffer(_combineEvent, _bufCombineShapes);
        }

        public void UnhookCombinePass()
        {
            if (_bufCombineShapes != null)
            {
                _combineCamera.RemoveCommandBuffer(_combineEvent, _bufCombineShapes);
                _bufCombineShapes = null;
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (_dataReadback != null)
            {
                _dataReadback._active = _readbackShapeForCollision;
            }

            // shape combine pass done by last shape camera - lod 0
            if (LodTransform.LodIndex == 0)
            {
                if (_bufCombineShapes != null && !_shapeCombinePass)
                {
                    UnhookCombinePass();
                }
                else if (_bufCombineShapes == null && _shapeCombinePass)
                {
                    HookCombinePass(_combineCamera, _combineEvent);
                }
            }
        }
#endif

        public float MaxWavelength()
        {
            float oceanBaseScale = OceanRenderer.Instance.transform.lossyScale.x;
            float maxDiameter = 4f * oceanBaseScale * Mathf.Pow(2f, LodTransform.LodIndex);
            float maxTexelSize = maxDiameter / (4f * OceanRenderer.Instance._baseVertDensity);
            return 2f * maxTexelSize * OceanRenderer.Instance._minTexelsPerWave;
        }

        // script execution order ensures this runs after ocean has been placed
        protected override void LateUpdate()
        {
            base.LateUpdate();

            LateUpdateShapeCombinePassSettings();
        }

        // apply this camera's properties to the shape combine materials
        void LateUpdateShapeCombinePassSettings()
        {
            BindResultData(0, CombineMaterial);

            if (LodTransform.LodIndex > 0)
            {
                var ldaws = OceanRenderer.Instance.Builder._lodDataAnimWaves;
                BindResultData(1, ldaws[LodTransform.LodIndex - 1].CombineMaterial);
            }
        }

        void OnDisable()
        {
            UnhookCombinePass();

            // free native array when component removed or destroyed
            if (_dataReadback._dataNative.IsCreated)
            {
                _dataReadback._dataNative.Dispose();
            }
        }

        protected override void BindData(int shapeSlot, IPropertyWrapper properties, Texture applyData, bool blendOut, ref LodTransform.RenderData renderData)
        {
            base.BindData(shapeSlot, properties, applyData, blendOut, ref renderData);

            // need to blend out shape if this is the largest lod, and the ocean might get scaled down later (so the largest lod will disappear)
            bool needToBlendOutShape = LodTransform.LodIndex == LodTransform.LodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease && blendOut;
            float shapeWeight = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            properties.SetVector(_paramsOceanParams[shapeSlot], 
                new Vector4(LodTransform._renderData._texelWidth, LodTransform._renderData._textureRes, shapeWeight, 1f / LodTransform._renderData._textureRes));
        }

        public bool SampleDisplacement(ref Vector3 worldPos, ref Vector3 displacement)
        {
            float xOffset = worldPos.x - _dataReadback._dataRenderData._posSnapped.x;
            float zOffset = worldPos.z - _dataReadback._dataRenderData._posSnapped.z;
            float r = _dataReadback._dataRenderData._texelWidth * _dataReadback._dataRenderData._textureRes / 2f;
            if (Mathf.Abs(xOffset) >= r || Mathf.Abs(zOffset) >= r)
            {
                // outside of this collision data
                return false;
            }

            var u = 0.5f + 0.5f * xOffset / r;
            var v = 0.5f + 0.5f * zOffset / r;
            var x = Mathf.FloorToInt(u * _dataReadback._dataRenderData._textureRes);
            var y = Mathf.FloorToInt(v * _dataReadback._dataRenderData._textureRes);
            var idx = 4 * (y * (int)_dataReadback._dataRenderData._textureRes + x);

            displacement.x = Mathf.HalfToFloat(_dataReadback._dataNative[idx + 0]);
            displacement.y = Mathf.HalfToFloat(_dataReadback._dataNative[idx + 1]);
            displacement.z = Mathf.HalfToFloat(_dataReadback._dataNative[idx + 2]);

            return true;
        }

        /// <summary>
        /// Get position on ocean plane that displaces horizontally to the given position.
        /// </summary>
        public Vector3 GetPositionDisplacedToPosition(ref Vector3 displacedWorldPos)
        {
            // fixed point iteration - guess should converge to location that displaces to the target position

            var guess = displacedWorldPos;

            // 2 iterations was enough to get very close when chop = 1, added 2 more which should be
            // sufficient for most applications. for high chop values or really stormy conditions there may
            // be some error here. one could also terminate iteration based on the size of the error, this is
            // worth trying but is left as future work for now.
            for (int i = 0; i < 4; i++)
            {
                var disp = Vector3.zero;
                SampleDisplacement(ref guess, ref disp);
                var error = guess + disp - displacedWorldPos;
                guess.x -= error.x;
                guess.z -= error.z;
            }
            guess.y = OceanRenderer.Instance.SeaLevel;
            return guess;
        }

        public float GetHeight(ref Vector3 worldPos)
        {
            var posFlatland = worldPos;
            posFlatland.y = OceanRenderer.Instance.transform.position.y;

            var undisplacedPos = GetPositionDisplacedToPosition(ref posFlatland);

            var disp = Vector3.zero;
            SampleDisplacement(ref undisplacedPos, ref disp);

            return posFlatland.y + disp.y;
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
            var ldaws = OceanRenderer.Instance.Builder._lodDataAnimWaves;
            for (int lod = 0; lod < ldaws.Length; lod++)
            {
                // shape texture needs to completely contain sample area
                var ldaw = ldaws[lod];
                if (ldaw.DataReadback == null) return -1;
                var wdcRect = ldaw.DataReadback.DataRectXZ;
                // shrink rect by 1 texel border - this is to make finite differences fit as well
                wdcRect.x += ldaw.LodTransform._renderData._texelWidth; wdcRect.y += ldaw.LodTransform._renderData._texelWidth;
                wdcRect.width -= 2f * ldaw.LodTransform._renderData._texelWidth; wdcRect.height -= 2f * ldaw.LodTransform._renderData._texelWidth;
                if (!wdcRect.Contains(sampleAreaXZ.min) || !wdcRect.Contains(sampleAreaXZ.max))
                    continue;

                // the smallest wavelengths should repeat no more than twice across the smaller spatial length
                var minWL = ldaw.MaxWavelength() / 2f;
                if (minWL < minSpatialLength / 2f)
                    continue;

                return lod;
            }

            return -1;
        }

        ReadbackLodData _dataReadback; public ReadbackLodData DataReadback { get { return _dataReadback; } }
    }
}
