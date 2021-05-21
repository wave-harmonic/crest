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
        private CommandBuffer _maskCommandBuffer;
        private CommandBuffer _postProcessCommandBuffer;

        private Plane[] _cameraFrustumPlanes;

        Mesh _fullScreenTriangleMesh;

        private Material _oceanMaskMaterial = null;

        private PropertyWrapperMaterial _underwaterPostProcessMaterialWrapper;


        private const string SHADER_OCEAN_MASK = "Crest/Underwater/Ocean Mask";

        UnderwaterSphericalHarmonicsData _sphericalHarmonicsData = new UnderwaterSphericalHarmonicsData();

        bool _eventsRegistered = false;
        bool _firstRender = true;

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
            if (_oceanMaskMaterial == null)
            {
                Debug.LogError($"Could not create a material with shader {SHADER_OCEAN_MASK}", this);
                return false;
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
        }

        void Awake()
        {
            _mainCamera = GetComponent<Camera>();
            if (_postProcessCommandBuffer == null)
            {
                _postProcessCommandBuffer = new CommandBuffer()
                {
                    name = "Underwater Post Process",
                };
            }

            _fullScreenTriangleMesh = new Mesh
            {
                name = "Full-Screen Triangle Mesh",
                vertices = new Vector3[] {
                new Vector3(-1f, -1f, 0f),
                new Vector3(-1f,  3f, 0f),
                new Vector3( 3f, -1f, 0f),
            },
                triangles = new int[] { 0, 1, 2 },
            };
            _fullScreenTriangleMesh.UploadMeshData(true);
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
            enabled = false;
        }

        private void ViewerLessThan2mAboveWater(OceanRenderer ocean)
        {
            enabled = true;
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
            }
            else
            {
                GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraFrustumPlanes);
                _maskCommandBuffer.Clear();
            }

            {
                RenderTextureDescriptor descriptor = XRHelpers.IsRunning
                    ? XRHelpers.EyeRenderTextureDescriptor
                    : new RenderTextureDescriptor(_mainCamera.pixelWidth, _mainCamera.pixelHeight);
                InitialiseMaskTextures(descriptor, ref _textureMask, ref _depthBuffer);
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
                enabled = OceanRenderer.Instance.ViewerHeightAboveWater < 2f;
                _eventsRegistered = true;
            }



            if (GL.wireframe)
            {
                return;
            }

            UpdatePostProcessMaterial(
                _mainCamera.targetTexture,
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
            _postProcessCommandBuffer.DrawMesh(_fullScreenTriangleMesh, Matrix4x4.identity, _underwaterPostProcessMaterial);

            _firstRender = false;
        }
    }
}
