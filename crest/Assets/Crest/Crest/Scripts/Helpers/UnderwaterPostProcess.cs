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
        static int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");

        [Header("Debug Options"), SerializeField]
        bool _viewOceanMask = false;
        // end public debug options

        private Camera _mainCamera;
        private RenderTexture _textureMask;
        private RenderTexture _depthBuffer;
        private CommandBuffer _commandBuffer;

        private Material _oceanMaskMaterial = null;

        private Material _underwaterPostProcessMaterial = null;
        private PropertyWrapperMaterial _underwaterPostProcessMaterialWrapper;

        // NOTE: We keep a list of ocean chunks to render for a given frame
        // (which ocean chunks add themselves to) and reset it each frame by
        // setting the currentChunkCount to 0. However, this could potentially
        // be a leak if the OceanChunks are ever deleted. We don't expect this
        // to happen, so this approach should be fine for now.
        private List<Renderer> _oceanChunksToRender;

        // This matches const on shader side
        private const float UNDERWATER_MASK_NO_MASK = 1.0f;
        private const string FULL_SCREEN_EFFECT = "_FULL_SCREEN_EFFECT";
        private const string DEBUG_VIEW_OCEAN_MASK = "_DEBUG_VIEW_OCEAN_MASK";

        private const string SHADER_UNDERWATER = "Crest/Underwater/Post Process";
        private const string SHADER_OCEAN_MASK = "Crest/Underwater/Ocean Mask";


        public void RegisterOceanChunkToRender(Renderer _oceanChunk)
        {
            _oceanChunksToRender.Add(_oceanChunk);
        }

        private bool InitialisedCorrectly()
        {
            _mainCamera = GetComponent<Camera>();
            if (_mainCamera == null)
            {
                Debug.LogError("UnderwaterPostProcess must be attached to a camera", this);
                return false;
            }

            _underwaterPostProcessMaterial = new Material(Shader.Find(SHADER_UNDERWATER));
            if (_underwaterPostProcessMaterial == null)
            {
                Debug.LogError("UnderwaterPostProcess expects to have a post processing material attached", this);
                return false;
            }

            _oceanMaskMaterial = new Material(Shader.Find(SHADER_OCEAN_MASK));
            if (_oceanMaskMaterial == null)
            {
                Debug.LogError("UnderwaterPostProcess expects to have an ocean mask material attached", this);
                return false;
            }

            if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_UNDERWATER_ON"))
            {
                Debug.LogError("Underwater must be enabled on the ocean material for UnderwaterPostProcess to work", this);
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
            _underwaterPostProcessMaterial = new Material(_underwaterPostProcessMaterial);
            _underwaterPostProcessMaterialWrapper = new PropertyWrapperMaterial(_underwaterPostProcessMaterial);

            _oceanChunksToRender = new List<Renderer>(OceanBuilder.GetChunkCount);
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
                _oceanChunksToRender.Clear();
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
            _commandBuffer.ClearRenderTarget(true, true, Color.white * UNDERWATER_MASK_NO_MASK);
            _commandBuffer.SetViewProjectionMatrices(_mainCamera.worldToCameraMatrix, _mainCamera.projectionMatrix);

            foreach (var chunk in _oceanChunksToRender)
            {
                _commandBuffer.DrawRenderer(chunk, _oceanMaskMaterial);
            }
            _oceanChunksToRender.Clear();

            _underwaterPostProcessMaterial.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);

            if (_viewOceanMask)
            {
                _underwaterPostProcessMaterial.EnableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }
            else
            {
                _underwaterPostProcessMaterial.DisableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }

            _underwaterPostProcessMaterial.SetFloat(OceanRenderer.sp_LD_SliceIndex, 0);

            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(_underwaterPostProcessMaterialWrapper);
            if (OceanRenderer.Instance._lodDataSeaDepths)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(_underwaterPostProcessMaterialWrapper);
            }
            else
            {
                LodDataMgrSeaFloorDepth.BindNull(_underwaterPostProcessMaterialWrapper);
            }
            if (OceanRenderer.Instance._lodDataShadow)
            {
                OceanRenderer.Instance._lodDataShadow.BindResultData(_underwaterPostProcessMaterialWrapper);
            }
            else
            {
                LodDataMgrShadow.BindNull(_underwaterPostProcessMaterialWrapper);
            }

            {
                float oceanHeight = OceanRenderer.Instance.transform.position.y;
                float maxOceanVerticalDisplacement = OceanRenderer.Instance.MaxVertDisplacement * 0.5f;
                float cameraHeight = _mainCamera.transform.position.y;
                bool forceFullShader = (cameraHeight + maxOceanVerticalDisplacement) <= oceanHeight;
                _underwaterPostProcessMaterial.SetFloat(sp_OceanHeight, oceanHeight);
                if (forceFullShader)
                {
                    _underwaterPostProcessMaterial.EnableKeyword(FULL_SCREEN_EFFECT);
                }
                else
                {
                    _underwaterPostProcessMaterial.DisableKeyword(FULL_SCREEN_EFFECT);
                }
            }

            _underwaterPostProcessMaterial.SetTexture(sp_MaskTex, _textureMask);
            _underwaterPostProcessMaterial.SetTexture(sp_MaskDepthTex, _depthBuffer);

            // Have to set these explicitly as the built-in transforms aren't in world-space for the blit function
            {
                var viewProjectionMatrix = _mainCamera.projectionMatrix * _mainCamera.worldToCameraMatrix;
                _underwaterPostProcessMaterial.SetMatrix(sp_InvViewProjection, viewProjectionMatrix.inverse);
            }

            _commandBuffer.Blit(source, target, _underwaterPostProcessMaterial);

            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();

            // Need this to prevent Unity from giving the following warning.
            // - OnRenderImage() possibly didn't write anything to the destination texture!
            Graphics.SetRenderTarget(target);
        }
    }
}
