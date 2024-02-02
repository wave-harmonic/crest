// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Helper to obtain the ocean surface height at a single location per frame. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
#if CREST_BURST_QUERY
    [BurstCompile]
#endif
    public class SampleHeightHelper
    {
#if CREST_BURST_QUERY
        Vector3 _queryPos;
        Vector3 _queryResult;
        Vector3 _queryResultNormal;
        Vector3 _queryResultVel;

        // These are only ever used on the main thread, and they're filled in when they're used, so we only need one
        // global copy of them. if we had one for every sample height helper, we would spend forever creating and
        // destroying nativearrays. consider revisiting this when 22.2 is the minspec, because then nativearray becomes
        // fully unmanaged. Also, obviously revisit if their usage is ever jobified.
        static NativeArray<Vector3> _tmpQueryPos;
        static NativeArray<Vector3> _tmpQueryResult;
        static NativeArray<Vector3> _tmpQueryResultNormal;
        static NativeArray<Vector3> _tmpQueryResultVel;
        private static bool HaveRegisteredDomainUnload = false;
#else
        Vector3[] _queryPos = new Vector3[1];
        Vector3[] _queryResult = new Vector3[1];
        Vector3[] _queryResultNormal = new Vector3[1];
        Vector3[] _queryResultVel = new Vector3[1];
#endif

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
#if CREST_BURST_QUERY
            _queryPos = i_queryPos;
            if (!_tmpQueryPos.IsCreated) _tmpQueryPos = new NativeArray<Vector3>(1, Allocator.Persistent);
            if (!_tmpQueryResult.IsCreated) _tmpQueryResult = new NativeArray<Vector3>(1, Allocator.Persistent);
            if (!_tmpQueryResultNormal.IsCreated) _tmpQueryResultNormal = new NativeArray<Vector3>(1, Allocator.Persistent);
            if (!_tmpQueryResultVel.IsCreated) _tmpQueryResultVel = new NativeArray<Vector3>(1, Allocator.Persistent);
#else
            _queryPos[0] = i_queryPos;
#endif
            _minLength = i_minLength;

#if UNITY_EDITOR
            if (!allowMultipleCallsPerFrame && _lastFrame >= OceanRenderer.FrameCount)
            {
                Debug.LogWarning($"Crest: SampleHeightHelper.Init() called multiple times in one frame which is not expected. Each SampleHeightHelper object services a single height query per frame. To perform multiple queries, create multiple SampleHeightHelper objects or use the CollProvider.Query() API directly. (_lastFrame = {_lastFrame})", context);
            }
            _lastFrame = OceanRenderer.FrameCount;

#if CREST_BURST_QUERY
            // If we do this registration on every init, it wastes lots of time and garbage. But if we never do it,
            // we leak memory. So, do it once if we can't tell that we've done it before.
            if (!HaveRegisteredDomainUnload)
            {
                System.AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
                System.AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
                HaveRegisteredDomainUnload = true;
            }
#endif
#endif
        }

#if CREST_BURST_QUERY
        static void OnDomainUnload(object sender, System.EventArgs e)
        {
            _tmpQueryPos.Dispose();
            _tmpQueryResult.Dispose();
            _tmpQueryResultNormal.Dispose();
            _tmpQueryResultVel.Dispose();
        }
#endif

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

#if CREST_BURST_QUERY
            var oResultNorms = new NativeArray<Vector3>();
            var oResultVels = new NativeArray<Vector3>();
            _tmpQueryPos[0] = _queryPos;
            var status = collProvider.Query(GetHashCode(), _minLength, ref _tmpQueryPos, ref _tmpQueryResult, ref oResultNorms, ref oResultVels, false);
            _queryResult = _tmpQueryResult[0];
#else
            var status = collProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult, null, null);
#endif

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_height = OceanRenderer.Instance.SeaLevel;
                return false;
            }

#if CREST_BURST_QUERY
            o_height = _queryResult.y + OceanRenderer.Instance.SeaLevel;
#else
            o_height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;
#endif

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

#if CREST_BURST_QUERY
            NativeArray<Vector3> oResultVels = new NativeArray<Vector3>();
            _tmpQueryPos[0] = _queryPos;
            var status = collProvider.Query(GetHashCode(), _minLength, ref _tmpQueryPos, ref _tmpQueryResult, ref _tmpQueryResultNormal, ref oResultVels, true);
            _queryResult = _tmpQueryResult[0];
            _queryResultNormal = _tmpQueryResultNormal[0];
#else
            var status = collProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult, _queryResultNormal, null);
#endif

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_height = OceanRenderer.Instance.SeaLevel;
                o_normal = Vector3.up;
                return false;
            }

