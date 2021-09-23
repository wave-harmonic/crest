// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Examples
{
    using UnityEditor;
    using UnityEngine;

    public class CycleChildren : MonoBehaviour
    {
        [SerializeField]
        KeyCode _previous = KeyCode.Comma;

        [SerializeField]
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

        void Cycle(bool isReverse = false)
        {
            var previous = transform.GetChild(transform.childCount - 1);
            foreach (Transform current in transform)
            {
                if (isReverse)
                {
                    if (current.gameObject.activeInHierarchy)
                    {
                        current.gameObject.SetActive(false);
                        previous.gameObject.SetActive(true);
                        break;
                    }
                }
                else
                {
                    if (previous.gameObject.activeInHierarchy)
                    {
                        previous.gameObject.SetActive(false);
                        current.gameObject.SetActive(true);
                        break;
                    }
                }

                previous = current;
            }
        }
    }

    [CustomEditor(typeof(CycleChildren))]
    public class CycleChildrenEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var target = this.target as CycleChildren;

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
}
