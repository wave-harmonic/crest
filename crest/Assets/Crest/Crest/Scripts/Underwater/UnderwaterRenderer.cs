// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Underwater Renderer. If a camera needs to go underwater it needs to have this script attached. This adds
    /// fullscreen passes and should only be used if necessary. This effect disables itself when camera is not close to
    /// the water volume.
    ///
    /// For convenience, all shader material settings are copied from the main ocean shader.
    /// </summary>
    [ExecuteDuringEditMode(ExecuteDuringEditModeAttribute.Include.None)]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Underwater Renderer")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "underwater.html" + Internal.Constants.HELP_URL_RP)]
    public partial class UnderwaterRenderer : CustomMonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        internal const string k_KeywordVolume = "CREST_WATER_VOLUME";
        internal const string k_KeywordVolume2D = "CREST_WATER_VOLUME_2D";
        internal const string k_KeywordVolumeHasBackFace = "CREST_WATER_VOLUME_HAS_BACKFACE";

        // The underlying value matches UnderwaterRenderer.EffectPass.
        // :UnderwaterRenderer.Mode
        public enum Mode
        {
            // Infinite water as a full-screen triangle.
            [InspectorNameAttribute("Full-Screen")]
            FullScreen,
            // Portal to infinite water rendered from front faces of geometry.
            Portal,
            // Volume of water rendered from front faces of geometry only. Back faces used for depth. Camera cannot see
            // the effect from within the volume.
            Volume,
            // Volume of water rendered using front faces, back faces and full-screen triangle. Camera can see effect
            // from within the volume.
            [InspectorNameAttribute("Volume (Fly-Through)")]
            VolumeFlyThrough,
        }

        [SerializeField]
        [Tooltip("Rendering mode of the underwater effect (and ocean). See the documentation for more details.")]
        public Mode _mode;

        // This adds an offset to the cascade index when sampling ocean data, in effect smoothing/blurring it. Default
        // to shifting the maximum amount (shift from lod 0 to penultimate lod - dont use last lod as it cross-fades
        // data in/out), as more filtering was better in testing.
        [SerializeField, Range(0, LodDataMgr.MAX_LOD_COUNT - 2)]
        [Tooltip("How much to smooth ocean data such as water depth, light scattering, shadowing. Helps to smooth flickering that can occur under camera motion.")]
        internal int _filterOceanData = LodDataMgr.MAX_LOD_COUNT - 2;

        [SerializeField]
        [Tooltip("Add a meniscus to the boundary between water and air.")]
        internal bool _meniscus = true;
        public bool IsMeniscusEnabled => _meniscus;

        [SerializeField, Range(0.01f, 1f)]
        [Tooltip("Scales the depth fog density. Useful to reduce the intensity of the depth fog when underwater water only.")]
        public float _depthFogDensityFactor = 1f;
        public static float DepthFogDensityFactor
        {
            get
            {
                if (Instance != null)
                {
                    return Instance._depthFogDensityFactor;
                }

                return 1f;
            }
        }


        [Header("Geometry")]

        [SerializeField, Predicated("_mode", inverted: false, Mode.FullScreen), DecoratedField]
        [Tooltip("Mesh to use to render the underwater effect.")]
        public MeshFilter _volumeGeometry;

        [SerializeField, Predicated("_mode", inverted: true, Mode.Portal), DecoratedField]
        [Tooltip("If enabled, the back faces of the mesh will be used instead of the front faces.")]
        public bool _invertCulling = false;


        [Header("Advanced")]

        [SerializeField]
        [Tooltip("Renders the underwater effect before the transparent pass (instead of after). So one can apply the underwater fog themselves to transparent objects. Cannot be changed at runtime.")]
        public bool _enableShaderAPI = false;
        public bool EnableShaderAPI { get => _enableShaderAPI; set => _enableShaderAPI = value; }

        [SerializeField]
        [Tooltip("Copying params each frame ensures underwater appearance stays consistent with ocean material params. Has a small overhead so should be disabled if not needed.")]
        public bool _copyOceanMaterialParamsEachFrame = true;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Adjusts the far plane for horizon line calculation. Helps with horizon line issue.")]
        public float _farPlaneMultiplier = 0.68f;

        [Space(10)]

        [SerializeField]
        internal DebugFields _debug = new DebugFields();
        [System.Serializable]
        public class DebugFields
        {
            public bool _viewOceanMask = false;
            public bool _disableOceanMask = false;
            public bool _viewStencil = false;
            public bool _disableHeightAboveWaterOptimization = false;
            public bool _disableArtifactCorrection = false;
        }

        /// <summary>
        /// Raised after copying the water material properties to the underwater material.
        /// </summary>
        public static System.Action<Material> AfterCopyMaterial { get; set; }

        internal Camera _camera;
        bool _firstRender = true;

        internal bool UseStencilBufferOnMask => _mode != Mode.FullScreen;
        internal bool UseStencilBufferOnEffect => _mode == Mode.VolumeFlyThrough;

        Matrix4x4 _gpuInverseViewProjectionMatrix;
        Matrix4x4 _gpuInverseViewProjectionMatrixRight;

        // XR MP will create two instances of this class so it needs to be static to track the pass/eye.
        internal static int s_xrPassIndex = -1;

