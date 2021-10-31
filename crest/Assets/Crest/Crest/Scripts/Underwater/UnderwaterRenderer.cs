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
            public bool _disableHeightAboveWaterOptimization = false;
            public bool _disableArtifactCorrection = false;
        }

        Camera _camera;
        bool _firstRender = true;

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

        internal bool IsActive
        {
            get
            {
                if (OceanRenderer.Instance == null)
                {
                    return false;
                }

                if (!_debug._disableHeightAboveWaterOptimization && OceanRenderer.Instance.ViewerHeightAboveWater > 2f)
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
            SetupUnderwaterEffect();
            _camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, _underwaterEffectCommandBuffer);
            _camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _oceanMaskCommandBuffer);
        }

        void Disable()
        {
            _camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _underwaterEffectCommandBuffer);
            _camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _oceanMaskCommandBuffer);
        }

        void OnPreRender()
        {
            if (!IsActive)
            {
                OnDisable();
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

            // Intentionally left empty. Here for downstream.

            return isValid;
        }
    }

    [CustomEditor(typeof(UnderwaterRenderer)), CanEditMultipleObjects]
    public class UnderwaterRendererEditor : ValidatedEditor { }
#endif
}