#if CREST_BURST_QUERY
            o_height = _queryResult.y + OceanRenderer.Instance.SeaLevel;
            o_normal = _queryResultNormal;
#else
            o_height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;
            o_normal = _queryResultNormal[0];
#endif
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

#if CREST_BURST_QUERY
            _tmpQueryPos[0] = _queryPos;
            var status = collProvider.Query(GetHashCode(), _minLength, ref _tmpQueryPos, ref _tmpQueryResult, ref _tmpQueryResultNormal, ref _tmpQueryResultVel, true);
            _queryResult = _tmpQueryResult[0];
            _queryResultNormal = _tmpQueryResultNormal[0];
            _queryResultVel = _tmpQueryResultVel[0];
#else
            var status = collProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult, _queryResultNormal, _queryResultVel);
#endif

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_height = OceanRenderer.Instance.SeaLevel;
                o_normal = Vector3.up;
                o_surfaceVel = Vector3.zero;
                return false;
            }

#if CREST_BURST_QUERY
            o_height = _queryResult.y + OceanRenderer.Instance.SeaLevel;
            o_normal = _queryResultNormal;
            o_surfaceVel = _queryResultVel;
#else
            o_height = _queryResult[0].y + OceanRenderer.Instance.SeaLevel;
            o_normal = _queryResultNormal[0];
            o_surfaceVel = _queryResultVel[0];
#endif

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

#if CREST_BURST_QUERY
            _tmpQueryPos[0] = _queryPos;
            var status = collProvider.Query(GetHashCode(), _minLength, ref _tmpQueryPos, ref _tmpQueryResult, ref _tmpQueryResultNormal, ref _tmpQueryResultVel, true);
            _queryResult = _tmpQueryResult[0];
            _queryResultNormal = _tmpQueryResultNormal[0];
            _queryResultVel = _tmpQueryResultVel[0];
#else
            var status = collProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult, _queryResultNormal, _queryResultVel);
#endif

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_displacementToPoint = Vector3.zero;
                o_normal = Vector3.up;
                o_surfaceVel = Vector3.zero;
                return false;
            }

#if CREST_BURST_QUERY
            o_displacementToPoint = _queryResult;
            o_normal = _queryResultNormal;
            o_surfaceVel = _queryResultVel;
#else
            o_displacementToPoint = _queryResult[0];
            o_normal = _queryResultNormal[0];
            o_surfaceVel = _queryResultVel[0];
#endif
            return true;
        }
    }

    /// <summary>
    /// Helper to obtain the flow data (horizontal water motion) at a single location. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
    public class SampleFlowHelper
    {
#if CREST_BURST_QUERY
        private Vector3 _queryPos;
        Vector3 _queryResult;

        // See comment on SampleHeightHelper about why these are static and more.
        private static NativeArray<Vector3> _tmpQueryPos;
        private static NativeArray<Vector3> _tmpQueryResult;
#else
        Vector3[] _queryPos = new Vector3[1];
        Vector3[] _queryResult = new Vector3[1];
#endif

        float _minLength = 0f;

        /// <summary>
        /// Call this to prime the sampling
        /// </summary>
        /// <param name="i_queryPos">World space position to sample</param>
        /// <param name="i_minLength">The smallest length scale you are interested in. If you are sampling data for boat physics,
        /// pass in the boats width. Larger objects will filter out detailed flow information.</param>
        public void Init(Vector3 i_queryPos, float i_minLength)
        {
#if CREST_BURST_QUERY
            _queryPos = i_queryPos;
            if (!_tmpQueryPos.IsCreated) _tmpQueryPos = new NativeArray<Vector3>(1, Allocator.Persistent);
            if (!_tmpQueryResult.IsCreated) _tmpQueryResult = new NativeArray<Vector3>(1, Allocator.Persistent);
#else
            _queryPos[0] = i_queryPos;
#endif
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

#if CREST_BURST_QUERY
            _tmpQueryPos[0] = _queryPos;
            var status = flowProvider.Query(GetHashCode(), _minLength, ref _tmpQueryPos, ref _tmpQueryResult);
            _queryResult = _tmpQueryResult[0];
#else
            var status = flowProvider.Query(GetHashCode(), _minLength, _queryPos, _queryResult);
#endif

            if (!flowProvider.RetrieveSucceeded(status))
            {
                o_flow = Vector2.zero;
                return false;
            }

            // We don't support float2 queries unfortunately, so unpack from float3
#if CREST_BURST_QUERY
            o_flow.x = _queryResult.x;
            o_flow.y = _queryResult.z;
#else
            o_flow.x = _queryResult[0].x;
            o_flow.y = _queryResult[0].z;
#endif

            return true;
        }
    }
}