#if UNITY_EDITOR
        static readonly List<Camera> s_EditorCameras = new List<Camera>();
#endif

        bool _currentEnableShaderAPI;

        // This will be the primary camera and matches OceanRenderer.Instance.ViewCamera.
        public static UnderwaterRenderer Instance { get; private set; }
        internal static Camera s_PrimaryCamera;
        static int s_InstancesCount;

        public static bool IsCullable =>
            Instance != null && s_InstancesCount == 1 && Instance._mode == Mode.FullScreen;
        public static bool SkipSurfaceSelfIntersectionFixMode =>
            s_InstancesCount > 1 || (Instance != null && Instance.IsActive && Instance._mode != Mode.FullScreen);

        SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();
        float _heightAboveWater;
        float HeightAboveWater => Instance == this ? OceanRenderer.Instance.ViewerHeightAboveWater : _heightAboveWater;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            s_PrimaryCamera = null;
            s_InstancesCount = 0;
            Instance = null;
            s_xrPassIndex = -1;

#if UNITY_EDITOR
            s_EditorCameras.Clear();
#endif
        }

        public bool IsActive
        {
            get
            {
                if (OceanRenderer.Instance == null)
                {
                    return false;
                }

                // Only run optimisation in play mode otherwise shared height above water will cause rendering to be
                // skipped for other cameras which could be confusing. This issue will also be present in play mode but
                // game view camera is always dominant camera which is less confusing.
                if (Application.isPlaying && !_debug._disableHeightAboveWaterOptimization && _mode == Mode.FullScreen && HeightAboveWater > 2f)
                {
                    return false;
                }

                return true;
            }
        }

        void OnEnable()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

#if UNITY_EDITOR
            Validate(OceanRenderer.Instance, ValidatedHelper.DebugLog);
#endif

            // Setup here because it is the same across pipelines.
            if (_cameraFrustumPlanes == null)
            {
                _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(_camera);
            }

            UpdateInstance();
            s_InstancesCount++;

            Enable();
        }

        void OnDisable()
        {
            Disable();

            if (Instance == this)
            {
                s_PrimaryCamera = null;
                Instance = null;
            }

            s_InstancesCount--;
        }

        void Enable()
        {
            SetupOceanMask();
            OnEnableMask();
            SetupUnderwaterEffect();
            AddCommandBuffers(_camera);

            _currentEnableShaderAPI = _enableShaderAPI;

#if UNITY_EDITOR
            EnableEditMode();
#endif
        }

        void Disable()
        {
            RemoveCommandBuffers(_camera);

            OnDisableMask();

#if UNITY_EDITOR
            DisableEditMode();
#endif
        }

        void LateUpdate()
        {
            UpdateInstance();

            if (Instance != this)
            {
                _sampleHeightHelper.Init(_camera.transform.position, 0f, true);
                _sampleHeightHelper.Sample(out var waterHeight);
                _heightAboveWater = _camera.transform.position.y - waterHeight;
            }

            if (_enableShaderAPI != _currentEnableShaderAPI && _underwaterEffectCommandBuffer != null)
            {
                _camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _underwaterEffectCommandBuffer);
                _camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _underwaterEffectCommandBuffer);
                _camera.AddCommandBuffer(_enableShaderAPI ? CameraEvent.BeforeForwardAlpha : CameraEvent.AfterForwardAlpha, _underwaterEffectCommandBuffer);
                _currentEnableShaderAPI = _enableShaderAPI;
