// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    [CustomEditor( typeof( OceanRenderer ) )]
    public class OceanRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            bool oldEn = GUI.enabled;
            GUI.enabled = EditorApplication.isPlaying;
            if( GUILayout.Button( new GUIContent( "Regenerate Mesh", "Regenerate the ocean mesh (only supported at runtime/in play mode)." ) ) )
            {
                (target as OceanRenderer).RegenMesh();
            }
            GUI.enabled = oldEn;
        }
    }
}
