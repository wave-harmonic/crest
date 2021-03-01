// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Crest.EditorHelpers
{
    /// <summary>
    /// Provides general helper functions for the editor.
    /// </summary>
    public static class EditorHelpers
    {
        static EditorWindow _lastGameOrSceneEditorWindow = null;

        /// <summary>
        /// Returns the scene view camera if the scene view is focused.
        /// </summary>
        public static Camera GetActiveSceneViewCamera()
        {
            Camera sceneCamera = null;

            if (EditorWindow.focusedWindow != null && (EditorWindow.focusedWindow.titleContent.text == "Scene" ||
                EditorWindow.focusedWindow.titleContent.text == "Game"))
            {
                _lastGameOrSceneEditorWindow = EditorWindow.focusedWindow;
            }

            // If scene view is focused, use its camera. This code is slightly ropey but seems to work ok enough.
            if (_lastGameOrSceneEditorWindow != null && _lastGameOrSceneEditorWindow.titleContent.text == "Scene")
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null && !EditorApplication.isPlaying)
                {
                    sceneCamera = sceneView.camera;
                }
            }

            return sceneCamera;
        }

        // https://docs.unity3d.com/Manual/ClassIDReference.html
        const int MONO_BEHAVIOUR_CLASS_ID = 114;

        // Adapted from: https://github.com/Unity-Technologies/com.unity.probuilder/blob/9c3775/Editor/EditorCore/EditorUtility.cs#L572
        // I do not know why the ProBuilder package can directly access AnnotationUtility in 2019+ when we cannot.
        static MethodInfo s_setIconEnabled;
        static MethodInfo SetIconEnabled => s_setIconEnabled = s_setIconEnabled ??
            Assembly.GetAssembly(typeof(Editor))
            ?.GetType("UnityEditor.AnnotationUtility")
            ?.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic, null,
                new System.Type[] { typeof(int), typeof(string), typeof(int) }, null);

        public static void SetGizmoIconEnabled(System.Type scriptType, bool isEnabled)
        {
            SetIconEnabled?.Invoke(null, new object[] { MONO_BEHAVIOUR_CLASS_ID, scriptType.Name, isEnabled ? 1 : 0 });
        }
    }
}

#endif
