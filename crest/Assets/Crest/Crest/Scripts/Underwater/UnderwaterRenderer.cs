// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
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
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Underwater Renderer")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "underwater.html" + Internal.Constants.HELP_URL_RP)]
    public partial class UnderwaterRenderer : MonoBehaviour
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
        internal Mode _mode;

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
        float _depthFogDensityFactor = 1f;
        public float DepthFogDensityFactor => _depthFogDensityFactor;


        [Header("Geometry")]

        [SerializeField, Predicated("_mode", inverted: false, Mode.FullScreen), DecoratedField]
        [Tooltip("Mesh to use to render the underwater effect.")]
        internal MeshFilter _volumeGeometry;

        [SerializeField, Predicated("_mode", inverted: true, Mode.Portal), DecoratedField]
        [Tooltip("If enabled, the back faces of the mesh will be used instead of the front faces.")]
        bool _invertCulling = false;


        [Header("Advanced")]

        [SerializeField]
        [Tooltip("Copying params each frame ensures underwater appearance stays consistent with ocean material params. Has a small overhead so should be disabled if not needed.")]
        internal bool _copyOceanMaterialParamsEachFrame = true;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Adjusts the far plane for horizon line calculation. Helps with horizon line issue.")]
        internal float _farPlaneMultiplier = 0.68f;

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

        internal Camera _camera;
        bool _firstRender = true;

        internal bool UseStencilBufferOnMask => _mode != Mode.FullScreen;
        internal bool UseStencilBufferOnEffect => _mode == Mode.VolumeFlyThrough;

        Matrix4x4 _gpuInverseViewProjectionMatrix;
        Matrix4x4 _gpuInverseViewProjectionMatrixRight;

        // XR MP will create two instances of this class so it needs to be static to track the pass/eye.
        internal static int s_xrPassIndex = -1;

        // Use instance to denote whether this is active or not. Only one camera is supported.
        public static UnderwaterRenderer Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            Instance = null;
            s_xrPassIndex = -1;
        }

        public bool IsActive
        {
            get
            {
                if (OceanRenderer.Instance == null)
                {
                    return false;
                }

                if (!_debug._disableHeightAboveWaterOptimization && _mode == Mode.FullScreen && OceanRenderer.Instance.ViewerHeightAboveWater > 2f)
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
            if (!Validate(OceanRenderer.Instance, ValidatedHelper.DebugLog))
            {
                enabled = false;
                return;
            }
#endif

            // Setup here because it is the same across pipelines.
            if (_cameraFrustumPlanes == null)
            {
                _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(_camera);
            }

            Instance = this;

            Enable();
        }

        void OnDisable()
        {
            Disable();
            Instance = null;
        }

        void Enable()
        {
            SetupOceanMask();
            OnEnableMask();
            SetupUnderwaterEffect();
            _camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _oceanMaskCommandBuffer);
            _camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, _underwaterEffectCommandBuffer);
        }

        void Disable()
        {
            if (_oceanMaskCommandBuffer != null)
            {
                _camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _oceanMaskCommandBuffer);
            }

            if (_underwaterEffectCommandBuffer != null)
            {
                _camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _underwaterEffectCommandBuffer);
            }

            OnDisableOceanMask();
        }

        void OnPreRender()
        {
            if (!IsActive)
            {
                if (Instance != null)
                {
                    OnDisable();
                }

                return;
            }

            if (GL.wireframe)
            {
                OnDisable();
                return;
            }

            if (Instance == null)
            {
                OnEnable();
            }

            XRHelpers.Update(_camera);
            XRHelpers.UpdatePassIndex(ref s_xrPassIndex);

            // Built-in renderer does not provide these matrices.
            if (XRHelpers.IsSinglePass)
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
            if (XRHelpers.IsSinglePass)
            {
                material.SetMatrix(sp_InvViewProjection, _gpuInverseViewProjectionMatrix);
                material.SetMatrix(sp_InvViewProjectionRight, _gpuInverseViewProjectionMatrixRight);
            }
            else
            {
                material.SetMatrix(sp_InvViewProjection, _gpuInverseViewProjectionMatrix);
            }
        }
    }

#if UNITY_EDITOR
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

            return isValid;
        }
    }

    [CustomEditor(typeof(UnderwaterRenderer)), CanEditMultipleObjects]
    public class UnderwaterRendererEditor : ValidatedEditor { }
#endif
}
