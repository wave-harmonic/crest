// KinoVision - Frame visualization utility
// https://github.com/keijiro/KinoVision

using UnityEngine;
using UnityEditor;

namespace Kino
{
    [CustomEditor(typeof(Vision))]
    public sealed class VisionEditor : Editor
    {
        // Common
        SerializedProperty _source;
        SerializedProperty _blendRatio;
        SerializedProperty _useDepthNormals;

        static GUIContent _textUseDepthNormals = new GUIContent("Use Depth Normals");

        // Depth
        SerializedProperty _depthRepeat;

        static GUIContent _textRepeat = new GUIContent("Repeat");

        // Normals
        SerializedProperty _validateNormals;

        static GUIContent _textCheckValidity = new GUIContent("Check Validity");

        // Motion vectors
        SerializedProperty _motionOverlayAmplitude;
        SerializedProperty _motionVectorsAmplitude;
        SerializedProperty _motionVectorsResolution;

        static GUIContent _textOverlayAmplitude = new GUIContent("Overlay Amplitude");
        static GUIContent _textArrowsAmplitude = new GUIContent("Arrows Amplitude");
        static GUIContent _textArrowsResolution = new GUIContent("Arrows Resolution");

        void OnEnable()
        {
            // Common
            _source = serializedObject.FindProperty("_source");
            _blendRatio = serializedObject.FindProperty("_blendRatio");
            _useDepthNormals = serializedObject.FindProperty("_useDepthNormals");

            // Depth
            _depthRepeat = serializedObject.FindProperty("_depthRepeat");

            // Normals
            _validateNormals = serializedObject.FindProperty("_validateNormals");

            // Motion vectors
            _motionOverlayAmplitude = serializedObject.FindProperty("_motionOverlayAmplitude");
            _motionVectorsAmplitude = serializedObject.FindProperty("_motionVectorsAmplitude");
            _motionVectorsResolution = serializedObject.FindProperty("_motionVectorsResolution");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_source);

            var source = (Vision.Source)_source.enumValueIndex;

            if (source == Vision.Source.Depth)
            {
                // Depth
                EditorGUILayout.PropertyField(_depthRepeat, _textRepeat);
                EditorGUILayout.PropertyField(_useDepthNormals, _textUseDepthNormals);
            }

            if (source == Vision.Source.Normals)
            {
                // Normals
                EditorGUILayout.PropertyField(_useDepthNormals, _textUseDepthNormals);
                EditorGUI.BeginDisabledGroup(_useDepthNormals.boolValue);
                EditorGUILayout.PropertyField(_validateNormals, _textCheckValidity);
                EditorGUI.EndDisabledGroup();
            }

            if (source == Vision.Source.MotionVectors)
            {
                // Motion vectors
                EditorGUILayout.PropertyField(_motionOverlayAmplitude, _textOverlayAmplitude);
                EditorGUILayout.PropertyField(_motionVectorsAmplitude, _textArrowsAmplitude);
                EditorGUILayout.PropertyField(_motionVectorsResolution, _textArrowsResolution);
            }

            EditorGUILayout.PropertyField(_blendRatio);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
