// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Crest
{
    using SceneItem = KeyValuePair<string, string>;

    public class WindowCrestDashboard : EditorWindow
    {
        List<SceneItem> _scenes = new List<SceneItem> {
            new SceneItem("main", "Assets/Crest/Crest-Examples/Main/Scenes/main.unity"),
            new SceneItem("boat", "Assets/Crest/Crest-Examples/BoatDev/Scenes/boat.unity"),
            new SceneItem("threeboats", "Assets/Crest/Crest-Examples/BoatDev/Scenes/threeboats.unity"),
            new SceneItem("whirlpool", "Assets/Crest/Crest-Examples/Whirlpool/Scenes/whirlpool.unity"),
        };

        private void OnGUI()
        {
            OnGUILoadSceneButtons();
        }

        void OnGUILoadSceneButtons()
        {
            EditorGUILayout.BeginHorizontal();

            foreach (var kvp in _scenes)
            {
                if (GUILayout.Button(kvp.Key))
                {
                    EditorSceneManager.OpenScene(kvp.Value);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        [MenuItem("Window/Crest/Dashboard")]
        public static void ShowWindow()
        {
            GetWindow<WindowCrestDashboard>("Crest Dashboard");
        }
    }
}
