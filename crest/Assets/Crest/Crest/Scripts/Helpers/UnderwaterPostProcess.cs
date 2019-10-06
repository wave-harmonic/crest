using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Crest
{
    /// <summary>
    /// Underwater Post Process. If a camera needs to go underwater it needs to have this script attached. This adds fullscreen passes and should
    /// only be used if necessary. This effect disables itself when camera is not close to the water volume.
    ///
    /// For convenience, all shader material settings are copied from the main ocean shader. This includes underwater
    /// specific features such as enabling the meniscus.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnderwaterPostProcess : MonoBehaviour
    {
        static readonly int sp_OceanHeight = Shader.PropertyToID("_OceanHeight");
        static readonly int sp_HorizonOrientation = Shader.PropertyToID("_HorizonOrientation");
        static readonly int sp_MainTex = Shader.PropertyToID("_MainTex");
        static readonly int sp_MaskTex = Shader.PropertyToID("_MaskTex");
        static readonly int sp_MaskDepthTex = Shader.PropertyToID("_MaskDepthTex");
        static readonly int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");
        static readonly int sp_InvViewProjectionRight = Shader.PropertyToID("_InvViewProjectionRight");
        static readonly int sp_InstanceData = Shader.PropertyToID("_InstanceData");
        static readonly int sp_AmbientLighting = Shader.PropertyToID("_AmbientLighting");

        [Header("Settings"), SerializeField, Tooltip("If true, underwater effect copies ocean material params each frame. Setting to false will make it cheaper but risks the underwater appearance looking wrong if the ocean material is changed.")]
        bool _copyOceanMaterialParamsEachFrame = true;

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

        Color[] _ambientLighting = new Color[1];
        SphericalHarmonicsL2 _sphericalHarmonicsL2;
        Vector3[] _shDirections = new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f) };

        bool _eventsRegistered = false;
        bool _firstRender = true;

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

            if (OceanRenderer.Instance && !OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_UNDERWATER_ON"))
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

        private void OnDestroy()
        {
            if (OceanRenderer.Instance && _eventsRegistered)
            {
                OceanRenderer.Instance.ViewerLessThan2mAboveWater -= ViewerLessThan2mAboveWater;
                OceanRenderer.Instance.ViewerMoreThan2mAboveWater -= ViewerMoreThan2mAboveWater;
            }

            _eventsRegistered = false;
        }

        private void ViewerMoreThan2mAboveWater(OceanRenderer ocean)
        {
            enabled = false;
        }

        private void ViewerLessThan2mAboveWater(OceanRenderer ocean)
        {
            enabled = true;
        }

        void OnRenderImage(RenderTexture source, RenderTexture target)
        {
            if (OceanRenderer.Instance == null)
            {
                Graphics.Blit(source, target);
                _eventsRegistered = false;
                return;
            }

            if (!_eventsRegistered)
            {
                OceanRenderer.Instance.ViewerLessThan2mAboveWater += ViewerLessThan2mAboveWater;
                OceanRenderer.Instance.ViewerMoreThan2mAboveWater += ViewerMoreThan2mAboveWater;
                enabled = OceanRenderer.Instance.ViewerHeightAboveWater < 2f;
            }

            if (_commandBuffer == null)
            {
                _commandBuffer = new CommandBuffer();
                _commandBuffer.name = "Underwater Post Process";
            }

            if (GL.wireframe)
            {
                Graphics.Blit(source, target);
                _oceanChunksToRender.Clear();
                return;
            }

            OnRenderImagePopulateMask(source);

            OnRenderImageUpdateMaterial(source);

            _commandBuffer.Blit(source, target, _underwaterPostProcessMaterial);

            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();

            // Need this to prevent Unity from giving the following warning:
            // - "OnRenderImage() possibly didn't write anything to the destination texture!"
            Graphics.SetRenderTarget(target);

            _firstRender = false;
        }

        // Populates a screen space mask which will inform the underwater postprocess. As a future optimisation we may
        // be able to avoid this pass completely if we can reuse the camera depth after transparents are rendered.
        private void OnRenderImagePopulateMask(RenderTexture source)
        {
            if (_textureMask == null || _textureMask.width != source.width || _textureMask.height != source.height)
            {
                _textureMask = new RenderTexture(source);
                _textureMask.name = "Ocean Mask";
                // @Memory: We could investigate making this an 8-bit texture instead to reduce GPU memory usage.
                // We could also potentially try a half res mask as the mensicus could mask res issues.
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

            // Spends approx 0.2-0.3ms here on dell laptop
            foreach (var chunk in _oceanChunksToRender)
            {
                _commandBuffer.DrawRenderer(chunk, _oceanMaskMaterial);
            }

            {
                // The same camera has to perform post-processing when performing
                // VR multipass rendering so don't clear chunks until we have
                // renderered the right eye.
                // This was the approach recommended by Unity's post-processing
                // lead Thomas Hourdel.
                bool saveChunksToRender =
                    XRSettings.enabled &&
                    XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass &&
                    _mainCamera.stereoActiveEye != Camera.MonoOrStereoscopicEye.Right;

                if (!saveChunksToRender) _oceanChunksToRender.Clear();
            }
        }

        void OnRenderImageUpdateMaterial(RenderTexture source)
        {
            if (_firstRender || _copyOceanMaterialParamsEachFrame)
            {
                // Measured this at approx 0.05ms on dell laptop
                _underwaterPostProcessMaterial.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
            }

            // Enabling/disabling keywords each frame don't seem to have large measurable overhead
            if (_viewOceanMask)
            {
                _underwaterPostProcessMaterial.EnableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }
            else
            {
                _underwaterPostProcessMaterial.DisableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }

            _underwaterPostProcessMaterial.SetFloat(LodDataMgr.sp_LD_SliceIndex, 0);
            _underwaterPostProcessMaterial.SetVector(sp_InstanceData, new Vector4(OceanRenderer.Instance.ViewerAltitudeLevelAlpha, 0f, 0f, OceanRenderer.Instance.CurrentLodCount));

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
            if (!XRSettings.enabled || XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
            {

                var viewProjectionMatrix = _mainCamera.projectionMatrix * _mainCamera.worldToCameraMatrix;
                _underwaterPostProcessMaterial.SetMatrix(sp_InvViewProjection, viewProjectionMatrix.inverse);
            }
            else
            {
                var viewProjectionMatrix = _mainCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) * _mainCamera.worldToCameraMatrix;
                _underwaterPostProcessMaterial.SetMatrix(sp_InvViewProjection, viewProjectionMatrix.inverse);
                var viewProjectionMatrixRightEye = _mainCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right) * _mainCamera.worldToCameraMatrix;
                _underwaterPostProcessMaterial.SetMatrix(sp_InvViewProjectionRight, viewProjectionMatrixRightEye.inverse);
            }

            // Not sure why we need to do this - blit should set it...?
            _underwaterPostProcessMaterial.SetTexture(sp_MainTex, source);

            // Compute ambient lighting SH
            {
                LightProbes.GetInterpolatedProbe(OceanRenderer.Instance.Viewpoint.position, null, out _sphericalHarmonicsL2);
                _sphericalHarmonicsL2.Evaluate(_shDirections, _ambientLighting);
                _underwaterPostProcessMaterial.SetVector(sp_AmbientLighting, _ambientLighting[0]);
            }
        }
    }
}
