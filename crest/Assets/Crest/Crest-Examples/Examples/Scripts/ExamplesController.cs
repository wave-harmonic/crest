// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Examples
{
    using UnityEditor;
    using UnityEngine;

    public class ExamplesController : CustomMonoBehaviour
    {
        [SerializeField, Predicated(typeof(ExamplesController), "IsController"), DecoratedField]
        KeyCode _previous = KeyCode.Comma;

        [SerializeField, Predicated(typeof(ExamplesController), "IsController"), DecoratedField]
        KeyCode _next = KeyCode.Period;

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
            if (Input.GetKeyUp(_previous))
            {
                Previous();
            }
            else if (Input.GetKeyUp(_next))
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
