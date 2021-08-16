// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Helper to obtain the ocean surface height at a single location per frame. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
    public class SampleHeightHelper
    {
        Vector3[] _queryPos = new Vector3[1];
        Vector3[] _queryResult = new Vector3[1];
        Vector3[] _queryResultNormal = new Vector3[1];
        Vector3[] _queryResultVel = new Vector3[1];

        float _minLength = 0f;

#if UNITY_EDITOR
        int _lastFrame = -1;
#endif

        /// <summary>
        /// Call this to prime the sampling. The SampleHeightHelper is good for one query per frame - if it is called multiple times in one frame
        /// it will throw a warning. Calls from FixedUpdate are an exception to this - pass true as the last argument to disable the warning.
        /// </summary>
        /// <param name="i_queryPos">World space position to sample</param>
        /// <param name="i_minLength">The smallest length scale you are interested in. If you are sampling data for boat physics,
        /// pass in the boats width. Larger objects will ignore small wavelengths.</param>
        /// <param name="allowMultipleCallsPerFrame">Pass true if calling from FixedUpdate(). This will omit a warning when there on multipled-FixedUpdate frames.</param>
        public void Init(Vector3 i_queryPos, float i_minLength = 0f, bool allowMultipleCallsPerFrame = false, Object context = null)
        {
            _queryPos[0] = i_queryPos;
            _minLength = i_minLength;

#if UNITY_EDITOR
            if (!allowMultipleCallsPerFrame && _lastFrame >= OceanRenderer.FrameCount)
            {
                Debug.LogWarning($"Crest: SampleHeightHelper.Init() called multiple times in one frame which is not expected. Each SampleHeightHelper object services a single height query per frame. To perform multiple queries, create multiple SampleHeightHelper objects or use the CollProvider.Query() API directly. (_lastFrame = {_lastFrame})", context);
            }
            _lastFrame = OceanRenderer.FrameCount;
#endif
        }

        /// <summary>
        /// Call this to do the query. Can be called only once after Init().
        /// </summary>
        public bool Sample(out float o_height)
        {
            var collProvider = OceanRenderer.Instance?.CollisionProvider;
            if (collProvider == null)
            {
                o_height = 0f;
                return false;
            }

            var status = collProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult, null, null);

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_height = OceanRenderer.Instance.SeaLevel;
                return false;
            }

            o_height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;

            return true;
        }

        public bool Sample(out float o_height, out Vector3 o_normal)
        {
            var collProvider = OceanRenderer.Instance?.CollisionProvider;
            if (collProvider == null)
            {
                o_height = 0f;
                o_normal = Vector3.up;
                return false;
            }

            var status = collProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult, _queryResultNormal, null);

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_height = OceanRenderer.Instance.SeaLevel;
                o_normal = Vector3.up;
                return false;
            }

            o_height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;
            o_normal = _queryResultNormal[0];

            return true;
        }

        public bool Sample(out float o_height, out Vector3 o_normal, out Vector3 o_surfaceVel)
        {
            var collProvider = OceanRenderer.Instance?.CollisionProvider;
            if (collProvider == null)
            {
                o_height = 0f;
                o_normal = Vector3.up;
                o_surfaceVel = Vector3.zero;
                return false;
            }

            var status = collProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult, _queryResultNormal, _queryResultVel);

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_height = OceanRenderer.Instance.SeaLevel;
                o_normal = Vector3.up;
                o_surfaceVel = Vector3.zero;
                return false;
            }

            o_height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;
            o_normal = _queryResultNormal[0];
            o_surfaceVel = _queryResultVel[0];

            return true;
        }

        public bool Sample(out Vector3 o_displacementToPoint, out Vector3 o_normal, out Vector3 o_surfaceVel)
        {
            var collProvider = OceanRenderer.Instance?.CollisionProvider;
            if (collProvider == null)
            {
                o_displacementToPoint = Vector3.zero;
                o_normal = Vector3.up;
                o_surfaceVel = Vector3.zero;
                return false;
            }
            var status = collProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult, _queryResultNormal, _queryResultVel);

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_displacementToPoint = Vector3.zero;
                o_normal = Vector3.up;
                o_surfaceVel = Vector3.zero;
                return false;
            }

            o_displacementToPoint = _queryResult[0];
            o_normal = _queryResultNormal[0];
            o_surfaceVel = _queryResultVel[0];

            return true;
        }
    }

    /// <summary>
    /// Helper to obtain the flow data (horizontal water motion) at a single location. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
    public class SampleFlowHelper
    {
        Vector3[] _queryPos = new Vector3[1];
        Vector3[] _queryResult = new Vector3[1];

        float _minLength = 0f;

        /// <summary>
        /// Call this to prime the sampling
        /// </summary>
        /// <param name="i_queryPos">World space position to sample</param>
        /// <param name="i_minLength">The smallest length scale you are interested in. If you are sampling data for boat physics,
        /// pass in the boats width. Larger objects will filter out detailed flow information.</param>
        public void Init(Vector3 i_queryPos, float i_minLength)
        {
            _queryPos[0] = i_queryPos;
            _minLength = i_minLength;
        }

        /// <summary>
        /// Call this to do the query. Can be called only once after Init().
        /// </summary>
        public bool Sample(out Vector2 o_flow)
        {
            var flowProvider = OceanRenderer.Instance?.FlowProvider;
            if (flowProvider == null)
            {
                o_flow = Vector2.zero;
                return false;
            }
            var status = flowProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult);

            if (!flowProvider.RetrieveSucceeded(status))
            {
                o_flow = Vector2.zero;
                return false;
            }

            // We don't support float2 queries unfortunately, so unpack from float3
            o_flow.x = _queryResult[0].x;
            o_flow.y = _queryResult[0].z;

            return true;
        }
    }
}
