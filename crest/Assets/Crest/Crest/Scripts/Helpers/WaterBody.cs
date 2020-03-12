using UnityEngine;

namespace Crest
{
    public class WaterBody : MonoBehaviour
    {
        public float _radius = 50f;

        private void OnEnable()
        {
            if (OceanRenderer.Instance == null) return;

            OceanRenderer.Instance.RegisterWaterBody(this);
        }

        private void OnDisable()
        {
            if (OceanRenderer.Instance == null) return;

            OceanRenderer.Instance.UnregisterWaterBody(this);
        }

        public Bounds Bounds => new Bounds(transform.position, _radius * Vector3.one);
    }
}