#if UNITY_EDITOR
                if (Instance == this)
                {
                    DisableEditMode();
                    EnableEditMode();
                }
#endif
            }
        }

        void UpdateInstance()
        {
            if (OceanRenderer.Instance == null)
            {
                return;
            }

            if (s_PrimaryCamera != OceanRenderer.Instance.ViewCameraExcludingSceneCamera)
            {
                s_PrimaryCamera = OceanRenderer.Instance.ViewCameraExcludingSceneCamera;
                if (s_PrimaryCamera.TryGetComponent<UnderwaterRenderer>(out var instance))
                {
#if UNITY_EDITOR
                    if (Instance != null) Instance.DisableEditMode();
#endif
                    Instance = instance;
#if UNITY_EDITOR
                    Instance.EnableEditMode();
#endif
                }
            }
        }

        void AddCommandBuffers(Camera camera)
        {
            // Handle both forward and deferred.
            camera.AddCommandBuffer(CameraEvent.BeforeDepthTexture, _oceanMaskCommandBuffer);
            camera.AddCommandBuffer(CameraEvent.BeforeGBuffer, _oceanMaskCommandBuffer);
            camera.AddCommandBuffer(_enableShaderAPI ? CameraEvent.BeforeForwardAlpha : CameraEvent.AfterForwardAlpha, _underwaterEffectCommandBuffer);
        }

        void RemoveCommandBuffers(Camera camera)
        {
            if (_oceanMaskCommandBuffer != null)
            {
                // Handle both forward and deferred.
                camera.RemoveCommandBuffer(CameraEvent.BeforeDepthTexture, _oceanMaskCommandBuffer);
                camera.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, _oceanMaskCommandBuffer);
            }

            if (_underwaterEffectCommandBuffer != null)
            {
                // It could be either event registered at this point. Remove from both for safety.
                camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _underwaterEffectCommandBuffer);
                camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _underwaterEffectCommandBuffer);
            }
        }

        void UpdateOceanRendererStateForCamera()
        {
            // Support these features across multiple cameras by resetting them before each UR camera renders.
            Helpers.SetGlobalKeyword("CREST_UNDERWATER_BEFORE_TRANSPARENT", _enableShaderAPI);
        }

        void OnPreRender()
        {
#if UNITY_EDITOR
            // Do not execute when editor is not active to conserve power and prevent possible leaks.
            if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            {
                _oceanMaskCommandBuffer?.Clear();
                _underwaterEffectCommandBuffer?.Clear();
                return;
            }
#endif

            UpdateOceanRendererStateForCamera();

            if (!IsActive)
            {
                if (Instance != null)
                {
                    _oceanMaskCommandBuffer?.Clear();
                    _underwaterEffectCommandBuffer?.Clear();
                }

                return;
            }

            if (!Helpers.MaskIncludesLayer(_camera.cullingMask, OceanRenderer.Instance.Layer))
            {
                _oceanMaskCommandBuffer?.Clear();
                _underwaterEffectCommandBuffer?.Clear();
                return;
            }

#if UNITY_EDITOR
            if (GL.wireframe)
            {
                _oceanMaskCommandBuffer?.Clear();
                _underwaterEffectCommandBuffer?.Clear();
                return;
            }
#endif

            XRHelpers.Update(_camera);
            XRHelpers.UpdatePassIndex(ref s_xrPassIndex);

            // Built-in renderer does not provide these matrices.
            if (_camera.stereoEnabled && XRHelpers.IsSinglePass)
            {
                _gpuInverseViewProjectionMatrix = (GL.GetGPUProjectionMatrix(XRHelpers.LeftEyeProjectionMatrix, false) * XRHelpers.LeftEyeViewMatrix).inverse;
                _gpuInverseViewProjectionMatrixRight = (GL.GetGPUProjectionMatrix(XRHelpers.RightEyeProjectionMatrix, false) * XRHelpers.RightEyeViewMatrix).inverse;
            }
            else
            {
                _gpuInverseViewProjectionMatrix = (GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false) * _camera.worldToCameraMatrix).inverse;
            }

            OnPreRenderOceanMask();
            OnPreRenderUnderwaterEffect();

            _firstRender = false;
        }

        void SetInverseViewProjectionMatrix(Material material)
        {
            // Have to set these explicitly as the built-in transforms aren't in world-space for the blit function.
            if (_camera.stereoEnabled && XRHelpers.IsSinglePass)
            {
                material.SetMatrix(ShaderIDs.s_InvViewProjection, _gpuInverseViewProjectionMatrix);
                material.SetMatrix(ShaderIDs.s_InvViewProjectionRight, _gpuInverseViewProjectionMatrixRight);
            }
            else
            {
                material.SetMatrix(ShaderIDs.s_InvViewProjection, _gpuInverseViewProjectionMatrix);
            }
        }

        internal static UnderwaterRenderer Get(Camera camera)
        {
            UnderwaterRenderer ur;

            // If this is the primary camera then we already have the UR as a static instance.
            if (camera == s_PrimaryCamera)
            {
                ur = Instance;
            }
#if UNITY_EDITOR
            // The scene view should use the primary camera instance exclusively.
            else if (IsActiveForEditorCamera(camera, null))
            {
                ur = Instance;
            }
#endif
            else
            {
                camera.TryGetComponent(out ur);
            }

            return ur;
        }
    }

