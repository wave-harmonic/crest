using Unity.Collections;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Debug draw crosses in an area around the GameObject on the water surface.
    /// </summary>
    public class VisualiseCollisionArea : MonoBehaviour
    {
        [SerializeField]
        float _objectWidth = 0f;

        NativeArray<float> _resultHeights;

        static float s_radius = 5f;
        static readonly int s_steps = 10;

        NativeArray<Vector3> _samplePositions;

        void Awake()
        {
            _resultHeights = new NativeArray<float>(s_steps * s_steps, Allocator.Persistent);
            _samplePositions = new NativeArray<Vector3>(s_steps * s_steps, Allocator.Persistent);
        }

        void OnDestroy()
        {
            _resultHeights.Dispose();
            _samplePositions.Dispose();
        }

        void Update()
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance.CollisionProvider == null)
            {
                return;
            }

            var collProvider = OceanRenderer.Instance.CollisionProvider;

            for (int i = 0; i < s_steps; i++)
            {
                for (int j = 0; j < s_steps; j++)
                {
                    Vector3 samplePosition = new Vector3(((i + 0.5f) - s_steps / 2f) * s_radius, 0f, ((j + 0.5f) - s_steps / 2f) * s_radius);
                    samplePosition.x += transform.position.x;
                    samplePosition.z += transform.position.z;
                    _samplePositions[j * s_steps + i] = samplePosition;
                }
            }

            if (collProvider.RetrieveSucceeded(collProvider.Query(GetHashCode(), _objectWidth, _samplePositions, _resultHeights, default, default)))
            {
                for (int i = 0; i < s_steps; i++)
                {
                    for (int j = 0; j < s_steps; j++)
                    {
                        var result = _samplePositions[j * s_steps + i];
                        result.y = _resultHeights[j * s_steps + i];

                        DebugDrawCross(result, 1f, Color.green);
                    }
                }
            }
        }

        public static void DebugDrawCross(Vector3 pos, float r, Color col, float duration = 0f)
        {
            Debug.DrawLine(pos - Vector3.up * r, pos + Vector3.up * r, col, duration);
            Debug.DrawLine(pos - Vector3.right * r, pos + Vector3.right * r, col, duration);
            Debug.DrawLine(pos - Vector3.forward * r, pos + Vector3.forward * r, col, duration);
        }
    }
}
