// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class WaterBody : MonoBehaviour
    {
        Bounds _bounds;

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
            _bounds = new Bounds();
            _bounds.center = transform.position;
            _bounds.Encapsulate(transform.TransformPoint(Vector3.right / 2f + Vector3.forward / 2f));
            _bounds.Encapsulate(transform.TransformPoint(Vector3.right / 2f - Vector3.forward / 2f));
            _bounds.Encapsulate(transform.TransformPoint(-Vector3.right / 2f + Vector3.forward / 2f));
            _bounds.Encapsulate(transform.TransformPoint(-Vector3.right / 2f - Vector3.forward / 2f));
        }

        public Bounds AABB { get; private set; }
    }
}
