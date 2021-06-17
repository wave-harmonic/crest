// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEditor;
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
    public class UnderwaterRenderer : MonoBehaviour
    {
        [Header("Settings"), SerializeField, Tooltip("If true, underwater effect copies ocean material params each frame. Setting to false will make it cheaper but risks the underwater appearance looking wrong if the ocean material is changed.")]
        bool _copyOceanMaterialParamsEachFrame = true;

        [SerializeField, Tooltip(tooltipFilterOceanData), Range(MinFilterOceanDataValue, MaxFilterOceanDataValue)]
        public int _filterOceanData = DefaultFilterOceanDataValue;

        [SerializeField, Tooltip(tooltipMeniscus)]
        bool _meniscus = true;

        [Header("Debug Options")]
        [SerializeField] bool _viewPostProcessMask = false;
        [SerializeField] bool _disableOceanMask = false;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Adjusts the far plane for horizon line calculation. Helps with horizon line issue. (Experimental)")]
        float _farPlaneMultiplier = 0.68f;

        [SerializeField]
        [Tooltip("Use the old horizon safety margin multiplier to fix horizon line issues instead of the new experimental far plane multiplier.")]
        bool _useHorizonSafetyMarginMultiplier = false;

        [SerializeField, Tooltip(tooltipHorizonSafetyMarginMultiplier), Range(0f, 1f)]
        float _horizonSafetyMarginMultiplier = DefaultHorizonSafetyMarginMultiplier;
        // end public debug options


        private Camera _mainCamera;
        private Plane[] _cameraFrustumPlanes;
        private RenderTexture _textureMask;
        private RenderTexture _depthBuffer;
        private CommandBuffer _oceanMaskCommandBuffer;
        PropertyWrapperMaterial _oceanMaskMaterial;
        private CommandBuffer _underwaterEffectCommandBuffer;
        PropertyWrapperMaterial _underwaterEffectMaterial;

        const string SHADER_UNDERWATER_EFFECT = "Hidden/Crest/Underwater/Underwater Effect";
        private const string SHADER_OCEAN_MASK = "Hidden/Crest/Underwater/Ocean Mask";

        readonly UnderwaterSphericalHarmonicsData _sphericalHarmonicsData = new UnderwaterSphericalHarmonicsData();

        bool _firstRender = true;

        int sp_CrestCameraColorTexture = Shader.PropertyToID("_CrestCameraColorTexture");

        static int _xrPassIndex = -1;

        // Only one camera is supported.
        public static UnderwaterRenderer Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            Instance = null;
            _xrPassIndex = -1;
        }

        void OnEnable()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GetComponent<Camera>();
            }

            if (_oceanMaskMaterial?.material == null)
            {
                _oceanMaskMaterial = new PropertyWrapperMaterial(SHADER_OCEAN_MASK);
            }

            if (_underwaterEffectMaterial?.material == null)
            {
                _underwaterEffectMaterial = new PropertyWrapperMaterial(SHADER_UNDERWATER_EFFECT);
            }

            if (_underwaterEffectCommandBuffer == null)
            {
                _underwaterEffectCommandBuffer = new CommandBuffer()
                {
                    name = "Underwater Pass",
                };
            }

            if (_oceanMaskCommandBuffer == null)
            {
                _oceanMaskCommandBuffer = new CommandBuffer()
                {
                    name = "Ocean Mask",
                };
            }

            if (_cameraFrustumPlanes == null)
            {
                _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
            }

            _mainCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, _underwaterEffectCommandBuffer);
            _mainCamera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _oceanMaskCommandBuffer);
            Instance = this;
        }

        void OnDisable()
        {
            _mainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _underwaterEffectCommandBuffer);
            _mainCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _oceanMaskCommandBuffer);
            Instance = null;
        }

        void OnPreRender()
        {
            if (OceanRenderer.Instance == null)
            {
                OnDisable();
                return;
            }

            if (GL.wireframe)
            {
                OnDisable();
                return;
            }

            if (OceanRenderer.Instance.ViewerHeightAboveWater > 2f)
            {
                OnDisable();
                return;
            }

            if (Instance == null)
            {
                OnEnable();
            }

            // Ensure legacy underwater fog is disabled.
            if (_firstRender)
            {
                OceanRenderer.Instance.OceanMaterial.DisableKeyword("_OLD_UNDERWATER");
            }

            XRHelpers.Update(_mainCamera);
            XRHelpers.UpdatePassIndex(ref _xrPassIndex);

            RenderTextureDescriptor descriptor = XRHelpers.GetRenderTextureDescriptor(_mainCamera);

            InitialiseMaskTextures(descriptor, ref _textureMask, ref _depthBuffer);

            _oceanMaskCommandBuffer.Clear();
            // Passing -1 to depth slice binds all slices. Important for XR SPI to work in both eyes.
            _oceanMaskCommandBuffer.SetRenderTarget(_textureMask.colorBuffer, _depthBuffer.depthBuffer, mipLevel: 0, CubemapFace.Unknown, depthSlice: -1);
            _oceanMaskCommandBuffer.ClearRenderTarget(true, true, Color.white * UNDERWATER_MASK_NO_MASK);
            _oceanMaskCommandBuffer.SetGlobalTexture(sp_CrestOceanMaskTexture, _textureMask.colorBuffer);
            _oceanMaskCommandBuffer.SetGlobalTexture(sp_CrestOceanMaskDepthTexture, _depthBuffer.depthBuffer);

            PopulateOceanMask
            (
                _oceanMaskCommandBuffer,
                _mainCamera,
                OceanRenderer.Instance.Tiles,
                _cameraFrustumPlanes,
                _oceanMaskMaterial.material,
                _disableOceanMask
            );

            descriptor.useDynamicScale = _mainCamera.allowDynamicResolution;
            // Format must be correct for CopyTexture to work. Hopefully this is good enough.
            if (_mainCamera.allowHDR)
            {
                descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            }

            var temporaryColorBuffer = RenderTexture.GetTemporary(descriptor);

            UpdatePostProcessMaterial
            (
                _mainCamera,
                _underwaterEffectMaterial,
                _sphericalHarmonicsData,
                _meniscus,
                _firstRender || _copyOceanMaterialParamsEachFrame,
                _viewPostProcessMask,
                 // horizonSafetyMarginMultiplier is added to the horizon, so no-op is zero.
                 _useHorizonSafetyMarginMultiplier ? _horizonSafetyMarginMultiplier : 0f,
                 // farPlaneMultiplier is multiplied to the far plane, so no-op is one.
                 _useHorizonSafetyMarginMultiplier ? 1f : _farPlaneMultiplier,
                _filterOceanData,
                _xrPassIndex
            );

            _underwaterEffectCommandBuffer.Clear();

            if (_mainCamera.allowMSAA)
            {
                // Use blit if MSAA is active because transparents were not included with CopyTexture.
                // Not sure if we need an MSAA resolve? Not sure how to do that...
                _underwaterEffectCommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, temporaryColorBuffer);
            }
            else
            {
                // Copy the frame buffer as we cannot read/write at the same time. If it causes problems, replace with Blit.
                _underwaterEffectCommandBuffer.CopyTexture(BuiltinRenderTextureType.CameraTarget, temporaryColorBuffer);
            }

            _underwaterEffectMaterial.SetTexture(sp_CrestCameraColorTexture, temporaryColorBuffer);

            _underwaterEffectCommandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown, -1);
            _underwaterEffectCommandBuffer.DrawProcedural(Matrix4x4.identity, _underwaterEffectMaterial.material, -1, MeshTopology.Triangles, 3, 1);

            RenderTexture.ReleaseTemporary(temporaryColorBuffer);

            _firstRender = false;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UnderwaterRenderer))]
    public class UnderwaterRendererEditor : Editor {}
#endif
}
