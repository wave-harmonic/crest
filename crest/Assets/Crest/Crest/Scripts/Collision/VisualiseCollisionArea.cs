using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Draw crosses in an area around the GameObject on the water surface
    /// </summary>
    public class VisualiseCollisionArea : MonoBehaviour
    {
        [SerializeField]
        float _objectWidth = 0f;

        SamplingData _samplingData = new SamplingData();

        float[] _resultHeights = new float[s_steps * s_steps];

        static float s_radius = 5f;
        static readonly int s_steps = 10;

        void Update()
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance.CollisionProvider == null)
            {
                return;
            }

            var collProvider = OceanRenderer.Instance.CollisionProvider;
            var thisRect = new Rect(transform.position.x - s_radius * s_steps / 2f, transform.position.z - s_radius * s_steps / 2f, s_radius * s_steps / 2f, s_radius * s_steps / 2f);
            if (!collProvider.GetSamplingData(ref thisRect, _objectWidth, _samplingData))
            {
                return;
            }

            var samplePositions = new Vector3[s_steps * s_steps];
            for (int i = 0; i < s_steps; i++)
            {
                for (int j = 0; j < s_steps; j++)
                {
                    samplePositions[j * s_steps + i] = new Vector3(((i + 0.5f) - s_steps / 2f) * s_radius, 0f, ((j + 0.5f) - s_steps / 2f) * s_radius);
                    samplePositions[j * s_steps + i].x += transform.position.x;
                    samplePositions[j * s_steps + i].z += transform.position.z;
                }
            }

            if (collProvider.RetrieveSucceeded(collProvider.Query(GetHashCode(), _samplingData, samplePositions, _resultHeights, null, null)))
            {
                for (int i = 0; i < s_steps; i++)
                {
                    for (int j = 0; j < s_steps; j++)
                    {
                        var result = samplePositions[j * s_steps + i];
                        result.y = _resultHeights[j * s_steps + i];

                        DebugDrawCross(result, 1f, Color.green);
                    }
                }
            }

            collProvider.ReturnSamplingData(_samplingData);
        }

        public static void DebugDrawCross(Vector3 pos, float r, Color col, float duration = 0f)
        {
            Debug.DrawLine(pos - Vector3.up * r, pos + Vector3.up * r, col, duration);
            Debug.DrawLine(pos - Vector3.right * r, pos + Vector3.right * r, col, duration);
            Debug.DrawLine(pos - Vector3.forward * r, pos + Vector3.forward * r, col, duration);
        }
    }
}
