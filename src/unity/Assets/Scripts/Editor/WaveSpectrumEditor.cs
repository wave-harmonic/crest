// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    [CustomEditor( typeof(WaveSpectrum) )]
    public class WaveSpectrumEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.LabelField("Spectrum", EditorStyles.boldLabel);

            var spec = target as WaveSpectrum;

            var spPower = serializedObject.FindProperty("_power");
            var spEnabled = serializedObject.FindProperty("_powerEnabled");
            
            for( int i = 0; i < spPower.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var spEnabled_i = spEnabled.GetArrayElementAtIndex(i);
                spEnabled_i.boolValue = EditorGUILayout.Toggle(spEnabled_i.boolValue, GUILayout.Width(15f));

                float smallWL = spec.SmallWavelength(i);
                EditorGUILayout.LabelField(string.Format("{0}", smallWL), GUILayout.Width(30f));
                var spPower_i = spPower.GetArrayElementAtIndex(i);
                float pow = 4f;
                spPower_i.floatValue = Mathf.Pow(GUILayout.HorizontalSlider(Mathf.Pow(spPower_i.floatValue, 1f / pow), 0f, 4f), pow);

                spPower_i.floatValue = EditorGUILayout.DelayedFloatField(spPower_i.floatValue, GUILayout.Width(60f));

                EditorGUILayout.EndHorizontal();
            }


            EditorGUILayout.LabelField("Empirical Spectrums", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Wind speed", GUILayout.Width(70f));

            var spWindSpeed = serializedObject.FindProperty("_windSpeed");
            float spd_kmh = spWindSpeed.floatValue * 3.6f;
            spd_kmh = EditorGUILayout.Slider(spd_kmh, 0f, 60f);
            spWindSpeed.floatValue = spd_kmh / 3.6f;

            if (GUILayout.Button("Phillips"))
            {
                spec.ApplyPhillipsSpectrum(spWindSpeed.floatValue);
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
