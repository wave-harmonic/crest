using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{

    [RequireComponent(typeof(Camera))]
    public class UnderwaterCamera : MonoBehaviour
    {
        static int sp_OceanHeight = Shader.PropertyToID("_OceanHeight");
        static int sp_HorizonOrientation = Shader.PropertyToID("_HorizonOrientation");
        static int sp_MaskTex = Shader.PropertyToID("_MaskTex");
        static int sp_MaskDepthTex = Shader.PropertyToID("_MaskDepthTex");
        static int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");

        [Header("Underwater Camera Materials")]
        [Tooltip("Material used to render underwater fog in post-process.")]
        public Material _underwaterPostProcessorMaterial;
        [Tooltip("Material used to re-render ocean to create a mask for underwater rendering.")]
        public Material _oceanMaskMaterial;

        [Header("Debug Options")]
        public bool _viewOceanMask;
        // end public debug options

        private Camera _mainCamera;
        private RenderTexture _textureMask;
        private RenderTexture _depthBuffer;
        private CommandBuffer _commandBuffer;
        private PropertyWrapperMaterial _underwaterPostProcessorMaterialWrapper;

        // NOTE: We keep a list of ocean chunks to render for a given frame
        // (which ocean chunks add themselves to) and reset it each frame by
        // setting the currentChunkCount to 0. However, this could potentially
        // be a leak if the OceanChunks are ever deleted. We don't expect this
        // to happen, so this approach should be fine for now.
        private List<Renderer> _oceanChunksToRender;
        private int _oceanChunksToRenderCount;

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
                Debug.LogError("UnderwaterCameras expect to be attached to a camera", this);
                return false;
            }
            if (_underwaterPostProcessorMaterial == null)
            {
                Debug.LogError("UnderwaterCamera expects to have a post processing material attached", this);
                return false;
            }
            if (_oceanMaskMaterial == null)
            {
                Debug.LogError("UnderwaterCamera expects to have an ocean mask material attached", this);
                return false;
            }
            if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_UNDERWATER_ON"))
            {
                Debug.LogError("Underwater must be enabled on the ocean material for UnderWaterCamera to work", this);
                return false;
            }
            return true;
        }

        void Start()
        {
            if (!InitialisedCorrectly())
            {
                enabled = false;
                return;
            }

            // Stop the material from being saved on-edits at runtime
            _underwaterPostProcessorMaterial = new Material(_underwaterPostProcessorMaterial);
            _underwaterPostProcessorMaterialWrapper = new PropertyWrapperMaterial(_underwaterPostProcessorMaterial);

            _oceanChunksToRender = new List<Renderer>(OceanBuilder.GetChunkCount);
            _oceanChunksToRenderCount = 0;
        }

        void OnRenderImage(RenderTexture source, RenderTexture target)
        {
            if (_commandBuffer == null)
            {
                _commandBuffer = new CommandBuffer();
                _commandBuffer.name = "Underwater Post Process";
            }

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
                // @Memory: We could investigate making this an 8-bit texture
                // instead to reduce GPU memory usage.
                _textureMask.format = RenderTextureFormat.RHalf;
                _textureMask.Create();

                _depthBuffer = new RenderTexture(source);
                _depthBuffer.name = "Ocean Mask Depth";
                _depthBuffer.format = RenderTextureFormat.Depth;
                _depthBuffer.Create();
            }

            // Get all ocean chunks and render them using cmd buffer, but with mask shader
            _commandBuffer.SetRenderTarget(_textureMask.colorBuffer, _depthBuffer.depthBuffer);
            _commandBuffer.ClearRenderTarget(true, true, Color.white);
            _commandBuffer.SetViewProjectionMatrices(_mainCamera.worldToCameraMatrix, _mainCamera.projectionMatrix);

            for (int oceanChunkIndex = 0; oceanChunkIndex < _oceanChunksToRenderCount; oceanChunkIndex++)
            {
                _commandBuffer.DrawRenderer(_oceanChunksToRender[oceanChunkIndex], _oceanMaskMaterial);
            }
            _oceanChunksToRenderCount = 0;

            _underwaterPostProcessorMaterial.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);

            if (_viewOceanMask)
            {
                _underwaterPostProcessorMaterial.EnableKeyword(_DEBUG_VIEW_OCEAN_MASK);
            }
            else
            {
                _underwaterPostProcessorMaterial.DisableKeyword(_DEBUG_VIEW_OCEAN_MASK);
            }

            _underwaterPostProcessorMaterial.SetFloat(OceanRenderer.sp_LD_SliceIndex, 0);

            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(_underwaterPostProcessorMaterialWrapper);
            if (OceanRenderer.Instance._lodDataSeaDepths)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(_underwaterPostProcessorMaterialWrapper);
            }
            else
            {
                LodDataMgrSeaFloorDepth.BindNull(_underwaterPostProcessorMaterialWrapper);
            }
            if (OceanRenderer.Instance._lodDataShadow)
            {
                OceanRenderer.Instance._lodDataShadow.BindResultData(_underwaterPostProcessorMaterialWrapper);
            }
            else
            {
                LodDataMgrShadow.BindNull(_underwaterPostProcessorMaterialWrapper);
            }

            {
                float oceanHeight = OceanRenderer.Instance.transform.position.y;
                float maxOceanVerticalDisplacement = OceanRenderer.Instance.MaxVertDisplacement * 0.5f;
                float cameraHeight = _mainCamera.transform.position.y;
                bool forceFullShader = (cameraHeight + maxOceanVerticalDisplacement) <= oceanHeight;
                _underwaterPostProcessorMaterial.SetFloat(sp_OceanHeight, oceanHeight);
                if (forceFullShader)
                {
                    _underwaterPostProcessorMaterial.EnableKeyword(_FULL_SCREEN_EFFECT);
                }
                else
                {
                    _underwaterPostProcessorMaterial.DisableKeyword(_FULL_SCREEN_EFFECT);
                }
            }

            _underwaterPostProcessorMaterial.SetTexture(sp_MaskTex, _textureMask);
            _underwaterPostProcessorMaterial.SetTexture(sp_MaskDepthTex, _depthBuffer);

            // Have to set these explicitly as the built-in transforms aren't in world-space for the blit function
            {
                var viewProjectionMatrix = _mainCamera.projectionMatrix * _mainCamera.worldToCameraMatrix;
                _underwaterPostProcessorMaterial.SetMatrix(sp_InvViewProjection, viewProjectionMatrix.inverse);
            }

            _commandBuffer.Blit(source, target, _underwaterPostProcessorMaterial);

            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();

            // Need this to prevent Unity from giving the following warning.
            // - OnRenderImage() possibly didn't write anything to the destination texture!
            Graphics.SetRenderTarget(target);
        }
    }
}
