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

            EditorGUILayout.Space();
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
                spPower_i.floatValue = Mathf.Pow(GUILayout.HorizontalSlider(Mathf.Pow(spPower_i.floatValue, 1f / pow), 0f, 6f), pow);

                spPower_i.floatValue = EditorGUILayout.DelayedFloatField(spPower_i.floatValue, GUILayout.Width(60f));

                EditorGUILayout.EndHorizontal();
            }


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Empirical Spectrums", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            var spWindSpeed = serializedObject.FindProperty("_windSpeed");
            float spd_kmh = spWindSpeed.floatValue * 3.6f;
            EditorGUILayout.LabelField("Wind speed", GUILayout.Width(70f));
            spd_kmh = EditorGUILayout.Slider(spd_kmh, 0f, 60f);
            spWindSpeed.floatValue = spd_kmh / 3.6f;
            EditorGUILayout.EndHorizontal();


            // descriptions from this very useful paper: https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf

            if (GUILayout.Button(new GUIContent("Phillips", "Base of modern parametric wave spectra")))
            {
                spec.ApplyPhillipsSpectrum(spWindSpeed.floatValue);
            }

            if (GUILayout.Button(new GUIContent("Pierson-Moskowitz", "Fully developed sea with infinite fetch")))
            {
                spec.ApplyPiersonMoskowitzSpectrum(spWindSpeed.floatValue);
            }

            EditorGUILayout.BeginHorizontal();
            var spFetch = serializedObject.FindProperty("_fetch");
            spFetch.floatValue = EditorGUILayout.Slider("Fetch", spFetch.floatValue, 0f, 1000000f);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button(new GUIContent("JONSWAP", "Fetch limited sea where waves continue to grow")))
            {
                spec.ApplyJONSWAPSpectrum(spWindSpeed.floatValue);
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
}
