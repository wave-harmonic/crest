// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
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

        public static LayerMask LayerMaskField(string label, LayerMask layerMask)
        {
            // Adapted from: http://answers.unity.com/answers/1387522/view.html
            var temporary = EditorGUILayout.MaskField(
                label,
                UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask),
                UnityEditorInternal.InternalEditorUtility.layers);
            return UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(temporary);
        }
    }
}

#endif
