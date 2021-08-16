// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Helper to trace a ray against the ocean surface, by sampling at a set of points along the ray and interpolating the
    /// intersection location.
    /// </summary>
    public class RayTraceHelper
    {
        Vector3[] _queryPos;
        Vector3[] _queryResult;

        float _rayLength;
        float _rayStepSize;

        float _minLength = 0f;

        /// <summary>
        /// Constructor. The length of the ray and the step size must be given here. The smaller the step size, the greater the accuracy.
        /// </summary>
        public RayTraceHelper(float rayLength, float rayStepSize = 2f)
        {
            _rayLength = rayLength;
            _rayStepSize = rayStepSize;

            Debug.Assert(_rayLength > 0f);
            Debug.Assert(_rayStepSize > 0f);

            var stepCount = Mathf.CeilToInt(_rayLength / _rayStepSize) + 1;

            var maxStepCount = 128;
            if (stepCount > maxStepCount)
            {
                stepCount = maxStepCount;
                _rayStepSize = _rayLength / (stepCount - 1f);
                Debug.LogWarning($"Crest: RayTraceHelper: ray steps exceed maximum ({maxStepCount}), step size increased to {_rayStepSize} to reduce step count.");
            }

            _queryPos = new Vector3[stepCount];
            _queryResult = new Vector3[stepCount];
        }

        /// <summary>
        /// Call this each frame to initialize the trace.
        /// </summary>
        /// <param name="i_rayOrigin">World space position of ray origin</param>
        /// <param name="i_rayDirection">World space ray direction</param>
        public void Init(Vector3 i_rayOrigin, Vector3 i_rayDirection)
        {
            for (int i = 0; i < _queryPos.Length; i++)
            {
                _queryPos[i] = i_rayOrigin + i * _rayStepSize * i_rayDirection;
            }

            var rect = new Rect();
            rect.xMin = Mathf.Min(_queryPos[0].x, _queryPos[_queryPos.Length - 1].x);
            rect.yMin = Mathf.Min(_queryPos[0].z, _queryPos[_queryPos.Length - 1].z);
            rect.xMax = Mathf.Max(_queryPos[0].x, _queryPos[_queryPos.Length - 1].x);
            rect.yMax = Mathf.Max(_queryPos[0].z, _queryPos[_queryPos.Length - 1].z);

            // Waves go max double along min length. Thats too much - only allow half of a wave per step.
            _minLength = _rayStepSize * 4f;
        }

        /// <summary>
        /// Call this once each frame to do the query, after calling Init().
        /// </summary>
        /// <param name="o_distance">The distance along the ray to the first intersection with the water surface.</param>
        /// <returns>True if the results have come back from the GPU, and if the ray intersects the water surface.</returns>
        public bool Trace(out float o_distance)
        {
            o_distance = -1f;

            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult, null, null);

            if (!OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                return false;
            }

            // Now that data is available, compare the height of the water to the height of each point of the ray. If
            // the ray crosses the surface, the distance to the intersection is interpolated from the heights.
            for (int i = 1; i < _queryPos.Length; i++)
            {
                var height0 = _queryResult[i - 1].y + OceanRenderer.Instance.SeaLevel - _queryPos[i - 1].y;
                var height1 = _queryResult[i].y + OceanRenderer.Instance.SeaLevel - _queryPos[i].y;

                if (Mathf.Sign(height0) != Mathf.Sign(height1))
                {
                    var prop = Mathf.Abs(height0) / (Mathf.Abs(height0) + Mathf.Abs(height1));
                    o_distance = (i - 1 + prop) * _rayStepSize;
                    break;
                }
            }

            return o_distance >= 0f;
        }
    }
}
