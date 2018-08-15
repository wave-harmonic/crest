// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    [CustomEditor( typeof(OceanWaveSpectrum) )]
    public class OceanWaveSpectrumEditor : Editor
    {
        private static GUIStyle ToggleButtonStyleNormal = null;
        private static GUIStyle ToggleButtonStyleToggled = null;

        static float _windSpeed = 10f;
        static float _fetch = 500000f;

        static bool _applyPhillipsSpectrum = false;
        static bool _applyPiersonMoskowitzSpectrum = false;
        static bool _applyJONSWAPSpectrum = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var showAdvancedControls = serializedObject.FindProperty("_showAdvancedControls").boolValue;

            // preamble - styles for toggle buttons. this code and the below was based off the useful info provided by user Lasse here:
            // https://gamedev.stackexchange.com/questions/98920/how-do-i-create-a-toggle-button-in-unity-inspector
            if (ToggleButtonStyleNormal == null)
            {
                ToggleButtonStyleNormal = "Button";
                ToggleButtonStyleToggled = new GUIStyle(ToggleButtonStyleNormal);
                ToggleButtonStyleToggled.normal.background = ToggleButtonStyleToggled.active.background;
            }

            EditorGUILayout.Space();

            var spDisabled = serializedObject.FindProperty("_powerDisabled");
            EditorGUILayout.BeginHorizontal();
            bool allEnabled = true;
            for( int i = 0; i < spDisabled.arraySize; i++)
            {
                if (spDisabled.GetArrayElementAtIndex(i).boolValue) allEnabled = false;
            }
            bool toggle = allEnabled;
            if (toggle != EditorGUILayout.Toggle(toggle, GUILayout.Width(13f)))
            {
                for (int i = 0; i < spDisabled.arraySize; i++)
                {
                    spDisabled.GetArrayElementAtIndex(i).boolValue = toggle;
                }
            }
            EditorGUILayout.LabelField("Spectrum", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            var spec = target as OceanWaveSpectrum;

            var spPower = serializedObject.FindProperty("_powerLog");
            var spChopScales = serializedObject.FindProperty("_chopScales");
            var spGravScales = serializedObject.FindProperty("_gravityScales");

            for( int i = 0; i < spPower.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var spDisabled_i = spDisabled.GetArrayElementAtIndex(i);
                spDisabled_i.boolValue = !EditorGUILayout.Toggle(!spDisabled_i.boolValue, GUILayout.Width(15f));

                float smallWL = OceanWaveSpectrum.SmallWavelength(i);
                var spPower_i = spPower.GetArrayElementAtIndex(i);

                if (showAdvancedControls)
                {
                    EditorGUILayout.LabelField(string.Format("{0}", smallWL), EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Slider(spPower_i, OceanWaveSpectrum.MIN_POWER_LOG, OceanWaveSpectrum.MAX_POWER_LOG, "    Power");
                }
                else
                {
                    EditorGUILayout.LabelField(string.Format("{0}", smallWL), GUILayout.Width(30f));
                    spPower_i.floatValue = GUILayout.HorizontalSlider(spPower_i.floatValue, OceanWaveSpectrum.MIN_POWER_LOG, OceanWaveSpectrum.MAX_POWER_LOG);
                    EditorGUILayout.EndHorizontal();
                }

                if (showAdvancedControls)
                {
                    EditorGUILayout.Slider(spChopScales.GetArrayElementAtIndex(i), 0f, 4f, "    Chop Scale");
                    EditorGUILayout.Slider(spGravScales.GetArrayElementAtIndex(i), 0f, 4f, "    Grav Scale");
                }
            }


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Empirical Spectra", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            float spd_kmh = _windSpeed * 3.6f;
            EditorGUILayout.LabelField("Wind speed (km/h)", GUILayout.Width(120f));
            spd_kmh = EditorGUILayout.Slider(spd_kmh, 0f, 60f);
            _windSpeed = spd_kmh / 3.6f;
            EditorGUILayout.EndHorizontal();


            // descriptions from this very useful paper: https://hal.archives-ouvertes.fr/file/index/docid/307938/filename/frechot_realistic_simulation_of_ocean_surface_using_wave_spectra.pdf

            if (GUILayout.Button(new GUIContent("Phillips", "Base of modern parametric wave spectra"), _applyPhillipsSpectrum ? ToggleButtonStyleToggled : ToggleButtonStyleNormal))
            {
                _applyPhillipsSpectrum = !_applyPhillipsSpectrum;
            }
            if (_applyPhillipsSpectrum)
            {
                _applyJONSWAPSpectrum = _applyPiersonMoskowitzSpectrum = false;

                Undo.RecordObject(this, "Apply Phillips Spectrum");

                spec.ApplyPhillipsSpectrum(_windSpeed);
            }

            if (GUILayout.Button(new GUIContent("Pierson-Moskowitz", "Fully developed sea with infinite fetch"), _applyPiersonMoskowitzSpectrum ? ToggleButtonStyleToggled : ToggleButtonStyleNormal))
            {
                _applyPiersonMoskowitzSpectrum = !_applyPiersonMoskowitzSpectrum;
            }
            if (_applyPiersonMoskowitzSpectrum)
            {
                _applyPhillipsSpectrum = _applyJONSWAPSpectrum = false;

                Undo.RecordObject(this, "Apply Pierson-Moskowitz Spectrum");

                spec.ApplyPiersonMoskowitzSpectrum(_windSpeed);
            }

            _fetch = EditorGUILayout.Slider("Fetch", _fetch, 0f, 1000000f);


            if (GUILayout.Button(new GUIContent("JONSWAP", "Fetch limited sea where waves continue to grow"), _applyJONSWAPSpectrum ? ToggleButtonStyleToggled : ToggleButtonStyleNormal))
            {
                _applyJONSWAPSpectrum = !_applyJONSWAPSpectrum;
            }
            if (_applyJONSWAPSpectrum)
            {
                _applyPhillipsSpectrum = _applyPiersonMoskowitzSpectrum = false;

                Undo.RecordObject(this, "Apply JONSWAP Spectrum");

                spec.ApplyJONSWAPSpectrum(_windSpeed, _fetch);
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
}
