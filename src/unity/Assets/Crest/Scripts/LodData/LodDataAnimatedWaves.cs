// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Captures waves/shape that is drawn kinematically - there is no frame-to-frame state. The gerstner
    /// waves are drawn in this way. There are two special features of this particular LodData:
    /// 
    ///  * A combine pass is done which combines downwards from low detail lods down into the high detail lods
    ///  * The textures from this LodData are passed to the ocean material when the surface is drawn
    ///  * Foam and Dynamic Waves LodDatas add their results into this LodData. The dynamic waves piggy back off the combine
    ///    pass. Both piggy back off the assignment to the ocean material.
    ///  * The LodDataSeaFloorDepth sits on this same GameObject and borrows the camera. This could be a model for the other sim types..
    /// </summary>
    public class LodDataAnimatedWaves : LodData
    {
        public override SimType LodDataType { get { return SimType.AnimatedWaves; } }
        public override SimSettingsBase CreateDefaultSettings() { return null; }
        public override void UseSettings(SimSettingsBase settings) {}
        // shape format. i tried RGB111110Float but error becomes visible. one option would be to use a UNORM setup.
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.ARGBHalf; } }
        public override int Depth { get { return -30; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Color; } }
        public override RenderTexture DataTexture { get { return Cam.targetTexture; } }

        // debug use
        public static bool _shapeCombinePass = true;

        Material _combineMaterial;
        CommandBuffer _bufCombineShapes = null;
        readonly CameraEvent _combineEvent = CameraEvent.AfterEverything;

        protected override void Start()
        {
            base.Start();

            _combineMaterial = new Material(Shader.Find("Ocean/Shape/Combine"));
        }

        private void Update()
        {
            // shape combine pass done by last shape camera - lod 0
            if (LodIndex == 0)
            {
                UpdateCmdBufShapeCombine();
            }
        }

        public float MaxWavelength()
        {
            float oceanBaseScale = OceanRenderer.Instance.transform.lossyScale.x;
            float maxDiameter = 4f * oceanBaseScale * Mathf.Pow(2f, LodIndex);
            float maxTexelSize = maxDiameter / (4f * OceanRenderer.Instance._baseVertDensity);
            return 2f * maxTexelSize * OceanRenderer.Instance._minTexelsPerWave;
        }

        // script execution order ensures this runs after ocean has been placed
        void LateUpdate()
        {
            LateUpdateTransformData();

            LateUpdateShapeCombinePassSettings();
        }

        // apply this camera's properties to the shape combine materials
        void LateUpdateShapeCombinePassSettings()
        {
            BindResultData(0, _combineMaterial);

            if (LodIndex > 0)
            {
                var ldaws = OceanRenderer.Instance.Builder._lodDataAnimWaves;
                BindResultData(1, ldaws[LodIndex - 1]._combineMaterial);
            }
        }

        /// <summary>
        /// Additively combine shape from biggest LODs to smallest LOD. Executed once per frame - attached to the LOD0 camera which
        /// is the last LOD camera to render.
        /// </summary>
        void UpdateCmdBufShapeCombine()
        {
            if(!_shapeCombinePass)
            {
                if (_bufCombineShapes != null)
                {
                    Cam.RemoveCommandBuffer(_combineEvent, _bufCombineShapes);
                    _bufCombineShapes = null;
                }

                return;
            }

            // create shape combine command buffer if it hasn't been created already
            if (_bufCombineShapes == null)
            {
                _bufCombineShapes = new CommandBuffer();
                Cam.AddCommandBuffer(_combineEvent, _bufCombineShapes);
                _bufCombineShapes.name = "Combine Shapes";

                var cams = OceanRenderer.Instance.Builder._camsAnimWaves;
                for (int L = cams.Length - 2; L >= 0; L--)
                {
                    // accumulate shape data down the LOD chain - combine L+1 into L
                    var mat = OceanRenderer.Instance.Builder._lodDataAnimWaves[L]._combineMaterial;
                    _bufCombineShapes.Blit(cams[L + 1].targetTexture, cams[L].targetTexture, mat);
                }
            }
        }

        void RemoveCommandBuffers()
        {
            if (_bufCombineShapes != null)
            {
                Cam.RemoveCommandBuffer(_combineEvent, _bufCombineShapes);
                _bufCombineShapes = null;
            }
        }

        void OnEnable()
        {
            RemoveCommandBuffers();
        }

        void OnDisable()
        {
            RemoveCommandBuffers();
        }

        protected override void BindData(int shapeSlot, IPropertyWrapper properties, Texture applyData, bool blendOut, ref RenderData renderData)
        {
            base.BindData(shapeSlot, properties, applyData, blendOut, ref renderData);

            // need to blend out shape if this is the largest lod, and the ocean might get scaled down later (so the largest lod will disappear)
            bool needToBlendOutShape = LodIndex == LodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease && blendOut;
            float shapeWeight = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            properties.SetVector(_paramsOceanParams[shapeSlot], 
                new Vector4(_renderData._texelWidth, _renderData._textureRes, shapeWeight, 1f / _renderData._textureRes));
        }

        ReadbackDisplacementsForCollision _collReadback;
        public ReadbackDisplacementsForCollision CollReadback
        { get { return _collReadback ?? (_collReadback = GetComponent<ReadbackDisplacementsForCollision>()); } }
    }
}
