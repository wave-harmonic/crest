﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Unity.Collections;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Helper to trace a ray against the ocean surface, by sampling at a set of points along the ray and interpolating the
    /// intersection location.
    /// </summary>
    public class RayTraceHelper
    {
#if CREST_BURST_QUERY
        NativeArray<Vector3> _queryPos;
        NativeArray<Vector3> _queryResult;
#else
        Vector3[] _queryPos;
        Vector3[] _queryResult;
#endif

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

#if CREST_BURST_QUERY
            if (_queryPos.IsCreated) _queryPos.Dispose();
            _queryPos = new NativeArray<Vector3>(stepCount, Allocator.Persistent);
            if (_queryResult.IsCreated) _queryResult.Dispose();
            _queryResult = new NativeArray<Vector3>(stepCount, Allocator.Persistent);
#else
            _queryPos = new Vector3[stepCount];
            _queryResult = new Vector3[stepCount];
#endif
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

#if CREST_BURST_QUERY
            var norms = default(NativeArray<Vector3>);
            var vels = default(NativeArray<Vector3>);
            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _minLength, ref _queryPos, ref _queryResult, ref norms, ref vels, false);
#else
            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult, null, null);
#endif

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
