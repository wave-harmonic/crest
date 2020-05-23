// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using static Crest.UnderwaterPostProcessUtils;

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
        [Header("Settings"), SerializeField, Tooltip("If true, underwater effect copies ocean material params each frame. Setting to false will make it cheaper but risks the underwater appearance looking wrong if the ocean material is changed.")]
        bool _copyOceanMaterialParamsEachFrame = true;

        [SerializeField, Tooltip("Assign this to a material that uses shader `Crest/Underwater/Post Process`, with the same features enabled as the ocean surface material(s).")]
        Material _underwaterPostProcessMaterial;

        [Header("Debug Options")]
        [SerializeField] bool _viewPostProcessMask = false;
        [SerializeField] bool _disableOceanMask = false;
        [SerializeField, Tooltip(UnderwaterPostProcessUtils.tooltipHorizonSafetyMarginMultiplier), Range(0f, 1f)]
        float _horizonSafetyMarginMultiplier = UnderwaterPostProcessUtils.DefaultHorizonSafetyMarginMultiplier;
        // end public debug options

        private Camera _mainCamera;
        private RenderTexture _oceanTextureMask;
        private RenderTexture _oceanDepthBuffer;
        private RenderTexture _generalTextureMask;
        private RenderTexture _generalDepthBuffer;
        private CommandBuffer _maskCommandBuffer;
        private CommandBuffer _postProcessCommandBuffer;

        private Plane[] _cameraFrustumPlanes;

        private Material _oceanMaskMaterial = null;
        private Material _generalMaskMaterial = null;

        private PropertyWrapperMaterial _underwaterPostProcessMaterialWrapper;

        private List<UnderwaterEffectFilter> _generalUnderwaterMasksToRender;
        public List<UnderwaterEffectFilter> GeneralUnderwaterMasksToRender => _generalUnderwaterMasksToRender;

        private const string SHADER_OCEAN_MASK = "Crest/Underwater/Ocean Mask";
        private const string SHADER_GENERAL_MASK = "Crest/Underwater/General Underwater Mask";

        UnderwaterSphericalHarmonicsData _sphericalHarmonicsData = new UnderwaterSphericalHarmonicsData();

        bool _eventsRegistered = false;
        bool _firstRender = true;

        public void RegisterGeneralUnderwaterMaskToRender(UnderwaterEffectFilter _underwaterEffectFilter)
        {
            _generalUnderwaterMasksToRender.Add(_underwaterEffectFilter);
        }

        private bool InitialisedCorrectly()
        {
            _mainCamera = GetComponent<Camera>();
            if (_mainCamera == null)
            {
                Debug.LogError("UnderwaterPostProcess must be attached to a camera", this);
                return false;
            }

            if (_underwaterPostProcessMaterial == null)
            {
                Debug.LogError("UnderwaterPostProcess must have a post processing material assigned", this);
                return false;
            }

            {
                var maskShader = Shader.Find(SHADER_OCEAN_MASK);
                _oceanMaskMaterial = maskShader ? new Material(maskShader) : null;
                if (_oceanMaskMaterial == null)
                {
                    Debug.LogError($"Could not create a material with shader {SHADER_OCEAN_MASK}", this);
                    return false;
                }
            }

            {
                var generalMaskShader = Shader.Find(SHADER_GENERAL_MASK);
                _generalMaskMaterial = generalMaskShader ? new Material(generalMaskShader) : null;
                if (_generalMaskMaterial == null)
                {
                    Debug.LogError($"Could not create a material with shader {SHADER_GENERAL_MASK}", this);
                    return false;
                }
            }

            if (OceanRenderer.Instance && !OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_UNDERWATER_ON"))
            {
                Debug.LogError("Underwater must be enabled on the ocean material for UnderwaterPostProcess to work", this);
                return false;
            }

            return CheckMaterial();
        }

        bool CheckMaterial()
        {
            var success = true;

            var keywords = _underwaterPostProcessMaterial.shaderKeywords;
            foreach (var keyword in keywords)
            {
                if (keyword == "_COMPILESHADERWITHDEBUGINFO_ON") continue;

                if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled(keyword))
                {
                    Debug.LogWarning($"Keyword {keyword} was enabled on the underwater material {_underwaterPostProcessMaterial.name} but not on the ocean material {OceanRenderer.Instance.OceanMaterial.name}, underwater appearance may not match ocean surface in standalone builds.", this);

                    success = false;
                }
            }

            return success;
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

            _generalUnderwaterMasksToRender = new List<UnderwaterEffectFilter>();
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
            // TODO(TRC):Now This "optimisation" doesn't work if you have underwater windows, because the ocean mask
            // needs to be rendered for them to work no matter the heigh of the camera- > it gives the ocean holes.

            // I think it might be worth re-considering remove the event system for this stuff and implementing this as
            // separate enabler script that spins-up to enable/disabel to the post-process effect. (maybe?)
            //
            // That way we can actively check if there are any UnderwaterEffectFilters, and then only enable this if
            // there aren't any? :DeOptimiseForFilters
            // enabled = false;
        }

        private void ViewerLessThan2mAboveWater(OceanRenderer ocean)
        {
            enabled = true;
        }

        void OnPreRender()
        {
            // Allocate planes only once
            if (_cameraFrustumPlanes == null)
            {
                _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
                _maskCommandBuffer = new CommandBuffer();
                _maskCommandBuffer.name = "Ocean Mask Command Buffer";
                _mainCamera.AddCommandBuffer(
                    CameraEvent.BeforeForwardAlpha,
                    _maskCommandBuffer
                );
            }
            else
            {
                GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraFrustumPlanes);
                _maskCommandBuffer.Clear();
            }

            {
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor(_mainCamera.pixelWidth, _mainCamera.pixelHeight);
                InitialiseMaskTextures(descriptor, true, ref _oceanTextureMask, ref _oceanDepthBuffer);
                InitialiseMaskTextures(descriptor, false, ref _generalTextureMask, ref _generalDepthBuffer);
            }

            PopulateOceanMask(
                _maskCommandBuffer, _mainCamera,
                OceanBuilder.OceanChunkRenderers,
                _generalUnderwaterMasksToRender,
                _cameraFrustumPlanes,
                _oceanTextureMask, _oceanDepthBuffer,
                _generalTextureMask, _generalDepthBuffer,
                _oceanMaskMaterial, _generalMaskMaterial,
                _sphericalHarmonicsData,
                _horizonSafetyMarginMultiplier,
                _disableOceanMask
            );

            _generalUnderwaterMasksToRender.Clear();
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
                // TODO(TRC):Now See :DeOptimiseForFilters
                // enabled = OceanRenderer.Instance.ViewerHeightAboveWater < 2f;
                _eventsRegistered = true;
            }

            if (_postProcessCommandBuffer == null)
            {
                _postProcessCommandBuffer = new CommandBuffer();
                _postProcessCommandBuffer.name = "Underwater Post Process";
            }

            if (GL.wireframe)
            {
                Graphics.Blit(source, target);
                return;
            }

            UpdatePostProcessMaterial(
                source,
                _mainCamera,
                _underwaterPostProcessMaterialWrapper,
                _firstRender || _copyOceanMaterialParamsEachFrame,
                _viewPostProcessMask
            );

            _postProcessCommandBuffer.Blit(source, target, _underwaterPostProcessMaterial);

            Graphics.ExecuteCommandBuffer(_postProcessCommandBuffer);
            _postProcessCommandBuffer.Clear();

            // Need this to prevent Unity from giving the following warning:
            // - "OnRenderImage() possibly didn't write anything to the destination texture!"
            Graphics.SetRenderTarget(target);

            _firstRender = false;
        }
    }
}
