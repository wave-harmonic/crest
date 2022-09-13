// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crest
{
    /// <summary>
    /// Helper to obtain the ocean surface height at a single location per frame. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
    [BurstCompile]
    public class SampleHeightHelper
    {
        Vector3 _queryPos;
        private Vector3 _queryResult;
        private Vector3 _queryResultNormal;
        private Vector3 _queryResultVel;

        //these are only ever used on the main thread, and they're filled in when they're used, so we only need one
        //global copy of them. if we had one for every sample height helper, we would spend forever creating and
        //destroying nativearrays. consider revisiting this when 22.2 is the minspec, because then nativearray becomes
        //fully unmanaged.

        //also, obviously revisit if their usage is ever jobified.
        static NativeArray<Vector3> _tmpqueryPos;
        static NativeArray<Vector3> _tmpqueryResult;
        static NativeArray<Vector3> _tmpqueryResultNormal;
        static NativeArray<Vector3> _tmpqueryResultVel;
        private static bool HaveRegisteredDomainUnload = false;

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
            _queryPos = i_queryPos;
            if (!_tmpqueryPos.IsCreated)
                _tmpqueryPos = new NativeArray<Vector3>(1, Allocator.Persistent);

            if (!_tmpqueryResult.IsCreated)
            {
                _tmpqueryResult = new NativeArray<Vector3>(1, Allocator.Persistent);
            }

            if (!_tmpqueryResultNormal.IsCreated)
            {
                _tmpqueryResultNormal = new NativeArray<Vector3>(1, Allocator.Persistent);
            }

            if (!_tmpqueryResultVel.IsCreated)
            {
                _tmpqueryResultVel = new NativeArray<Vector3>(1, Allocator.Persistent);
            }

            _minLength = i_minLength;

#if UNITY_EDITOR
            if (!allowMultipleCallsPerFrame && _lastFrame >= OceanRenderer.FrameCount)
            {
                Debug.LogWarning($"Crest: SampleHeightHelper.Init() called multiple times in one frame which is not expected. Each SampleHeightHelper object services a single height query per frame. To perform multiple queries, create multiple SampleHeightHelper objects or use the CollProvider.Query() API directly. (_lastFrame = {_lastFrame})", context);
            }
            _lastFrame = OceanRenderer.FrameCount;

            //if we do this registration on every init, it wastes lots of time and garbage. but if we never do it,
            //we leak memory. so, do it once if we can't tell that we've done it before.
            if (!HaveRegisteredDomainUnload)
            {
                AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
                AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
                HaveRegisteredDomainUnload = true;
            }
#endif
        }

        private static void OnDomainUnload(object sender, EventArgs e)
        {
            _tmpqueryPos.Dispose();
            _tmpqueryResult.Dispose();
            _tmpqueryResultNormal.Dispose();
            _tmpqueryResultVel.Dispose();
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

            if (!DoQueryAndRetrieveSucceeded(out o_height, collProvider, _minLength, _queryPos, out var queryResult, GetHashCode())) return false;

            return true;
        }


        private static bool DoQueryAndRetrieveSucceeded(out float o_height, ICollProvider collProvider, float minLength, Vector3 queryPos, out Vector3 queryResult, int selfHashCode)
        {
            var oResultNorms = new NativeArray<Vector3>();
            var oResultVels = new NativeArray<Vector3>();
            _tmpqueryPos[0] = queryPos;

            var status = collProvider.Query(selfHashCode,
                minLength,
                ref _tmpqueryPos,
                ref _tmpqueryResult,
                ref oResultNorms,
                ref oResultVels,
                false);

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_height = OceanRenderer.Instance.SeaLevel;
                queryResult = _tmpqueryResult[0];
                return false;
            }

            queryResult = _tmpqueryResult[0];

            o_height = queryResult.y + OceanRenderer.Instance.SeaLevel;

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

            NativeArray<Vector3> oResultVels = new NativeArray<Vector3>();
            _tmpqueryPos[0] = _queryPos;
            var status = collProvider.Query(GetHashCode(),
                _minLength,
                ref _tmpqueryPos,
                ref _tmpqueryResult,
                ref _tmpqueryResultNormal,
                ref oResultVels,
                true);
            _queryResult = _tmpqueryResult[0];
            _queryResultNormal = _tmpqueryResultNormal[0];

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_height = OceanRenderer.Instance.SeaLevel;
                o_normal = Vector3.up;
                return false;
            }

            o_height = _queryResult.y + OceanRenderer.Instance.SeaLevel;
            o_normal = _queryResultNormal;

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

            _tmpqueryPos[0] = _queryPos;

            var status = collProvider.Query(GetHashCode(),
                _minLength,
                ref _tmpqueryPos,
                ref _tmpqueryResult,
                ref _tmpqueryResultNormal,
                ref _tmpqueryResultVel,
                true);

            _queryResult = _tmpqueryResult[0];
            _queryResultNormal = _tmpqueryResultNormal[0];
            _queryResultVel = _tmpqueryResultVel[0];

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_height = OceanRenderer.Instance.SeaLevel;
                o_normal = Vector3.up;
                o_surfaceVel = Vector3.zero;
                return false;
            }

            o_height = _queryResult.y + OceanRenderer.Instance.SeaLevel;
            o_normal = _queryResultNormal;
            o_surfaceVel = _queryResultVel;

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

            _tmpqueryPos[0] = _queryPos;
            var status = collProvider.Query(GetHashCode(),
                _minLength,
                ref _tmpqueryPos,
                ref _tmpqueryResult,
                ref _tmpqueryResultNormal,
                ref _tmpqueryResultVel,
                true);
            _queryResult = _tmpqueryResult[0];
            _queryResultNormal = _tmpqueryResultNormal[0];
            _queryResultVel = _tmpqueryResultVel[0];

            if (!collProvider.RetrieveSucceeded(status))
            {
                o_displacementToPoint = Vector3.zero;
                o_normal = Vector3.up;
                o_surfaceVel = Vector3.zero;
                return false;
            }

            o_displacementToPoint = _queryResult;
            o_normal = _queryResultNormal;
            o_surfaceVel = _queryResultVel;

            return true;
        }
    }

    /// <summary>
    /// Helper to obtain the flow data (horizontal water motion) at a single location. This is not particularly efficient to sample a single height,
    /// but is a fairly common case.
    /// </summary>
    public class SampleFlowHelper
    {
        private Vector3 _queryPos;
        Vector3 _queryResult;

        //see comment on SampleHeightHelper about why these are static and more
        private static NativeArray<Vector3> _tmpQueryPos;
        private static NativeArray<Vector3> _tmpQueryResult;

        float _minLength = 0f;

        /// <summary>
        /// Call this to prime the sampling
        /// </summary>
        /// <param name="i_queryPos">World space position to sample</param>
        /// <param name="i_minLength">The smallest length scale you are interested in. If you are sampling data for boat physics,
        /// pass in the boats width. Larger objects will filter out detailed flow information.</param>
        public void Init(Vector3 i_queryPos, float i_minLength)
        {
            _queryPos = i_queryPos;
            if (!_tmpQueryPos.IsCreated)
            {
                _tmpQueryPos = new NativeArray<Vector3>(1, Allocator.Persistent);
            }

            if (!_tmpQueryResult.IsCreated)
            {
                _tmpQueryResult = new NativeArray<Vector3>(1, Allocator.Persistent);
            }
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
            _tmpQueryPos[0] = _queryPos;
            var status = flowProvider.Query(GetHashCode(), _minLength, ref _tmpQueryPos, ref _tmpQueryResult);
            _queryResult = _tmpQueryResult[0];
            if (!flowProvider.RetrieveSucceeded(status))
            {
                o_flow = Vector2.zero;
                return false;
            }

            // We don't support float2 queries unfortunately, so unpack from float3
            o_flow.x = _queryResult.x;
            o_flow.y = _queryResult.z;

            return true;
        }
    }
}
