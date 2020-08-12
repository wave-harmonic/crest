// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Emits an event on state change for when either above or below the ocean surface.
/// </summary>
public class OceanSampleHeightEvents : MonoBehaviour
{
    [Tooltip("The higher the value, the more smaller waves will be ignored.")]
    [SerializeField] float _samplingLengthScale = 1f;
    [SerializeField] UnityEvent _onBelowOceanSurface = new UnityEvent();
    public UnityEvent OnBelowOceanSurface => _onBelowOceanSurface;
    [SerializeField] UnityEvent _onAboveOceanSurface = new UnityEvent();
    public UnityEvent OnAboveOceanSurface => _onAboveOceanSurface;

    // Store state
    bool _isAboveSurface = false;
    bool _isFirstUpdate = true;
    readonly SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

    void Update()
    {
        _sampleHeightHelper.Init(transform.position, 2f * _samplingLengthScale);

        if (_sampleHeightHelper.Sample(out var height))
        {
            var isAboveSurface = (transform.position.y - height) > 0;

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
                "Whenever this game object goes below or above the ocean surface, the appropriate event is fired " +
                "once per state change. It can be used to trigger audio to play underwater and much more.",
                MessageType.Info
            );
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
#endif
}
