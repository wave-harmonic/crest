// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Demarcates an AABB area where water is present in the world. If present, ocean tiles will be
    /// culled if they don't overlap any WaterBody.
    /// </summary>
    [ExecuteAlways]
    public partial class WaterBody : MonoBehaviour
    {
#pragma warning disable 414
        [Tooltip("Editor only: run validation checks on Start() to check for issues."), SerializeField]
        bool _runValidationOnStart = true;
#pragma warning restore 414

        public static List<WaterBody> WaterBodies => _waterBodies;
        static List<WaterBody> _waterBodies = new List<WaterBody>();

        public Bounds AABB { get; private set; }

        private void OnEnable()
        {
            CalculateBounds();

            _waterBodies.Add(this);
        }

        private void OnDisable()
        {
            _waterBodies.Remove(this);
        }

        private void CalculateBounds()
        {
            var bounds = new Bounds();
            bounds.center = transform.position;
            bounds.Encapsulate(transform.TransformPoint(Vector3.right / 2f + Vector3.forward / 2f));
            bounds.Encapsulate(transform.TransformPoint(Vector3.right / 2f - Vector3.forward / 2f));
            bounds.Encapsulate(transform.TransformPoint(-Vector3.right / 2f + Vector3.forward / 2f));
            bounds.Encapsulate(transform.TransformPoint(-Vector3.right / 2f - Vector3.forward / 2f));

            AABB = bounds;
        }

#if UNITY_EDITOR
        private void Start()
        {
            if (_runValidationOnStart)
            {
                Validate(OceanRenderer.Instance, ValidatedHelper.DebugLog);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            // Required as we're not normally executing in edit mode
            CalculateBounds();
            AABB.GizmosDraw();
        }
#endif
    }

#if UNITY_EDITOR
    public partial class WaterBody : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            if (Mathf.Abs(transform.lossyScale.x) < 2f && Mathf.Abs(transform.lossyScale.z) < 2f)
            {
                showMessage
                (
                    $"Water body {gameObject.name} has a very small size (the size is set by the X & Z scale of its transform). This will be a very small body of water. Is this intentional?",
                    ValidatedHelper.MessageType.Error, this
                );

                return false;
            }

            return true;
        }
    }

    [CustomEditor(typeof(WaterBody), true), CanEditMultipleObjects]
    class WaterBodyEditor : ValidatedEditor { }
#endif
}
