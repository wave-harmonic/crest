using UnityEngine;

namespace Crest
{
    public class WaterBody : MonoBehaviour
    {
        public Bounds _bounds;

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

        private void OnDrawGizmosSelected()
        {
            _bounds.DebugDraw();
        }
    }
}
