using Unity.Collections;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Helper to obtain the ocean surface height at a single location. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
    public class SampleHeightHelper
    {
        Vector3 _queryPosition;

        float _minLength = 0f;

        /// <summary>
        /// Call this to prime the sampling
        /// </summary>
        /// <param name="i_queryPos">World space position to sample</param>
        /// <param name="i_minLength">The smallest length scale you are interested in. If you are sampling data for boat physics,
        /// pass in the boats width. Larger objects will ignore small wavelengths.</param>
        public void Init(Vector3 i_queryPos, float i_minLength)
        {
            _queryPosition = i_queryPos;
            _minLength = i_minLength;
        }

        /// <summary>
        /// Call this to do the query. Can be called only once after Init().
        /// </summary>
        public bool Sample(ref float o_height)
        {
            NativeArray<Vector3> queryPositions = new NativeArray<Vector3>(1, Allocator.Temp);
            queryPositions[0] = _queryPosition;
            NativeArray<float> queryResults = new NativeArray<float>(1, Allocator.Temp);
            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _minLength, queryPositions, queryResults, default, default);

            if (OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                o_height = queryResults[0] + OceanRenderer.Instance.SeaLevel;
            }

            queryResults.Dispose();
            queryPositions.Dispose();

            return OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status);
        }

        public bool Sample(ref float o_height, ref Vector3 o_normal)
        {
            NativeArray<Vector3> queryPositions = new NativeArray<Vector3>(1, Allocator.Temp);
            queryPositions[0] = _queryPosition;
            NativeArray<float> queryResults = new NativeArray<float>(1, Allocator.Temp);
            NativeArray<Vector3> queryResultNormals = new NativeArray<Vector3>(1, Allocator.Temp);

            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _minLength, queryPositions, queryResults, queryResultNormals, default);

            if (OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                o_height = queryResults[0] + OceanRenderer.Instance.SeaLevel;
                o_normal = queryResultNormals[0];
            }


            queryResultNormals.Dispose();
            queryResults.Dispose();
            queryPositions.Dispose();

            return OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status);
        }

        public bool Sample(ref float o_height, ref Vector3 o_normal, ref Vector3 o_surfaceVel)
        {
            NativeArray<Vector3> queryPositions = new NativeArray<Vector3>(1, Allocator.Temp);
            queryPositions[0] = _queryPosition;

            NativeArray<float> queryResults = new NativeArray<float>(1, Allocator.Temp);
            NativeArray<Vector3> queryResultNormals = new NativeArray<Vector3>(1, Allocator.Temp);
            NativeArray<Vector3> queryResultVels = new NativeArray<Vector3>(1, Allocator.Temp);
            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _minLength, queryPositions, queryResults, queryResultNormals, queryResultVels);

            if (OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                o_height = queryResults[0] + OceanRenderer.Instance.SeaLevel;
                o_normal = queryResultNormals[0];
                o_surfaceVel = queryResultVels[0];
            }


            queryResultVels.Dispose();
            queryResultNormals.Dispose();
            queryResults.Dispose();
            queryPositions.Dispose();

            return OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status);
        }

        public bool Sample(ref Vector3 o_displacementToPoint, ref Vector3 o_normal, ref Vector3 o_surfaceVel)
        {
            NativeArray<Vector3> queryPositions = new NativeArray<Vector3>(1, Allocator.Temp);
            queryPositions[0] = _queryPosition;

            NativeArray<Vector3> queryResults = new NativeArray<Vector3>(1, Allocator.Temp);
            NativeArray<Vector3> queryResultNormals = new NativeArray<Vector3>(1, Allocator.Temp);
            NativeArray<Vector3> queryResultVels = new NativeArray<Vector3>(1, Allocator.Temp);
            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _minLength, queryPositions, queryResults, queryResultNormals, queryResultVels);

            if (OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                o_displacementToPoint = queryResults[0];
                o_normal = queryResultNormals[0];
                o_surfaceVel = queryResultVels[0];
            }


            queryResultVels.Dispose();
            queryResultNormals.Dispose();
            queryResults.Dispose();
            queryPositions.Dispose();

            return OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status);
        }
    }

    /// <summary>
    /// Helper to obtain the ocean surface height at a single location. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
    public class SampleFlowHelper
    {
        Vector3 _queryPosition;

        float _minLength = 0f;

        /// <summary>
        /// Call this to prime the sampling
        /// </summary>
        /// <param name="i_queryPos">World space position to sample</param>
        /// <param name="i_minLength">The smallest length scale you are interested in. If you are sampling data for boat physics,
        /// pass in the boats width. Larger objects will filter out detailed flow information.</param>
        public void Init(Vector3 i_queryPos, float i_minLength)
        {
            _queryPosition = i_queryPos;
            _minLength = i_minLength;
        }

        /// <summary>
        /// Call this to do the query. Can be called only once after Init().
        /// </summary>
        public bool Sample(ref Vector2 o_flow)
        {

            NativeArray<Vector3> queryPositions = new NativeArray<Vector3>(1, Allocator.Temp);
            queryPositions[0] = _queryPosition;
            NativeArray<Vector3> queryResults = new NativeArray<Vector3>(1, Allocator.Temp);
            var status = QueryFlow.Instance.Query(GetHashCode(), _minLength, queryPositions, queryResults);

            if (QueryFlow.Instance.RetrieveSucceeded(status))
            {
                // We don't support float2 queries unfortunately, so unpack from float3
                o_flow.x = queryResults[0].x;
                o_flow.y = queryResults[0].z;
            }

            queryResults.Dispose();
            queryPositions.Dispose();


            return QueryFlow.Instance.RetrieveSucceeded(status);
        }
    }
}
