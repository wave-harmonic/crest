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
    public class UnderwaterPostProcess : MonoBehaviour
    {
        [SerializeField]
        MeshFilter _waterVolumeBoundaryGeometry;

        [Header("Settings"), SerializeField, Tooltip("If true, underwater effect copies ocean material params each frame. Setting to false will make it cheaper but risks the underwater appearance looking wrong if the ocean material is changed.")]
        bool _copyOceanMaterialParamsEachFrame = true;

        [SerializeField, Tooltip("Assign this to a material that uses shader `Crest/Underwater/Post Process`, with the same features enabled as the ocean surface material(s).")]
        Material _underwaterPostProcessMaterial;

        [SerializeField, Tooltip(UnderwaterPostProcessUtils.tooltipFilterOceanData), Range(UnderwaterPostProcessUtils.MinFilterOceanDataValue, UnderwaterPostProcessUtils.MaxFilterOceanDataValue)]
        public int _filterOceanData = UnderwaterPostProcessUtils.DefaultFilterOceanDataValue;

        [SerializeField, Tooltip(tooltipMeniscus)]
        bool _meniscus = true;

        [Header("Debug Options")]
        [SerializeField] bool _viewPostProcessMask = false;
        [SerializeField] bool _disableOceanMask = false;
        [SerializeField, Tooltip(UnderwaterPostProcessUtils.tooltipHorizonSafetyMarginMultiplier), Range(0f, 1f)]
        float _horizonSafetyMarginMultiplier = UnderwaterPostProcessUtils.DefaultHorizonSafetyMarginMultiplier;
        // end public debug options

        private Camera _mainCamera;
        private RenderTexture _textureMask;
        private RenderTexture _depthBuffer;
        private RenderTexture _waterBoundaryGeometryTexture;
        private CommandBuffer _waterBoundaryGeometryCommandBuffer;
        private CommandBuffer _maskCommandBuffer;
        private CommandBuffer _postProcessCommandBuffer;

        private Plane[] _cameraFrustumPlanes;

        private Material _oceanMaskMaterial = null;
        private Material _waterBoundaryGeometryMaterial = null;

        private PropertyWrapperMaterial _underwaterPostProcessMaterialWrapper;

        private const string SHADER_OCEAN_MASK = "Crest/Underwater/Ocean Mask";

        UnderwaterSphericalHarmonicsData _sphericalHarmonicsData = new UnderwaterSphericalHarmonicsData();

        bool _eventsRegistered = false;
        bool _firstRender = true;

        int sp_CrestCameraColorTexture = Shader.PropertyToID("_CrestCameraColorTexture");
        int sp_CrestWaterBoundaryGeometryTexture = Shader.PropertyToID("_CrestWaterBoundaryGeometryTexture");

        // Only one camera is supported.
        public static UnderwaterPostProcess Instance { get; private set; }

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

            var maskShader = Shader.Find(SHADER_OCEAN_MASK);
            _oceanMaskMaterial = maskShader ? new Material(maskShader) : null;
            _waterBoundaryGeometryMaterial = new Material(Shader.Find("Crest/Hidden/Water Boundary Geometry"));
            if (_oceanMaskMaterial == null)
            {
                Debug.LogError($"Could not create a material with shader {SHADER_OCEAN_MASK}", this);
                return false;
            }

            // TODO: Use run-time materials only.
            return true;
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

        void Awake()
        {
            if (!InitialisedCorrectly())
            {
                enabled = false;
                return;
            }

            if (_postProcessCommandBuffer == null)
            {
                _postProcessCommandBuffer = new CommandBuffer()
                {
                    name = "Underwater Pass",
                };
            }

            // Stop the material from being saved on-edits at runtime
            _underwaterPostProcessMaterial = new Material(_underwaterPostProcessMaterial);
            _underwaterPostProcessMaterialWrapper = new PropertyWrapperMaterial(_underwaterPostProcessMaterial);
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

        void OnEnable()
        {
            Instance = this;
            _mainCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, _postProcessCommandBuffer);
        }

        void OnDisable()
        {
            Instance = null;
            _mainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _postProcessCommandBuffer);
        }

        private void ViewerMoreThan2mAboveWater(OceanRenderer ocean)
        {
            // enabled = false;
        }

        private void ViewerLessThan2mAboveWater(OceanRenderer ocean)
        {
            // enabled = true;
        }

        void OnPreRender()
        {
            XRHelpers.Update(_mainCamera);

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

                _waterBoundaryGeometryCommandBuffer = new CommandBuffer()
                {
                    name = "Water Boundary Geometry Depth",
                };
                _mainCamera.AddCommandBuffer(
                    CameraEvent.BeforeForwardOpaque,
                    _waterBoundaryGeometryCommandBuffer
                );
            }
            else
            {
                GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraFrustumPlanes);
                _maskCommandBuffer.Clear();
            }

            RenderTextureDescriptor descriptor = XRHelpers.IsRunning
                    ? XRHelpers.EyeRenderTextureDescriptor
                    : new RenderTextureDescriptor(_mainCamera.pixelWidth, _mainCamera.pixelHeight);

            InitialiseMaskTextures(descriptor, ref _textureMask, ref _depthBuffer);
            InitialiseClipSurfaceMaskTextures(descriptor, ref _waterBoundaryGeometryTexture);

            _waterBoundaryGeometryCommandBuffer.Clear();
            _waterBoundaryGeometryCommandBuffer.SetRenderTarget(_waterBoundaryGeometryTexture);
            _waterBoundaryGeometryCommandBuffer.ClearRenderTarget(true, true, Color.black);
            _waterBoundaryGeometryCommandBuffer.SetViewProjectionMatrices(_mainCamera.worldToCameraMatrix, _mainCamera.projectionMatrix);
            _waterBoundaryGeometryCommandBuffer.SetGlobalTexture(sp_CrestWaterBoundaryGeometryTexture, _waterBoundaryGeometryTexture);

            if (_waterVolumeBoundaryGeometry != null)
            {
                _waterBoundaryGeometryCommandBuffer.DrawMesh(_waterVolumeBoundaryGeometry.mesh, _waterVolumeBoundaryGeometry.transform.localToWorldMatrix, _waterBoundaryGeometryMaterial, 0, 0);
            }

            PopulateOceanMask(
                _maskCommandBuffer, _mainCamera, OceanRenderer.Instance.Tiles, _cameraFrustumPlanes,
                _textureMask, _depthBuffer,
                _oceanMaskMaterial,
                _disableOceanMask
            );

            if (OceanRenderer.Instance == null)
            {
                _eventsRegistered = false;
                return;
            }

            if (!_eventsRegistered)
            {
                OceanRenderer.Instance.ViewerLessThan2mAboveWater += ViewerLessThan2mAboveWater;
                OceanRenderer.Instance.ViewerMoreThan2mAboveWater += ViewerMoreThan2mAboveWater;
                // enabled = OceanRenderer.Instance.ViewerHeightAboveWater < 2f;
                _eventsRegistered = true;
            }

            if (GL.wireframe)
            {
                return;
            }

            descriptor.useDynamicScale = _mainCamera.allowDynamicResolution;
            // Format must be correct for CopyTexture to work. Hopefully this is good enough.
            if (_mainCamera.allowHDR) descriptor.colorFormat = RenderTextureFormat.DefaultHDR;

            var temporaryColorBuffer = RenderTexture.GetTemporary(descriptor);

            UpdatePostProcessMaterial(
                temporaryColorBuffer,
                _mainCamera,
                _underwaterPostProcessMaterialWrapper,
                _sphericalHarmonicsData,
                _meniscus,
                _firstRender || _copyOceanMaterialParamsEachFrame,
                _viewPostProcessMask,
                _horizonSafetyMarginMultiplier,
                _filterOceanData
            );

            _postProcessCommandBuffer.Clear();

            if (_mainCamera.allowMSAA)
            {
                // Use blit if MSAA is active because transparents were not included with CopyTexture.
                // Not sure if we need an MSAA resolve? Not sure how to do that...
                _postProcessCommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, temporaryColorBuffer);
            }
            else
            {
                // Copy the frame buffer as we cannot read/write at the same time. If it causes problems, replace with Blit.
                _postProcessCommandBuffer.CopyTexture(BuiltinRenderTextureType.CameraTarget, temporaryColorBuffer);
            }

            _underwaterPostProcessMaterialWrapper.SetTexture(sp_CrestCameraColorTexture, temporaryColorBuffer);

            _postProcessCommandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown, -1);

            if (_waterVolumeBoundaryGeometry == null)
            {
                _underwaterPostProcessMaterial.DisableKeyword("_GEOMETRY_EFFECT");
                _postProcessCommandBuffer.DrawProcedural(Matrix4x4.identity, _underwaterPostProcessMaterial, 0, MeshTopology.Triangles, 3, 1);
            }
            else
            {
                _underwaterPostProcessMaterial.EnableKeyword("_GEOMETRY_EFFECT");
                _postProcessCommandBuffer.DrawMesh(_waterVolumeBoundaryGeometry.mesh, _waterVolumeBoundaryGeometry.transform.localToWorldMatrix, _underwaterPostProcessMaterial,0, 1);
            }

            RenderTexture.ReleaseTemporary(temporaryColorBuffer);

            _firstRender = false;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UnderwaterPostProcess))]
    public class UnderwaterPostProcessEditor : Editor {}
#endif
}
