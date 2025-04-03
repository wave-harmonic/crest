// FloatingOrigin.cs
// Written by Peter Stirling
// 11 November 2010
// Uploaded to Unify Community Wiki on 11 November 2010
// Updated to Unity 5.x particle system by Tony Lovell 14 January, 2016
// fix to ensure ALL particles get moved by Tony Lovell 8 September, 2016
//
// URL: http://wiki.unity3d.com/index.php/Floating_Origin
//
// Adjusted to suit Crest by Huw Bowles:
// * Recommend a power-of-2 threshold - this avoids pops in the ocean geometry.
// * Move origin when x or z exceeds threshold (not the dist from origin exceeds threshold). This is required to support the previous point.
// * Notify ocean when origin moves
// * Misc style adjustments to align with Crest
// * Optional lists of components that can be provided to avoid evil FindObjectsOfType() calls
//
// NOTE 1: This thread discusses usage of this script: https://github.com/huwb/crest-oceanrender/issues/150
// NOTE 2: Of particular note - any world space texturing should have a period that tiles perfectly across the teleport distance, otherwise visible
// pops will occur. Example - set teleport radius to 16384m, and set normal map scale to 16m which divides evenly into the teleport radius.

using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    public interface IShiftingOrigin
    {
        /// <summary>
        /// Set a new origin. This is equivalent to subtracting the new origin position from any world position state.
        /// </summary>
        void SetOrigin(Vector3 newOrigin);
    }

    /// <summary>
    /// This script translates all objects in the world to keep the camera near the origin in order to prevent spatial jittering due to limited
    /// floating-point precision. The script detects when the camera is further than 'threshold' units from the origin in one or more axes, at which
    /// point it moves everything so that the camera is back at the origin. There is also an option to disable physics beyond a certain point. This
    /// script should normally be attached to the viewpoint, typically the main camera.
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Shifting Origin")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "open-worlds.html" + Internal.Constants.HELP_URL_RP + "#floating-origin")]
    public class ShiftingOrigin : CustomMonoBehaviour
    {
        const string k_Keyword = "CREST_FLOATING_ORIGIN";

        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        const float k_MinimumThreshold = 2048f;

        [Tooltip("Use a power of 2 to avoid pops in ocean surface geometry."), Min(k_MinimumThreshold), SerializeField]
        public float _threshold = 16384f;
        [Tooltip("Set to zero to disable."), SerializeField]
        float _physicsThreshold = 1000.0f;

        [SerializeField] float _defaultSleepThreshold = 0.14f;

        [Tooltip("Optionally provide a list of transforms to avoid doing a FindObjectsOfType() call."), SerializeField]
        Transform[] _overrideTransformList = null;
        [Tooltip("Optionally provide a list of particle systems to avoid doing a FindObjectsOfType() call."), SerializeField]
        ParticleSystem[] _overrideParticleSystemList = null;
        [Tooltip("Optionally provide a list of rigidbodies to avoid doing a FindObjectsOfType() call."), SerializeField]
        Rigidbody[] _overrideRigidbodyList = null;
        [Tooltip("Optionally provide a list of Gerstner components to avoid doing a FindObjectsOfType() call."), SerializeField]
        ShapeGerstnerBatched[] _overrideGerstnerList = null;

        [Space(10)]

        [SerializeField]
        internal DebugFields _debug = new DebugFields();

        [System.Serializable]
        internal partial class DebugFields
        {
            [Tooltip("Pause editor on origin shift.")]
            public bool _pauseOnShift;
            [Tooltip("Pause editor before origin shift. When it meets the threshold it postpones the shift to the next frame and pauses this frame.")]
            public bool _pauseBeforeShift;
            [Tooltip("Log to console on origin shift.")]
            public bool _logOnShift;
            internal bool _isCapturing;
            internal bool _shiftNextX;
            internal bool _shiftNextY;
            internal bool _shiftNextZ;
        }

        int _lastUpdateFrame;

        ParticleSystem.Particle[] _particleBuffer = null;

        public static readonly int sp_CrestFloatingOriginOffset = Shader.PropertyToID("_CrestFloatingOriginOffset");

        Vector3 _originOffset;

        public static Vector3 TeleportOriginThisFrame { get; private set; }
        public static bool HasTeleportedThisFrame { get; private set; }

        IEnumerator Start()
        {
            while (true)
            {
                // NOTE: Will not work in batch mode:
                // https://docs.unity3d.com/Manual/CLIBatchmodeCoroutines.html
                yield return Helpers.WaitForEndOfFrame;

#if CREST_DEBUG
                if (_debug._isCapturing)
                {
                    ExternalGPUProfiler.EndGPUCapture();
                    _debug._isCapturing = false;
                }
#endif

#if UNITY_EDITOR
                if (_debug._pauseOnShift && HasTeleportedThisFrame)
                {
                    UnityEditor.EditorApplication.isPaused = true;
                }
#endif

                // Cannot clear at start of FixedUpdate (without extra logic) as it can run multiple times or not at all.
                TeleportOriginThisFrame = Vector3.zero;
                HasTeleportedThisFrame = false;
            }
        }

        void FixedUpdate()
        {
            // Run FixedUpdate no more than once per frame.
            if (_lastUpdateFrame == Time.frameCount)
            {
                return;
            }

            _lastUpdateFrame = Time.frameCount;

            var newOrigin = Vector3.zero;

            if (Mathf.Abs(transform.position.x) > _threshold)
            {
#if UNITY_EDITOR
                if (_debug._pauseBeforeShift && !_debug._shiftNextX)
                {
                    _debug._shiftNextX = true;
                    UnityEditor.EditorApplication.isPaused = true;
                }
                else
#endif // UNITY_EDITOR
                {
                    // Teleport by threshold value intervals to avoid popping.
                    newOrigin.x += Mathf.Floor(transform.position.x / _threshold) * _threshold;
#if UNITY_EDITOR
                    _debug._shiftNextX = false;
#endif
                }
            }

            if (Mathf.Abs(transform.position.y) > _threshold)
            {
#if UNITY_EDITOR
                if (_debug._pauseBeforeShift && !_debug._shiftNextY)
                {
                    _debug._shiftNextY = true;
                    UnityEditor.EditorApplication.isPaused = true;
                }
                else
#endif // UNITY_EDITOR
                {
                    // Teleport by threshold value intervals to avoid popping.
                    newOrigin.y += Mathf.Floor(transform.position.y / _threshold) * _threshold;
#if UNITY_EDITOR
                    _debug._shiftNextY = false;
#endif
                }
            }

            if (Mathf.Abs(transform.position.z) > _threshold)
            {
#if UNITY_EDITOR
                if (_debug._pauseBeforeShift && !_debug._shiftNextZ)
                {
                    _debug._shiftNextZ = true;
                    UnityEditor.EditorApplication.isPaused = true;
                }
                else
#endif // UNITY_EDITOR
                {
                    // Teleport by threshold value intervals to avoid popping.
                    newOrigin.z += Mathf.Floor(transform.position.z / _threshold) * _threshold;
#if UNITY_EDITOR
                    _debug._shiftNextZ = false;
#endif
                }
            }

            if (newOrigin != Vector3.zero)
            {
                MoveOrigin(newOrigin);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            // Must be power of two to avoid popping.
            _threshold = Mathf.Pow(2f, Mathf.Round(Mathf.Log(_threshold, 2f)));
            _threshold = Mathf.Max(_threshold, k_MinimumThreshold);
        }
#endif

        void OnEnable()
        {
            Shader.EnableKeyword(k_Keyword);
        }

        void OnDisable()
        {
            Shader.DisableKeyword(k_Keyword);
            Shader.SetGlobalVector(sp_CrestFloatingOriginOffset, Vector3.zero);
        }

        void MoveOrigin(Vector3 newOrigin)
        {
            if (_debug._logOnShift)
            {
                Debug.Log($"Crest.FloatingOrigin.MoveOrigin({newOrigin})");
            }

#if CREST_DEBUG
            if (_debug._captureOnShift && ExternalGPUProfiler.IsAttached())
            {
                ExternalGPUProfiler.BeginGPUCapture();
                _debug._isCapturing = true;
            }
#endif

            TeleportOriginThisFrame = newOrigin;
            HasTeleportedThisFrame = true;

            MoveOriginTransforms(newOrigin);
            MoveOriginParticles(newOrigin);
            MoveOriginOcean(newOrigin);

            MoveOriginDisablePhysics();
        }

        /// <summary>
        /// Move transforms to recenter around new origin
        /// </summary>
        void MoveOriginTransforms(Vector3 newOrigin)
        {
            var transforms = (_overrideTransformList != null && _overrideTransformList.Length > 0) ? _overrideTransformList : Helpers.FindObjectsByType<Transform>();
            foreach (var t in transforms)
            {
                if (t.parent == null)
                {
                    t.position -= newOrigin;
                }
            }
        }

        /// <summary>
        /// Move all particles that are simulated in world space
        /// </summary>
        void MoveOriginParticles(Vector3 newOrigin)
        {
            var pss = (_overrideParticleSystemList != null && _overrideParticleSystemList.Length > 0) ? _overrideParticleSystemList : Helpers.FindObjectsByType<ParticleSystem>();
            foreach (var sys in pss)
            {
                if (sys.main.simulationSpace != ParticleSystemSimulationSpace.World) continue;

                var particlesNeeded = sys.main.maxParticles;
                if (particlesNeeded <= 0) continue;

                var wasPaused = sys.isPaused;
                var wasPlaying = sys.isPlaying;

                if (!wasPaused)
                {
                    sys.Pause();
                }

                // Ensure a sufficiently large array in which to store the particles
                if (_particleBuffer == null || _particleBuffer.Length < particlesNeeded)
                {
                    _particleBuffer = new ParticleSystem.Particle[particlesNeeded];
                }

                // Update the particles
                var num = sys.GetParticles(_particleBuffer);
                for (var i = 0; i < num; i++)
                {
                    _particleBuffer[i].position -= newOrigin;
                }
                sys.SetParticles(_particleBuffer, num);

                if (wasPlaying)
                {
                    sys.Play();
                }
            }
        }

        /// <summary>
        /// Notify ocean of origin shift
        /// </summary>
        void MoveOriginOcean(Vector3 newOrigin)
        {
            if (OceanRenderer.Instance)
            {
                OceanRenderer.Instance._lodTransform.SetOrigin(newOrigin);

                _originOffset -= newOrigin;
                Shader.SetGlobalVector(sp_CrestFloatingOriginOffset, _originOffset);

                var fos = OceanRenderer.Instance.GetComponentsInChildren<IShiftingOrigin>();
                foreach (var fo in fos)
                {
                    fo.SetOrigin(newOrigin);
                }

                // Gerstner components
                var gerstners = _overrideGerstnerList != null && _overrideGerstnerList.Length > 0 ? _overrideGerstnerList : Helpers.FindObjectsByType<ShapeGerstnerBatched>();
                foreach (var gerstner in gerstners)
                {
                    gerstner.SetOrigin(newOrigin);
                }
            }
        }

        /// <summary>
        /// Disable physics outside radius
        /// </summary>
        void MoveOriginDisablePhysics()
        {
            if (_physicsThreshold > 0f)
            {
                var physicsThreshold2 = _physicsThreshold * _physicsThreshold;
                var rbs = (_overrideRigidbodyList != null && _overrideRigidbodyList.Length > 0) ? _overrideRigidbodyList : Helpers.FindObjectsByType<Rigidbody>();
                foreach (var rb in rbs)
                {
                    if (rb.gameObject.transform.position.sqrMagnitude > physicsThreshold2)
                    {
                        rb.sleepThreshold = float.MaxValue;
                    }
                    else
                    {
                        rb.sleepThreshold = _defaultSleepThreshold;
                    }
                }
            }
        }

#if CREST_DEBUG
        // Extra options only if developer mode enabled.
        internal partial class DebugFields
        {
            [Header("Developer Only")]

            [Tooltip("GPU capture using external tool on origin shift. Uses experimental API so keep developer only.")]
            public bool _captureOnShift;
            [Tooltip("Whether to update the X axis.")]
            public bool _updateX = false;
            [Tooltip("Whether to update the Y axis.")]
            public bool _updateY = false;
            [Tooltip("Whether to update the Z axis.")]
            public bool _updateZ = false;
            [Tooltip("Adds an extra offset so transform can skip thresholds (eg with value of 900 and for 512 threshold, transform will be at 1024).")]
            public Vector3 _positionOffset = Vector3.zero;
            [Tooltip("How far from the threshold will the transform be?")]
            public Vector3 _offsetFromThreshold = Vector3.one * 0.01f;
            [Tooltip("Teleport distance when using \"Teleport\" button.")]
            public Vector3 _teleport;
        }
#endif
    }

}

