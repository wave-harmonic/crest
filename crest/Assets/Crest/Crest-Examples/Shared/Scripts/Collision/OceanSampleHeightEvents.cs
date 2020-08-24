// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Emits useful events (UnityEvents) based on the sampled height of the ocean surface.
/// </summary>
public class OceanSampleHeightEvents : MonoBehaviour
{
    [Header("Settings For All Events")]

    [Tooltip("The higher the value, the more smaller waves will be ignored when sampling the ocean surface.")]
    [SerializeField] float _minimumWaveLength = 1f;


    [Header("Distance From Ocean Surface")]

    [Tooltip("A normalised distance from ocean surface will be between zero and one.")]
    [SerializeField] bool _normaliseDistance = true;

    [Tooltip("The maximum distance passed to function. Always use a real distance value (not a normalised one).")]
    [SerializeField] float _maximumDistance = 100f;

    [Tooltip("Apply a curve to the distance passed to the function.")]
    [SerializeField] AnimationCurve _distanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);


    [Header("Contact With Ocean Surface")]

    [Tooltip("Minimum vertical water velocity, emits if water exceeds this velocity upwards."), SerializeField]
    float _minimumVelocity = 0.4f;

    [Tooltip("Maximum difference in height between the water surface and this position. No emission if water is too far above/below this position."), SerializeField]
    float _maximumHeightDifference = 0.5f;

    [Tooltip("Scale value for particle speed multiplier. Time axis is proportion of the Minimum Velocity setting. Value of curve at Time=1 is used if water vel exactly matches minimum emission velocity. Value of curve at Time=3 is used if water vel is 3x greater than the minimum velocity."), SerializeField]
    AnimationCurve _initalVelVsWaterVel2 = new AnimationCurve(new Keyframe[] { new Keyframe(1f, 1f), new Keyframe(4f, 5f) });

    [Tooltip("If false, script will wait until particle system is not playing before emitting again."), SerializeField]
    bool _allowMultipleSimultaneousEmissions = false;


    [Header("Events")]

    [SerializeField] UnityEvent _onBelowOceanSurface = new UnityEvent();
    public UnityEvent OnBelowOceanSurface => _onBelowOceanSurface;
    [SerializeField] UnityEvent _onAboveOceanSurface = new UnityEvent();
    public UnityEvent OnAboveOceanSurface => _onAboveOceanSurface;
    [SerializeField] FloatEvent _distanceFromOceanSurface = new FloatEvent();
    public FloatEvent DistanceFromOceanSurface => _distanceFromOceanSurface;
    [SerializeField] UnityEvent _onContactWithSurface = new UnityEvent();
    public UnityEvent OnContactWithSurface => _onContactWithSurface;

    // Store state
    bool _isAboveSurface = false;
    bool _isFirstUpdate = true;
    readonly SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

    Vector3 _lastPos = Vector3.zero;
    bool _lastPosValid = false;
    Vector3 _thisVelocity = Vector3.zero;

    // Dynamic UnityEvent definitions.
    [System.Serializable] public class FloatEvent : UnityEvent<float> { }

    void Update()
    {
        if (_lastPosValid && Time.deltaTime > 0.0001f)
        {
            _thisVelocity = (transform.position - _lastPos) / Time.deltaTime;
        }
        _lastPos = transform.position;
        _lastPosValid = true;

        _sampleHeightHelper.Init(transform.position, 2f * _minimumWaveLength);

        if (_sampleHeightHelper.Sample(out float height, out _, out var velocity))
        {
            var distance = transform.position.y - height;
            var isAboveSurface = distance > 0;

            // Has the below/above ocean surface state changed?
            if (_isAboveSurface != isAboveSurface || _isFirstUpdate)
            {
                _isAboveSurface = isAboveSurface;
                _isFirstUpdate = false;

                if (_isAboveSurface)
                {
                    _onAboveOceanSurface.Invoke();
                }
                else
                {
                    _onBelowOceanSurface.Invoke();
                }
            }

            // Save some processing when not being used.
            if (_distanceFromOceanSurface.GetPersistentEventCount() > 0)
            {
                // Normalise distance so we can use the curve.
                var distanceFromOceanSurface = _distanceCurve.Evaluate(1f - Mathf.Abs(distance) / _maximumDistance);

                // Restore raw distance if desired.
                if (!_normaliseDistance)
                {
                    distanceFromOceanSurface = _maximumDistance - distanceFromOceanSurface * _maximumDistance;
                }

                _distanceFromOceanSurface.Invoke(distanceFromOceanSurface);
            }

            {
                // Compensate for motion of this emitter object
                velocity -= _thisVelocity;

                if (Mathf.Abs(height - transform.position.y) < _maximumHeightDifference && velocity.y > _minimumVelocity)
                {
                    // We want to modify some of the particle system's properties. Unfortunately, not all properties are
                    // available to us from the UnityEvent inspector which means we have to get the particle system.
                    var startSpeedMultiplier = _initalVelVsWaterVel2.Evaluate(velocity.y / _minimumVelocity);
                    for (var i = 0; i < _onContactWithSurface.GetPersistentEventCount(); i++)
                    {
                        var particleSystem = _onContactWithSurface.GetPersistentTarget(i) as ParticleSystem;
                        if (particleSystem != null)
                        {
                            var module = particleSystem.main;
                            module.startSpeedMultiplier = startSpeedMultiplier;

                            if (!_allowMultipleSimultaneousEmissions)
                            {
                                if (particleSystem.isEmitting || particleSystem.isPlaying)
                                {
                                    // To prevent multiple emissions, we need to disable the listener.
                                    _onContactWithSurface.SetPersistentListenerState(i, UnityEventCallState.Off);
                                }
                                else
                                {
                                    // It doesn't look like we can get the state so we have to guess what user wants
                                    // when restoring.
                                    _onContactWithSurface.SetPersistentListenerState(i, UnityEventCallState.RuntimeOnly);
                                }
                            }
                        }
                    }

                    _onContactWithSurface.Invoke();
                }
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(OceanSampleHeightEvents))]
    public class OceanSampleHeightEventsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox
            (
                "For the Above/Below Ocean Surface Events, whenever this game object goes below or above the ocean " + 
                "surface, the appropriate event is fired once per state change. It can be used to trigger audio to " +
                "play underwater and much more. For the Distance From Ocean Surface event, it will pass the " +
                "distance every frame (passing normalised distance to audio volume as an example).",
                MessageType.Info
            );
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
#endif
}
