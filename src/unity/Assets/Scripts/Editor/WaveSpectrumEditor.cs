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

            EditorGUILayout.LabelField("Amplitudes", EditorStyles.boldLabel);

            var spec = target as WaveSpectrum;

            var spAmp = serializedObject.FindProperty("_amp");
            var spAmpEn = serializedObject.FindProperty("_ampEn");
            
            for( int i = 0; i < spAmp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var spAmpEn_i = spAmpEn.GetArrayElementAtIndex(i);
                spAmpEn_i.boolValue = EditorGUILayout.Toggle(spAmpEn_i.boolValue, GUILayout.Width(15f));

                float smallWL = spec.SmallWavelength(i);
                EditorGUILayout.LabelField(string.Format("{0}", smallWL), GUILayout.Width(30f));
                var spAmp_i = spAmp.GetArrayElementAtIndex(i);
                spAmp_i.floatValue = EditorGUILayout.Slider(spAmp_i.floatValue, 0f, 1.25f);

                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();

            //bool oldEn = GUI.enabled;
            //GUI.enabled = EditorApplication.isPlaying;
            //if( GUILayout.Button( new GUIContent( "Regenerate Mesh", "Regenerate the ocean mesh (only supported at runtime/in play mode)." ) ) )
            //{
            //    (target as OceanRenderer).RegenMesh();
            //}
            //GUI.enabled = oldEn;
        }
    }
}
