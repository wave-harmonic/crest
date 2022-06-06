// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if UNITY_EDITOR

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

        /// <summary>Attempts to get the scene view this camera is rendering.</summary>
        /// <returns>The scene view or null if not found.</returns>
        public static SceneView GetSceneViewFromSceneCamera(Camera camera)
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                if (sceneView.camera == camera)
                {
                    return sceneView;
                }
            }

            return null;
        }

        public static void Run2022Migration()
        {
            System.Action<ShapeFFT, ShapeFFT.Mode> setFFTMode = (input, newMode) =>
            {
                Debug.Log($"Crest: Changing Mode of {input.GetType().Name} component on GameObject {input.gameObject.name} from {input._inputMode.ToString()} to {newMode}. Click this message to highlight this GameObject.", input);
                input._inputMode = newMode;
                EditorUtility.SetDirty(input);
            };

            foreach (var fft in GameObject.FindObjectsOfType<ShapeFFT>(true))
            {
                if (fft._inputMode == ShapeFFT.Mode.Painted)
                {
                    fft.AutoDetectMode(out var newMode);

                    if (newMode != fft._inputMode)
                    {
                        setFFTMode(fft, newMode);
                    }

                    continue;
                }

                if (fft._inputMode != ShapeFFT.Mode.Global)
                {
                    // Don't touch if already set to a non-default mode
                    continue;
                }

                if (fft.AutoDetectMode(out var autoMode) && autoMode != fft._inputMode)
                {
                    setFFTMode(fft, autoMode);
                }
            }

            System.Action<RegisterLodDataInputBase, RegisterLodDataInputBase.InputMode> setInputMode = (input, newMode) =>
            {
                Debug.Log($"Crest: Changing Input Mode of {input.GetType().Name} component on GameObject {input.gameObject.name} from {input._inputMode.ToString()} to {newMode}. Click this message to highlight this GameObject.", input);
                input._inputMode = newMode;
                EditorUtility.SetDirty(input);
            };

            foreach (var input in GameObject.FindObjectsOfType<RegisterLodDataInputBase>(true))
            {
                if (input._inputMode == RegisterLodDataInputBase.InputMode.Unset)
                {
                    var newMode = input.DefaultMode;
                    input.AutoDetectMode(out newMode);

                    if (newMode != input._inputMode)
                    {
                        setInputMode(input, newMode);
                    }

                    continue;
                }

                if (input._inputMode != input.DefaultMode)
                {
                    // Don't touch if already set to a non-default mode
                    continue;
                }

                if (input.AutoDetectMode(out var autoMode) && autoMode != input._inputMode)
                {
                    setInputMode(input, autoMode);
                }
            }
        }
    }
}

#endif