#if UNITY_EDITOR
    // Edit Mode.
    public partial class UnderwaterRenderer
    {
        void EnableEditMode()
        {
            if (Instance != this)
            {
                return;
            }

            Camera.onPreRender -= OnBeforeRender;
            Camera.onPreRender += OnBeforeRender;
        }

        void DisableEditMode()
        {
            if (Instance != this)
            {
                return;
            }

            foreach (var camera in s_EditorCameras)
            {
                // This can happen on recompile. Thankfully, command buffers will be removed for us.
                if (camera == null)
                {
                    continue;
                }

                RemoveCommandBuffers(camera);
            }

            s_EditorCameras.Clear();
            Camera.onPreRender -= OnBeforeRender;
        }

        /// <summary>
        /// Whether the effect is active for the editor only camera (eg scene view). You can check game preview cameras,
        /// but do not check game cameras.
        /// </summary>
        internal static bool IsActiveForEditorCamera(Camera camera, UnderwaterRenderer renderer)
        {
            // Skip rendering altogether if proxy plane is being used.
            if (OceanRenderer.Instance == null || (!Application.isPlaying && OceanRenderer.Instance._showOceanProxyPlane))
            {
                // These two clears will only run for built-in renderer as they'll be null for SRPs.
                if (renderer != null)
                {
                    renderer._oceanMaskCommandBuffer?.Clear();
                    renderer._underwaterEffectCommandBuffer?.Clear();
                }

                return false;
            }

            // Only use for scene and game preview cameras.
            if (camera.cameraType != CameraType.SceneView)
            {
                return false;
            }

            return true;
        }

        internal static bool IsFogEnabledForEditorCamera(Camera camera)
        {
            // Check if scene view has disabled fog rendering.
            if (camera.cameraType == CameraType.SceneView)
            {
                var sceneView = EditorHelpers.EditorHelpers.GetSceneViewFromSceneCamera(camera);
                // Skip rendering if fog is disabled or for some reason we could not find the scene view.
                if (sceneView == null || !sceneView.sceneViewState.fogEnabled)
                {
                    return false;
                }
            }

            return true;
        }

        void OnBeforeRender(Camera camera)
        {
#if UNITY_EDITOR
            // Do not execute when editor is not active to conserve power and prevent possible leaks.
            if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            {
                _oceanMaskCommandBuffer?.Clear();
                _underwaterEffectCommandBuffer?.Clear();
                return;
            }
#endif

            if (!IsActiveForEditorCamera(camera, this))
            {
                return;
            }

            if (!s_EditorCameras.Contains(camera))
            {
                s_EditorCameras.Add(camera);
                AddCommandBuffers(camera);
            }

            var oldCamera = _camera;
            _camera = camera;
            OnPreRender();
            _camera = oldCamera;
        }
    }

    // Validation.
    public partial class UnderwaterRenderer : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            if (_mode != Mode.FullScreen && _volumeGeometry == null)
            {
                showMessage
                (
                    $"<i>{_mode}</i> mode requires a <i>Mesh Filter</i> be set to <i>Volume Geometry</i>.",
                    "Change <i>Mode</i> to <i>FullScreen</i>.",
                    ValidatedHelper.MessageType.Error, this,
                    (SerializedObject so) =>
                    {
                        so.FindProperty("_mode").enumValueIndex = (int)Mode.FullScreen;
                    }
                );

                isValid = false;
            }

            if (ocean != null && ocean.OceanMaterial != null)
            {
                var material = ocean.OceanMaterial;

                if (!material.IsKeywordEnabled("_UNDERWATER_ON"))
                {
                    showMessage
                    (
                        $"<i>Underwater</i> is not enabled on material <i>{material.name}</i>. " +
                        "The underside of the ocean surface will not be rendered correctly.",
                        $"Enable <i>Underwater</i> on <i>{material.name}</i>.",
                        ValidatedHelper.MessageType.Warning, material,
                        (material) => ValidatedHelper.FixSetMaterialOptionEnabled(material, "_UNDERWATER_ON", "_Underwater", enabled: true)
                    );
                }

                if (material.GetFloat("_CullMode") == (int)CullMode.Back)
                {
                    showMessage
                    (
                        $"<i>Cull Mode</i> is set to <i>Back</i> on material <i>{material.name}</i>. " +
                        "The underside of the ocean surface will not be rendered.",
                        $"Set <i>Cull Mode</i> to <i>Off</i> (or <i>Front</i>) on <i>{material.name}</i>.",
                        ValidatedHelper.MessageType.Warning, material,
                        (material) => ValidatedHelper.FixSetMaterialIntProperty(material, "Cull Mode", "_CullMode", (int)CullMode.Off)
                    );
                }

                if (_enableShaderAPI && ocean.OceanMaterial.IsKeywordEnabled("_SUBSURFACESHALLOWCOLOUR_ON"))
                {
                    showMessage
                    (
                        "<i>Enable Shader API</i> does not support the <i>Scatter Colour Shallow</i> option",
                        $"Disable <i>Scatter Colour Shallow</i> on the ocean material <i>{material.name}</i>.",
                        ValidatedHelper.MessageType.Error, material
                    );
                }
            }

            return isValid;
        }
    }

    [CustomEditor(typeof(UnderwaterRenderer)), CanEditMultipleObjects]
    public class UnderwaterRendererEditor : CustomBaseEditor
    {
        public override void OnInspectorGUI()
        {
            var target = this.target as UnderwaterRenderer;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Scene view rendering can be enabled/disabled with the scene view fog toggle in the scene view command bar.", MessageType.Info);
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
#endif
}
