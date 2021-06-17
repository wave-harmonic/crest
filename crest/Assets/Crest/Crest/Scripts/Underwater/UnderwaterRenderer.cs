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
        [Range(0, LodDataMgr.MAX_LOD_COUNT - 2)]
        [Tooltip("How much to smooth ocean data such as water depth, light scattering, shadowing. Helps to smooth flickering that can occur under camera motion.")]
        public int filterOceanData = LodDataMgr.MAX_LOD_COUNT - 2;

        [Tooltip("Add a meniscus to the boundary between water and air.")]
        public bool meniscus = true;


        [Header("Advanced")]

        [Tooltip("Copying params each frame ensures underwater appearance stays consistent with ocean material params. Has a small overhead so should be disabled if not needed.")]
        public bool copyOceanMaterialParamsEachFrame = true;

        [Range(0f, 1f)]
        [Tooltip("Adjusts the far plane for horizon line calculation. Helps with horizon line issue. (Experimental)")]
        public float farPlaneMultiplier = 0.68f;

        [Tooltip("Use the old horizon safety margin multiplier to fix horizon line issues instead of the new experimental far plane multiplier.")]
        public bool useHorizonSafetyMarginMultiplier = false;

        // A magic number found after a small-amount of iteration that is used to deal with horizon-line floating-point
        // issues. It allows us to give it a small *nudge* in the right direction based on whether the camera is above
        // or below the horizon line itself already.
        [Range(0f, 1f)]
        [Tooltip("A safety margin multiplier to adjust horizon line based on camera position to avoid minor artifacts caused by floating point precision issues, the default value has been chosen based on careful experimentation.")]
        public float horizonSafetyMarginMultiplier = 0.01f;

        [Tooltip("Dynamic resolution can cause the horizon gap issue to widen. Scales the safety margin multiplier to compensate.")]
        public bool scaleSafetyMarginWithDynamicResolution = true;

        [Space(10)]
        public DebugFields debug = new DebugFields();
        [System.Serializable]
        public class DebugFields
        {
            public bool viewOceanMask = false;
            public bool disableOceanMask = false;
        }

        Camera _camera;
        bool _firstRender = true;
        static int _xrPassIndex = -1;

        // Use instance to denote whether this is active or not. Only one camera is supported.
        public static UnderwaterRenderer Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            Instance = null;
            _xrPassIndex = -1;
        }

        void OnEnable()
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            if (_cameraFrustumPlanes == null)
            {
                _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(_camera);
            }

            Enable();
            Instance = this;
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

            XRHelpers.Update(_camera);
            XRHelpers.UpdatePassIndex(ref _xrPassIndex);

            OnPreRenderOceanMask();
            OnPreRenderUnderwaterEffect();

            _firstRender = false;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UnderwaterRenderer))]
    public class UnderwaterRendererEditor : Editor {}
#endif
}
