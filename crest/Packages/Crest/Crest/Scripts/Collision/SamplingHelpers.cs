using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Helper to obtain the ocean surface height at a single location. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
    public class SampleHeightHelper
    {
        SamplingData _samplingData = new SamplingData();
        Vector3[] _queryPos = new Vector3[1];
        Vector3[] _queryResult = new Vector3[1];
        Vector3[] _queryResultNormal = new Vector3[1];
        Vector3[] _queryResultVel = new Vector3[1];

        bool _valid = false;

        /// <summary>
        /// Call this to prime the sampling
        /// </summary>
        /// <param name="i_queryPos">World space position to sample</param>
        /// <param name="i_minLength">The smallest length scale you are interested in. If you are sampling data for boat physics,
        /// pass in the boats width. Larger objects will ignore small wavelengths.</param>
        /// <returns></returns>
        public bool Init(Vector3 i_queryPos, float i_minLength)
        {
            _queryPos[0] = i_queryPos;
            var rect = new Rect(i_queryPos.x, i_queryPos.z, 0f, 0f);
            return _valid = OceanRenderer.Instance.CollisionProvider.GetSamplingData(ref rect, i_minLength, _samplingData);
        }

        /// <summary>
        /// Call this to do the query. Can be called only once after Init().
        /// </summary>
        public bool Sample(ref float o_height)
        {
            if (!_valid)
            {
                return false;
            }

            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _samplingData, _queryPos, _queryResult, null, null);

            OceanRenderer.Instance.CollisionProvider.ReturnSamplingData(_samplingData);

            if (!OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                _valid = false;
                return false;
            }

            o_height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;

            return true;
        }

        public bool Sample(ref float o_height, ref Vector3 o_normal)
        {
            if (!_valid)
            {
                return false;
            }

            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _samplingData, _queryPos, _queryResult, _queryResultNormal, null);

            OceanRenderer.Instance.CollisionProvider.ReturnSamplingData(_samplingData);

            if (!OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                _valid = false;
                return false;
            }

            o_height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;
            o_normal = _queryResultNormal[0];

            return true;
        }

        public bool Sample(ref float o_height, ref Vector3 o_normal, ref Vector3 o_surfaceVel)
        {
            if (!_valid)
            {
                return false;
            }

            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _samplingData, _queryPos, _queryResult, _queryResultNormal, _queryResultVel);

            OceanRenderer.Instance.CollisionProvider.ReturnSamplingData(_samplingData);

            if (!OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                return false;
            }

            o_height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;
            o_normal = _queryResultNormal[0];
            o_surfaceVel = _queryResultVel[0];

            return true;
        }

        public bool Sample(ref Vector3 o_displacementToPoint, ref Vector3 o_normal, ref Vector3 o_surfaceVel)
        {
            if (!_valid)
            {
                return false;
            }

            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _samplingData, _queryPos, _queryResult, _queryResultNormal, _queryResultVel);

            OceanRenderer.Instance.CollisionProvider.ReturnSamplingData(_samplingData);

            if (!OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                return false;
            }

            o_displacementToPoint = _queryResult[0];
            o_normal = _queryResultNormal[0];
            o_surfaceVel = _queryResultVel[0];

            return true;
        }
    }

    /// <summary>
    /// Helper to obtain the ocean surface height at a single location. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
    public class SampleFlowHelper
    {
        SamplingData _samplingData = new SamplingData();
        Vector3[] _queryPos = new Vector3[1];
        Vector3[] _queryResult = new Vector3[1];

        bool _valid = false;

        /// <summary>
        /// Call this to prime the sampling
        /// </summary>
        /// <param name="i_queryPos">World space position to sample</param>
        /// <param name="i_minLength">The smallest length scale you are interested in. If you are sampling data for boat physics,
        /// pass in the boats width. Larger objects will filter out detailed flow information.</param>
        /// <returns></returns>
        public bool Init(Vector3 i_queryPos, float i_minLength)
        {
            _queryPos[0] = i_queryPos;
            var rect = new Rect(i_queryPos.x, i_queryPos.z, 0f, 0f);
            return _valid = OceanRenderer.Instance.CollisionProvider.GetSamplingData(ref rect, i_minLength, _samplingData);
        }

        /// <summary>
        /// Call this to do the query. Can be called only once after Init().
        /// </summary>
        public bool Sample(ref Vector2 o_flow)
        {
            if (!_valid)
            {
                return false;
            }

            var status = QueryFlow.Instance.Query(GetHashCode(), _samplingData, _queryPos, _queryResult);

            if (!QueryFlow.Instance.RetrieveSucceeded(status))
            {
                _valid = false;
                return false;
            }

            // We don't support float2 queries unfortunately, so unpack from float3
            o_flow.x = _queryResult[0].x;
            o_flow.y = _queryResult[0].z;

            return true;
        }
    }
}
