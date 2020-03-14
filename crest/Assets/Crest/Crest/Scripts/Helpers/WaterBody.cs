// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class WaterBody : MonoBehaviour
    {
#pragma warning disable 414
        [Tooltip("Editor only: run validation checks on Start() to check for issues."), SerializeField]
        bool _runValidationOnStart = true;
#pragma warning restore 414

        public Bounds AABB { get; private set; }

        private void OnEnable()
        {
            if (OceanRenderer.Instance == null) return;

            CalculateBounds();

            OceanRenderer.Instance.RegisterWaterBody(this);
        }

        private void OnDisable()
        {
            if (OceanRenderer.Instance == null) return;

            OceanRenderer.Instance.UnregisterWaterBody(this);
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
                Validate(OceanRenderer.Instance);
            }
        }

        public void Validate(OceanRenderer ocean)
        {
            if (transform.lossyScale.magnitude < 2f)
            {
                Debug.LogWarning($"Water body {gameObject.name} has a very small size (the size is set by the scale of its transform). This will be a very small body of water. Is this intentional?", this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            // Required as we're not normally executing in edit mode
            CalculateBounds();
            AABB.GizmosDraw();
            Gizmos.color = Color.white;
        }
#endif
    }
}
