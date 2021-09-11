// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.SceneManagement;

namespace Crest
{
    public class TestCameraChange : MonoBehaviour
    {
        [Tooltip("This will populate on start if left empty. Use 'C' to cycle cameras.")]
        [SerializeField] List<Camera> _cameras = new List<Camera>();

        void OnEnable()
        {
            if (_cameras.Count > 0)
            {
                return;
            }

            // We want all active and inactive cameras. Yes, this appears to be the best way to do this...
            foreach (var gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var camera in gameObject.GetComponentsInChildren<Camera>(true))
                {
                    if (camera.hideFlags == HideFlags.NotEditable || camera.hideFlags == HideFlags.HideAndDontSave)
                    {
                        continue;
                    }

                    _cameras.Add(camera);
                }
            }
        }

        void Update()
        {
            // New input system works even when game view is not focused.
            if (!Application.isFocused)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.cKey.wasReleasedThisFrame)
#else
            if (Input.GetKeyUp(KeyCode.C))
#endif
            {
                // Cycle camera
                Camera previous = _cameras[_cameras.Count - 1];
                foreach (var current in _cameras)
                {
                    if (previous.gameObject.activeInHierarchy)
                    {
                        previous.gameObject.SetActive(false);
                        current.gameObject.SetActive(true);
                        break;
                    }
                    previous = current;
                }
            }
        }
    }
}
