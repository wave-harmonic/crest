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
    ///  * This LodData currently also takes care of rendering the depth of the ocean sea floor. This could/should be moved out.
    /// </summary>
    public class LodDataAnimatedWaves : LodData
    {
        // debug use
        public static bool _shapeCombinePass = true;

        Material _matOceanDepth;
        RenderTexture _rtOceanDepth;
        CommandBuffer _bufOceanDepth = null;
        bool _oceanDepthRenderersDirty = true;
        /// <summary>Called when one or more objects that will render into depth are created, so that all objects are registered.</summary>
        public void OnOceanDepthRenderersChanged() { _oceanDepthRenderersDirty = true; }

        public override SimSettingsBase CreateDefaultSettings() { return null; }
        public override void UseSettings(SimSettingsBase settings) {}
        // shape format. i tried RGB111110Float but error becomes visible. one option would be to use a UNORM setup.
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.ARGBHalf; } }
        public override int Depth { get { return -10; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Color; } }

        Material _combineMaterial;
        CommandBuffer _bufCombineShapes = null;

        // these would ideally be static but then they get cleared when editing-and-continuing in the editor.
        int[] _paramsDisplacementsSampler;
        int[] _paramsOceanDepthSampler;
        int[] _paramsOceanParams;
        int[] _paramsPosScale;
        int[] _paramsLodIdx;

        void Start()
        {
            Cam.depthTextureMode = DepthTextureMode.None;

            _matOceanDepth = new Material(Shader.Find("Ocean/Ocean Depth"));
            _combineMaterial = new Material(Shader.Find("Ocean/Shape/Combine"));

            // create shader param IDs for each LOD once on start to avoid creating garbage each frame.
            if (_paramsDisplacementsSampler == null)
            {
                int numToGenerate = 16;
                CreateParamIDs(ref _paramsDisplacementsSampler, "_WD_Sampler_", numToGenerate);
                CreateParamIDs(ref _paramsOceanDepthSampler, "_WD_OceanDepth_Sampler_", numToGenerate);
                CreateParamIDs(ref _paramsOceanParams, "_WD_Params_", numToGenerate);
                CreateParamIDs(ref _paramsPosScale, "_WD_Pos_Scale_", numToGenerate);
                CreateParamIDs(ref _paramsLodIdx, "_WD_LodIdx_", numToGenerate);
            }
        }

        private void Update()
        {
            // shape combine pass done by last shape camera - lod 0
            if (LodIndex == 0)
            {
                UpdateCmdBufShapeCombine();
            }

            if (_oceanDepthRenderersDirty)
            {
                UpdateCmdBufOceanFloorDepth();
                _oceanDepthRenderersDirty = false;
            }
        }

        public void CreateParamIDs(ref int[] ids, string prefix, int count)
        {
            ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = Shader.PropertyToID(string.Format("{0}{1}", prefix, i));
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
            ApplyMaterialParams(0, _combineMaterial);

            if (LodIndex > 0)
            {
                var ldaws = OceanRenderer.Instance.Builder._lodDataAnimWaves;
                ApplyMaterialParams(1, ldaws[LodIndex - 1]._combineMaterial);
            }
        }

        // The command buffer populates the LODs with ocean depth data. It submits any objects with a RenderOceanDepth component attached.
        // It's stateless - the textures don't have to be managed across frames/scale changes
        void UpdateCmdBufOceanFloorDepth()
        {
            var objs = FindObjectsOfType<RenderOceanDepth>();

            // if there is nothing in the scene tagged up for depth rendering then there is no depth rendering required
            if (objs.Length < 1)
            {
                if (_bufOceanDepth != null)
                {
                    _bufOceanDepth.Clear();
                }

                return;
            }

            if (!_rtOceanDepth)
            {
                _rtOceanDepth = new RenderTexture(Cam.targetTexture.width, Cam.targetTexture.height, 0);
                _rtOceanDepth.name = gameObject.name + "_oceanDepth";
                _rtOceanDepth.format = RenderTextureFormat.RHalf;
                _rtOceanDepth.useMipMap = false;
                _rtOceanDepth.anisoLevel = 0;
            }

            if (_bufOceanDepth == null)
            {
                _bufOceanDepth = new CommandBuffer();
                Cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _bufOceanDepth);
                _bufOceanDepth.name = "Ocean Depth";
            }

            _bufOceanDepth.Clear();

            _bufOceanDepth.SetRenderTarget( _rtOceanDepth );
            _bufOceanDepth.ClearRenderTarget( false, true, Color.red * 10000f );

            foreach (var obj in objs)
            {
                if (!obj.enabled)
                    continue;

                var r = obj.GetComponent<Renderer>();
                if (r == null)
                {
                    Debug.LogError("GameObject '" + obj.gameObject.name + "' must have a renderer component attached. Unity Terrain objects are not supported - these must be captured by an Ocean Depth Cache.", obj);
                }
                else if (obj.transform.parent != null && obj.transform.parent.GetComponent<OceanDepthCache>() != null)
                {
                    _bufOceanDepth.DrawRenderer(r, r.material);
                }
                else
                {
                    _bufOceanDepth.DrawRenderer(r, _matOceanDepth);
                }
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
                    Cam.RemoveCommandBuffer(CameraEvent.AfterEverything, _bufCombineShapes);
                    _bufCombineShapes = null;
                }

                return;
            }

            // create shape combine command buffer if it hasn't been created already
            if (_bufCombineShapes == null)
            {
                _bufCombineShapes = new CommandBuffer();
                Cam.AddCommandBuffer(CameraEvent.AfterEverything, _bufCombineShapes);
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
            if( _bufOceanDepth != null )
            {
                Cam.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _bufOceanDepth);
                _bufOceanDepth = null;
            }

            if (_bufCombineShapes != null)
            {
                Cam.RemoveCommandBuffer(CameraEvent.AfterEverything, _bufCombineShapes);
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

        PropertyWrapperMaterial _pwMat = new PropertyWrapperMaterial();
        PropertyWrapperMPB _pwMPB = new PropertyWrapperMPB();

        public void ApplyMaterialParams(int shapeSlot, Material properties)
        {
            _pwMat._target = properties;
            ApplyMaterialParams(shapeSlot, _pwMat, true, true);
            _pwMat._target = null;
        }

        public void ApplyMaterialParams(int shapeSlot, MaterialPropertyBlock properties)
        {
            _pwMPB._target = properties;
            ApplyMaterialParams(shapeSlot, _pwMPB, true, true);
            _pwMPB._target = null;
        }

        public void ApplyMaterialParams(int shapeSlot, Material properties, bool applyWaveHeights, bool blendOut)
        {
            _pwMat._target = properties;
            ApplyMaterialParams(shapeSlot, _pwMat, applyWaveHeights, blendOut);
            _pwMat._target = null;
        }

        public void ApplyMaterialParams(int shapeSlot, MaterialPropertyBlock properties, bool applyWaveHeights, bool blendOut)
        {
            _pwMPB._target = properties;
            ApplyMaterialParams(shapeSlot, _pwMPB, applyWaveHeights, blendOut);
            _pwMPB._target = null;
        }

        public void ApplyMaterialParams(int shapeSlot, IPropertyWrapper properties)
        {
            ApplyMaterialParams(shapeSlot, properties, true, true);
        }

        public void ApplyMaterialParams(int shapeSlot, IPropertyWrapper properties, bool applyWaveHeights, bool blendOut)
        {
            if (applyWaveHeights)
            {
                properties.SetTexture(_paramsDisplacementsSampler[shapeSlot], Cam.targetTexture);
            }

            if (_rtOceanDepth != null)
            {
                properties.SetTexture(_paramsOceanDepthSampler[shapeSlot], _rtOceanDepth);
            }

            // need to blend out shape if this is the largest lod, and the ocean might get scaled down later (so the largest lod will disappear)
            bool needToBlendOutShape = LodIndex == LodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease && blendOut;
            float shapeWeight = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            properties.SetVector(_paramsOceanParams[shapeSlot], 
                new Vector4(_renderData._texelWidth, _renderData._textureRes, shapeWeight, 1f / _renderData._textureRes));

            properties.SetVector(_paramsPosScale[shapeSlot], new Vector3(_renderData._posSnapped.x, _renderData._posSnapped.z, transform.lossyScale.x));
            properties.SetFloat(_paramsLodIdx[shapeSlot], LodIndex);
        }

        ReadbackDisplacementsForCollision _collReadback;
        public ReadbackDisplacementsForCollision CollReadback
        { get { return _collReadback ?? (_collReadback = GetComponent<ReadbackDisplacementsForCollision>()); } }
    }
}