#if UNITY_EDITOR
namespace Crest.CrestEditor
{
    using UnityEditor;

    [CustomEditor(typeof(ShiftingOrigin))]
    class ShiftingOriginEditor : CustomBaseEditor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox
            (
                "It is recommended to read the documentation on this component (click the (?) button) if you want to " +
                "change the default threshold value to avoid popping on world shifts.",
                MessageType.Info
            );
            EditorGUILayout.Space();

            base.OnInspectorGUI();

            var target = this.target as ShiftingOrigin;

#if CREST_DEBUG
            if (GUILayout.Button("Teleport"))
            {
                target.transform.position += target._debug._teleport;
            }

            UpdatePosition(target);
#endif
        }

#if CREST_DEBUG
        void UpdatePosition(ShiftingOrigin target)
        {
            if (Application.isPlaying)
            {
                return;
            }
            var position = target.transform.position;

            if (target._debug._updateX)
            {
                position.x = Mathf.Floor(target._debug._positionOffset.x / target._threshold) * target._threshold +
                    target._threshold - target._debug._offsetFromThreshold.x;
            }

            if (target._debug._updateY)
            {
                position.y = Mathf.Floor(target._debug._positionOffset.y / target._threshold) * target._threshold +
                    target._threshold - target._debug._offsetFromThreshold.y;
            }

            if (target._debug._updateZ)
            {
                position.z = Mathf.Floor(target._debug._positionOffset.z / target._threshold) * target._threshold +
                    target._threshold - target._debug._offsetFromThreshold.z;
            }

            target.transform.position = position;
        }
#endif
    }
}
#endif
