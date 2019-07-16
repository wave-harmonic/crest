using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{

    [RequireComponent(typeof(Camera))]
    public class UnderwaterPostProcess : MonoBehaviour
    {
        static int sp_HorizonHeight = Shader.PropertyToID("_HorizonHeight");
        static int sp_HorizonOrientation = Shader.PropertyToID("_HorizonOrientation");
        static int sp_MaskTex = Shader.PropertyToID("_MaskTex");
        static int sp_MaskDepthTex = Shader.PropertyToID("_MaskDepthTex");
        static int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");
        private const int CHUNKS_PER_LOD = 12;

        public Material _underWaterPostProcMat;
        public Material _oceanMaskMat;

        private Camera _mainCamera;
        private RenderTexture _textureMask;
        private RenderTexture _depthBuffer;
        private CommandBuffer _commandBuffer;
        private PropertyWrapperMaterial _underWaterPostProcMatWrapper;

        // NOTE: We keep a list of ocean chunks to render for a given frame
        // (which ocean chunks add themselves to) and reset it each frame by
        // setting the currentChunkCount to 0. However, this could potentially
        // be a leak if the OceanChunks are ever deleted. We don't expect this
        // to happen, so this approach should be fine for now.
        private Renderer[] _oceanChunksToRender;
        private int _oceanChunksToRenderCount;

        public void RegisterOceanChunkToRender(Renderer _oceanChunk)
        {
            if(_oceanChunksToRenderCount >= _oceanChunksToRender.Length)
            {
                Debug.LogError("Attempting to render more ocean chunks than we have capacity for");
                return;
            }
            _oceanChunksToRender[_oceanChunksToRenderCount] = _oceanChunk;
            _oceanChunksToRenderCount = _oceanChunksToRenderCount + 1;
        }

        void Start()
        {
            _mainCamera = GetComponent<Camera>();
            if (_mainCamera == null)
            {
                Debug.LogError("Underwater effects expect to be attached to a camera", this);
                enabled = false;

                return;
            }

            _commandBuffer = new CommandBuffer();
            _commandBuffer.name = "Underwater Post Process";

            if(_underWaterPostProcMat != null)
            {
                _underWaterPostProcMatWrapper = new PropertyWrapperMaterial(_underWaterPostProcMat);
            }

            _oceanChunksToRender = new Renderer[CHUNKS_PER_LOD * OceanRenderer.Instance.CurrentLodCount];
            _oceanChunksToRenderCount = 0;
        }

        void OnEnable()
        {
            OceanRenderer.Instance.RegisterUnderwaterPostProcessor(this);
        }

        void OnDisable()
        {
            OceanRenderer.Instance.UnregisterUnderwaterPostProcessor(this);
        }

        void OnRenderImage(RenderTexture source, RenderTexture target)
        {
            if (_oceanMaskMat == null)
            {
                _oceanMaskMat = OceanRenderer.Instance.OceanMaterial;
            }

            if (_textureMask == null)
            {
                _textureMask = new RenderTexture(source);
                _textureMask.name = "Ocean Mask";
                // TODO(UPP): See if we can make this an 8bit texture somehow
                _textureMask.format = RenderTextureFormat.RFloat;
                _textureMask.Create();

                _depthBuffer = new RenderTexture(source);
                _depthBuffer.name = "Ocean Mask Depth";
                _depthBuffer.format = RenderTextureFormat.Depth;
                _depthBuffer.Create();
            }

            // Get all ocean chunks and render them using cmd buffer, but with
            _commandBuffer.SetRenderTarget(_textureMask.colorBuffer, _depthBuffer.depthBuffer);
            _commandBuffer.ClearRenderTarget(true, true, Color.black);
            _oceanMaskMat.EnableKeyword("_RENDER_UNDERWATER_MASK");
            _commandBuffer.SetViewProjectionMatrices(_mainCamera.worldToCameraMatrix, _mainCamera.projectionMatrix);
            for(int oceanChunkIndex = 0; oceanChunkIndex < _oceanChunksToRenderCount; oceanChunkIndex++)
            {
                Renderer renderer = _oceanChunksToRender[oceanChunkIndex];
                _commandBuffer.DrawRenderer(renderer, _oceanMaskMat);
            }
            _oceanChunksToRenderCount = 0;

            // TODO(UPP): handle Roll
            float horizonRoll = 0.0f;
            float horizonHeight = 0.0f;
            {
                // Calculate the horizon height in screen space
                // TODO(UPP): Get this to actually work
                float halfFov = _mainCamera.fieldOfView * 0.5f;
                Vector3 cameraForward = _mainCamera.transform.forward;
                float cameraRotation = Mathf.Atan2(-1.0f * cameraForward.y, (new Vector2(cameraForward.x, cameraForward.z)).magnitude);
                float halfProp = Mathf.Tan(cameraRotation * 0.5f) / Mathf.Tan(halfFov * Mathf.Deg2Rad);
                horizonHeight = halfProp + 0.5f;
            }

            _underWaterPostProcMat.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
            _underWaterPostProcMat.SetFloat(OceanRenderer.sp_LD_SliceIndex, 0);

            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(_underWaterPostProcMatWrapper);
            if (OceanRenderer.Instance._lodDataSeaDepths)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(_underWaterPostProcMatWrapper);
            }
            else
            {
                LodDataMgrSeaFloorDepth.BindNull(_underWaterPostProcMatWrapper);
            }
            if (OceanRenderer.Instance._lodDataShadow)
            {
                OceanRenderer.Instance._lodDataShadow.BindResultData(_underWaterPostProcMatWrapper);
            }
            else
            {
                LodDataMgrShadow.BindNull(_underWaterPostProcMatWrapper);
            }

            _underWaterPostProcMat.SetFloat(sp_HorizonHeight, horizonHeight);
            _underWaterPostProcMat.SetFloat(sp_HorizonOrientation, horizonRoll);
            _underWaterPostProcMat.SetTexture(sp_MaskTex, _textureMask);
            _underWaterPostProcMat.SetTexture(sp_MaskDepthTex, _depthBuffer);

            // Have to set this explicitly as the built-in transforms aren't in world-space for the blit function
            _underWaterPostProcMat.SetMatrix(sp_InvViewProjection, (_mainCamera.projectionMatrix * _mainCamera.worldToCameraMatrix).inverse);

            _commandBuffer.Blit(source, target, _underWaterPostProcMat);

            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _oceanMaskMat.DisableKeyword("_RENDER_UNDERWATER_MASK");
            _commandBuffer.Clear();

            // Need this to prevent Unity from giving the following warning.
            // - OnRenderImage() possibly didn't write anything to the destination texture!
            Graphics.SetRenderTarget(target);
        }
    }

}
