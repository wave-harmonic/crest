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
        float[] _queryResult = new float[1];
        int _instanceID = -1;

        /// <summary>
        /// Call this to prime the sampling. Should be called once per frame.
        /// </summary>
        public void Init(int instanceID, Vector3 queryPos, float minLength)
        {
            _instanceID = instanceID;
            _queryPos[0] = queryPos;
            var rect = new Rect(queryPos.x, queryPos.z, 0f, 0f);
            OceanRenderer.Instance.CollisionProvider.GetSamplingData(ref rect, minLength, _samplingData);
        }

        /// <summary>
        /// Call this to do the query. Can be called only once after Init().
        /// </summary>
        public bool Sample(ref float height)
        {
            var status = OceanRenderer.Instance.CollisionProvider.Query(_instanceID, _samplingData, _queryPos, null, _queryResult, null);

            OceanRenderer.Instance.CollisionProvider.ReturnSamplingData(_samplingData);

            if (!OceanRenderer.Instance.CollisionProvider.RetrieveSucceeded(status))
            {
                return false;
            }

            height = _queryResult[0];

            return true;
        }
    }
}
