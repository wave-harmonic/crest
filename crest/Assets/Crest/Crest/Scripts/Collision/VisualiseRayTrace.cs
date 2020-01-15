using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Debug draw a line trace from this gameobjects position, in this gameobjects forward direction.
    /// </summary>
    public class VisualiseRayTrace : MonoBehaviour
    {
        RayTraceHelper _rayTrace = new RayTraceHelper(50f, 2f);

        void Update()
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance.CollisionProvider == null)
            {
                return;
            }

            // Even if only a single ray trace is desired, this still must be called every frame until Trace() returns true
            _rayTrace.Init(transform.position, transform.forward);
            if (_rayTrace.Trace(out float dist))
            {
                var endPos = transform.position + transform.forward * dist;
                Debug.DrawLine(transform.position, endPos, Color.green);
                VisualiseCollisionArea.DebugDrawCross(endPos, 2f, Color.green, 0f);
            }
            else
            {
                Debug.DrawLine(transform.position, transform.position + transform.forward * 50f, Color.red);
            }
        }
    }
}
