// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if CREST_UNITY_INPUT && ENABLE_INPUT_SYSTEM
#define INPUT_SYSTEM_ENABLED
#endif

namespace Crest.Examples
{
    using UnityEditor;
    using UnityEngine;

    public class ExamplesController : CustomMonoBehaviour
    {
        public void Previous() => Cycle(true);
        public void Next() => Cycle(false);

        void Start()
        {
            var hasActive = false;
            foreach (Transform current in transform)
            {
                if (current.gameObject.activeSelf)
                {
                    hasActive = true;
                    break;
                }
            }

            if (!hasActive)
            {
                transform.GetChild(0).gameObject.SetActive(true);
            }
        }

        void Update()
        {
#if INPUT_SYSTEM_ENABLED
            if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.N].wasReleasedThisFrame)
#else
            if (Input.GetKeyUp(KeyCode.N))
#endif
            {
                Previous();
            }
#if INPUT_SYSTEM_ENABLED
            else if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.M].wasReleasedThisFrame)
#else
            else if (Input.GetKeyUp(KeyCode.M))
#endif
            {
                Next();
            }
        }

        // Called by Predicated attribute. Signature must not be changed.
        bool IsController(Component component)
        {
            return transform.parent == null || !transform.parent.TryGetComponent<ExamplesController>(out _);
        }

        internal void Cycle(bool isReverse = false)
        {
            var hasActive = false;
            var previous = transform.GetChild(transform.childCount - 1);
            foreach (Transform current in transform)
            {
                if (isReverse)
                {
                    if (current.gameObject.activeInHierarchy)
                    {
                        current.gameObject.SetActive(false);
                        previous.gameObject.SetActive(true);
#if UNITY_EDITOR
                        Selection.activeGameObject = previous.gameObject;
#endif
                        hasActive = true;
                        break;
                    }
                }
                else
                {
                    if (previous.gameObject.activeInHierarchy)
                    {
                        previous.gameObject.SetActive(false);
                        current.gameObject.SetActive(true);
#if UNITY_EDITOR
                        Selection.activeGameObject = current.gameObject;
#endif
                        hasActive = true;
                        break;
                    }
                }

                previous = current;
            }

            if (!hasActive)
            {
                transform.GetChild(0).gameObject.SetActive(true);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ExamplesController))]
    public class ExamplesControllerEditor : CustomBaseEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var target = this.target as ExamplesController;

            if (target.transform.parent != null && target.transform.parent.TryGetComponent<ExamplesController>(out var parent))
            {
                target = parent;
            }

            if (GUILayout.Button("Previous"))
            {
                target.Previous();
            }

            if (GUILayout.Button("Next"))
            {
                target.Next();
            }
        }
    }
#endif
}
