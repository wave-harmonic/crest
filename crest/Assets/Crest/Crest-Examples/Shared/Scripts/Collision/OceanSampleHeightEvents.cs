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
[AddComponentMenu(Crest.Internal.Constants.MENU_PREFIX_EXAMPLE + "Ocean Sample Height Events")]
public class OceanSampleHeightEvents : MonoBehaviour
{
    /// <summary>
    /// The version of this asset. Can be used to migrate across versions. This value should
    /// only be changed when the editor upgrades the version.
    /// </summary>
    [SerializeField, HideInInspector]
#pragma warning disable 414
    int _version = 0;
#pragma warning restore 414

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


    [Header("Events")]

    [SerializeField] UnityEvent _onBelowOceanSurface = new UnityEvent();
    public UnityEvent OnBelowOceanSurface => _onBelowOceanSurface;
    [SerializeField] UnityEvent _onAboveOceanSurface = new UnityEvent();
    public UnityEvent OnAboveOceanSurface => _onAboveOceanSurface;
    [SerializeField] FloatEvent _distanceFromOceanSurface = new FloatEvent();
    public FloatEvent DistanceFromOceanSurface => _distanceFromOceanSurface;

    // Store state
    bool _isAboveSurface = false;
    bool _isFirstUpdate = true;
    readonly SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

    // Dynamic UnityEvent definitions.
    [System.Serializable] public class FloatEvent : UnityEvent<float> { }

    void Update()
    {
        _sampleHeightHelper.Init(transform.position, 2f * _minimumWaveLength);

        if (_sampleHeightHelper.Sample(out var height))
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
