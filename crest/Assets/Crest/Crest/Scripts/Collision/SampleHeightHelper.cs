using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Helper to obtain the ocean surface height at a single location. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
    class SampleHeightHelper
    {
        SamplingData _samplingData = new SamplingData();
        Vector3[] _queryPos = new Vector3[1];
        Vector3[] _queryResult = new Vector3[1];
        Vector3[] _queryResultNormal = new Vector3[1];
        Vector3[] _queryResultVel = new Vector3[1];

        bool _valid = false;

        /// <summary>
        /// Call this to prime the sampling. Should be called once per frame.
        /// </summary>
        public void Init(Vector3 queryPos, float minLength)
        {
            _queryPos[0] = queryPos;
            var rect = new Rect(queryPos.x, queryPos.z, 0f, 0f);
            _valid = OceanRenderer.Instance.CollisionProvider.GetSamplingData(ref rect, minLength, _samplingData);
        }

        /// <summary>
        /// Call this to do the query. Can be called only once after Init().
        /// </summary>
        public bool Sample(ref float height)
        {
            if (!_valid)
            {
                return false;
            }

            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _samplingData, _queryPos, null, _queryResult, null);

            OceanRenderer.Instance.CollisionProvider.ReturnSamplingData(_samplingData);

            if (!OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                return false;
            }

            height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;

            return true;
        }

        public bool Sample(ref float height, ref Vector3 normal)
        {
            if (!_valid)
            {
                return false;
            }

            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _samplingData, _queryPos, _queryPos, _queryResult, _queryResultNormal);

            OceanRenderer.Instance.CollisionProvider.ReturnSamplingData(_samplingData);

            if (!OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                return false;
            }

            height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;
            normal = _queryResultNormal[0];

            return true;
        }

        public bool Sample(ref float height, ref Vector3 normal, ref Vector3 surfaceVel)
        {
            if (!_valid)
            {
                return false;
            }

            var status = OceanRenderer.Instance.CollisionProvider.Query(GetHashCode(), _samplingData, _queryPos, _queryPos, _queryResult, _queryResultNormal, _queryResultVel);

            OceanRenderer.Instance.CollisionProvider.ReturnSamplingData(_samplingData);

            if (!OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                return false;
            }

            height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;
            normal = _queryResultNormal[0];
            surfaceVel = _queryResultVel[0];

            return true;
        }
    }
}
