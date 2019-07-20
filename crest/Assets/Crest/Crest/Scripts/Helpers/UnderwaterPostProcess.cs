using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{

    [RequireComponent(typeof(Camera))]
    public class UnderwaterPostProcess : MonoBehaviour
    {
        static int sp_OceanHeight = Shader.PropertyToID("_OceanHeight");
        static int sp_HorizonOrientation = Shader.PropertyToID("_HorizonOrientation");
        static int sp_MaskTex = Shader.PropertyToID("_MaskTex");
        static int sp_MaskDepthTex = Shader.PropertyToID("_MaskDepthTex");
        static int sp_ViewProjection = Shader.PropertyToID("_ViewProjection");
        static int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");

        public Material _underWaterPostProcMat;
        public Material _oceanMaskMat;

        [Header("Debug Options")]
        public bool _viewOceanMask;
        // end public debug options

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
        private List<Renderer> _oceanChunksToRender;
        private int _oceanChunksToRenderCount;

        private const string _RENDER_UNDERWATER_MASK = "_RENDER_UNDERWATER_MASK";
        private const string _FULL_SCREEN_EFFECT = "_FULL_SCREEN_EFFECT";
        private const string _DEBUG_VIEW_OCEAN_MASK = "_DEBUG_VIEW_OCEAN_MASK";


        public void RegisterOceanChunkToRender(Renderer _oceanChunk)
        {
            if (_oceanChunksToRenderCount >= _oceanChunksToRender.Count)
            {
                _oceanChunksToRender.Add(_oceanChunk);
            }
            else
            {
                _oceanChunksToRender[_oceanChunksToRenderCount] = _oceanChunk;
            }
            _oceanChunksToRenderCount = _oceanChunksToRenderCount + 1;
        }

        private bool InitialisedCorrectly()
        {
            _mainCamera = GetComponent<Camera>();
            if (_mainCamera == null)
            {
                Debug.LogError("Underwater effects expect to be attached to a camera", this);
                return false;
            }
            if (_underWaterPostProcMat == null)
            {
                Debug.LogError("Underwater effect expects to have a post processing material attached", this);
                return false;
            }
            if(_oceanMaskMat == null)
            {
                Debug.LogError("Underwater effect expects to have an ocean mask material attached", this);
                return false;
            }
            return true;
        }

        void Start()
        {
            if(!InitialisedCorrectly())
            {
                enabled = false;
                return;
            }

            _commandBuffer = new CommandBuffer();
            _commandBuffer.name = "Underwater Post Process";

            if (_underWaterPostProcMat != null)
            {
                _underWaterPostProcMatWrapper = new PropertyWrapperMaterial(_underWaterPostProcMat);
            }

            _oceanChunksToRender = new List<Renderer>(OceanBuilder.GetChunkCount);
            _oceanChunksToRenderCount = 0;
        }

        void OnRenderImage(RenderTexture source, RenderTexture target)
        {
            bool definitelyAboveTheWater = false;
            {
                float oceanHeight = OceanRenderer.Instance.transform.position.y;
                float maxOceanVerticalDisplacement = OceanRenderer.Instance.MaxVertDisplacement * 0.5f;
                float cameraHeight = _mainCamera.transform.position.y;
                definitelyAboveTheWater = (cameraHeight - maxOceanVerticalDisplacement) >= oceanHeight;
            }

            if (GL.wireframe || definitelyAboveTheWater)
            {
                Graphics.Blit(source, target);
                _oceanChunksToRenderCount = 0;
                return;
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
            _oceanMaskMat.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
            _commandBuffer.SetViewProjectionMatrices(_mainCamera.worldToCameraMatrix, _mainCamera.projectionMatrix);
            for (int oceanChunkIndex = 0; oceanChunkIndex < _oceanChunksToRenderCount; oceanChunkIndex++)
            {
                Renderer renderer = _oceanChunksToRender[oceanChunkIndex];
                _commandBuffer.DrawRenderer(renderer, _oceanMaskMat);
            }
            _oceanChunksToRenderCount = 0;

            _underWaterPostProcMat.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);

            if(_viewOceanMask)
            {
                _underWaterPostProcMat.EnableKeyword(_DEBUG_VIEW_OCEAN_MASK);
            }
            else
            {
                _underWaterPostProcMat.DisableKeyword(_DEBUG_VIEW_OCEAN_MASK);
            }

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

            {
                float oceanHeight = OceanRenderer.Instance.transform.position.y;
                float maxOceanVerticalDisplacement = OceanRenderer.Instance.MaxVertDisplacement * 0.5f;
                float cameraHeight = _mainCamera.transform.position.y;
                bool forceFullShader = (cameraHeight + maxOceanVerticalDisplacement) <= oceanHeight;
                _underWaterPostProcMat.SetFloat(sp_OceanHeight, oceanHeight);
                if(forceFullShader)
                {
                    _underWaterPostProcMat.EnableKeyword(_FULL_SCREEN_EFFECT);
                }
                else
                {
                    _underWaterPostProcMat.DisableKeyword(_FULL_SCREEN_EFFECT);
                }
            }
            _underWaterPostProcMat.SetTexture(sp_MaskTex, _textureMask);
            _underWaterPostProcMat.SetTexture(sp_MaskDepthTex, _depthBuffer);

            // Have to set these explicitly as the built-in transforms aren't in world-space for the blit function
            {
                var viewProjectionMatrix = _mainCamera.projectionMatrix * _mainCamera.worldToCameraMatrix;
                _underWaterPostProcMat.SetMatrix(sp_ViewProjection, viewProjectionMatrix);
                _underWaterPostProcMat.SetMatrix(sp_InvViewProjection, viewProjectionMatrix.inverse);
            }

            _commandBuffer.Blit(source, target, _underWaterPostProcMat);

            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();

            // Need this to prevent Unity from giving the following warning.
            // - OnRenderImage() possibly didn't write anything to the destination texture!
            Graphics.SetRenderTarget(target);
        }
    }

}
