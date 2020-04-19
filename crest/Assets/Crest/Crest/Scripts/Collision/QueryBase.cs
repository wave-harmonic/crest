// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Potential improvements
// - Half return values
// - Half minGridSize

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Provides heights and other physical data about the water surface. Works by uploading query positions to GPU and computing
    /// the data and then transferring back the results asynchronously. An exception to this is water surface velocities - these can
    /// not be computed on the GPU and are instead computed on the CPU by retaining last frames' query results and computing finite diffs.
    /// </summary>
    public abstract class QueryBase : MonoBehaviour
    {
        protected int _kernelHandle;

        protected abstract string QueryShaderName { get; }
        protected abstract string QueryKernelName { get; }

        const int s_maxRequests = 4;
        const int s_maxGuids = 1024;

        protected virtual ComputeShader ShaderProcessQueries => _shaderProcessQueries;
        ComputeShader _shaderProcessQueries;
        PropertyWrapperComputeStandalone _wrapper;

        System.Action<AsyncGPUReadbackRequest> _dataArrivedAction;

        // Must match value in compute shader
        const int s_computeGroupSize = 64;
        public static bool s_useComputeCollQueries = true;

        readonly int sp_queryPositions_minGridSizes = Shader.PropertyToID("_QueryPositions_MinGridSizes");

        const float s_finiteDiffDx = 0.1f;

        ComputeBuffer _computeBufQueries;
        ComputeBuffer _computeBufResults;

        public const int MAX_QUERY_COUNT_DEFAULT = 4096;

        int _maxQueryCount = MAX_QUERY_COUNT_DEFAULT;
        Vector3[] _queryPosXZ_minGridSize = new Vector3[MAX_QUERY_COUNT_DEFAULT];

        /// <summary>
        /// Holds information about all query points. Maps from unique hash code to position in point array.
        /// </summary>
        class SegmentRegistrar
        {
            public Dictionary<int, Vector2Int> _segments = new Dictionary<int, Vector2Int>();
            public int _numQueries = 0;
        }

        /// <summary>
        /// Since query results return asynchronously and may not return at all (in theory), we keep a ringbuffer
        /// of the registrars of the last frames so that when data does come back it can be interpreted correctly.
        /// </summary>
        class SegmentRegistrarRingBuffer
        {
            // Requests in flight plus 2 held values, plus one current
            readonly static int s_poolSize = s_maxRequests + 2 + 1;

            SegmentRegistrar[] _segments = new SegmentRegistrar[s_poolSize];

            public int _segmentRelease = 0;
            public int _segmentAcquire = 0;

            public SegmentRegistrar Current => _segments[_segmentAcquire];

            public SegmentRegistrarRingBuffer()
            {
                for (int i = 0; i < _segments.Length; i++)
                {
                    _segments[i] = new SegmentRegistrar();
                }
            }

            public void AcquireNew()
            {
                var lastIndex = _segmentAcquire;

                _segmentAcquire = (_segmentAcquire + 1) % _segments.Length;

                // The last index should never increment and land on the first index - it should only happen the other way around.
                Debug.Assert(_segmentAcquire != _segmentRelease, "Segment registrar scratch exhausted.");

                _segments[_segmentAcquire]._numQueries = _segments[lastIndex]._numQueries;

                _segments[_segmentAcquire]._segments.Clear();
                foreach (var segment in _segments[lastIndex]._segments)
                {
                    _segments[_segmentAcquire]._segments.Add(segment.Key, segment.Value);
                }
            }

            public void ReleaseLast()
            {
                _segmentRelease = (_segmentRelease + 1) % _segments.Length;
            }

            public void RemoveRegistrations(int key)
            {
                // Remove the guid for all of the next spare segment registrars. However, don't touch the ones that are being
                // used for active requests.
                int i = _segmentAcquire;
                while (true)
                {
                    if (_segments[i]._segments.ContainsKey(key))
                    {
                        _segments[i]._segments.Remove(key);
                    }

                    i = (i + 1) % _segments.Length;

                    if (i == _segmentRelease)
                    {
                        break;
                    }
                }
            }

            public void ClearAvailable()
            {
                // Extreme approach - flush all segments for next spare registrars (but don't touch ones being used for active requests)
                int i = _segmentAcquire;
                while (true)
                {
                    _segments[i]._segments.Clear();
                    _segments[i]._numQueries = 0;

                    i = (i + 1) % _segments.Length;

                    if (i == _segmentRelease)
                    {
                        break;
                    }
                }
            }

            public void ClearAll()
            {
                for (int i = 0; i < _segments.Length; i++)
                {
                    _segments[i]._numQueries = 0;
                    _segments[i]._segments.Clear();
                }
            }
        }

        SegmentRegistrarRingBuffer _segmentRegistrarRingBuffer = new SegmentRegistrarRingBuffer();

        NativeArray<Vector3> _queryResults;
        float _queryResultsTime = -1f;
        Dictionary<int, Vector2Int> _resultSegments;

        NativeArray<Vector3> _queryResultsLast;
        float _queryResultsTimeLast = -1f;
        Dictionary<int, Vector2Int> _resultSegmentsLast;

        struct ReadbackRequest
        {
            public AsyncGPUReadbackRequest _request;
            public float _dataTimestamp;
            public Dictionary<int, Vector2Int> _segments;
        }

        List<ReadbackRequest> _requests = new List<ReadbackRequest>();

        public enum QueryStatus
        {
            OK = 0,
            RetrieveFailed = 1,
            PostFailed = 2,
            NotEnoughDataForVels = 4,
            VelocityDataInvalidated = 8,
            InvalidDtForVelocity = 16,
        }

        protected abstract void BindInputsAndOutputs(PropertyWrapperComputeStandalone wrapper, ComputeBuffer resultsBuffer);

        /// <summary>
        /// Takes a unique request ID and some world space XZ positions, and computes the displacement vector that lands at this position,
        /// to a good approximation. The world space height of the water at that position is then SeaLevel + displacement.y.
        /// </summary>
        protected bool UpdateQueryPoints(int i_ownerHash, float i_minSpatialLength, Vector3[] queryPoints, Vector3[] queryNormals)
        {
            var segmentRetrieved = false;
            Vector2Int segment;

            // We'll send in 3 points to get normals
            var countPts = (queryPoints != null ? queryPoints.Length : 0);
            var countNorms = (queryNormals != null ? queryNormals.Length : 0);
            var countTotal = countPts + countNorms * 3;

            if (_segmentRegistrarRingBuffer.Current._segments.TryGetValue(i_ownerHash, out segment))
            {
                var segmentSize = segment.y - segment.x + 1;
                if (segmentSize == countTotal)
                {
                    segmentRetrieved = true;
                }
                else
                {
                    _segmentRegistrarRingBuffer.Current._segments.Remove(i_ownerHash);
                }
            }

            if (countTotal == 0)
            {
                // No query data
                return false;
            }

            if (!segmentRetrieved)
            {
                if (_segmentRegistrarRingBuffer.Current._segments.Count >= s_maxGuids)
                {
                    Debug.LogError("Too many guids registered with CollProviderCompute. Increase s_maxGuids.", this);
                    return false;
                }

                segment.x = _segmentRegistrarRingBuffer.Current._numQueries;
                segment.y = segment.x + countTotal - 1;
                _segmentRegistrarRingBuffer.Current._segments.Add(i_ownerHash, segment);

                _segmentRegistrarRingBuffer.Current._numQueries += countTotal;

                //Debug.Log("Added points for " + guid);
            }

            // The smallest wavelengths should repeat no more than twice across the smaller spatial length. Unless we're
            // in the last LOD - then this is the best we can do.
            float minWavelength = i_minSpatialLength / 2f;
            float minGridSize = minWavelength / OceanRenderer.Instance.MinTexelsPerWave;

            if (countPts + segment.x > _queryPosXZ_minGridSize.Length)
            {
                Debug.LogError("Too many wave height queries. Increase Max Query Count in the Animated Waves Settings.", this);
                return false;
            }

            for (int pointi = 0; pointi < countPts; pointi++)
            {
                _queryPosXZ_minGridSize[pointi + segment.x].x = queryPoints[pointi].x;
                _queryPosXZ_minGridSize[pointi + segment.x].y = queryPoints[pointi].z;
                _queryPosXZ_minGridSize[pointi + segment.x].z = minGridSize;
            }

            // To compute each normal, post 3 query points
            for (int normi = 0; normi < countNorms; normi++)
            {
                var arrIdx = segment.x + countPts + 3 * normi;

                _queryPosXZ_minGridSize[arrIdx + 0].x = queryNormals[normi].x;
                _queryPosXZ_minGridSize[arrIdx + 0].y = queryNormals[normi].z;
                _queryPosXZ_minGridSize[arrIdx + 0].z = minGridSize;

                _queryPosXZ_minGridSize[arrIdx + 1].x = queryNormals[normi].x + s_finiteDiffDx;
                _queryPosXZ_minGridSize[arrIdx + 1].y = queryNormals[normi].z;
                _queryPosXZ_minGridSize[arrIdx + 1].z = minGridSize;

                _queryPosXZ_minGridSize[arrIdx + 2].x = queryNormals[normi].x;
                _queryPosXZ_minGridSize[arrIdx + 2].y = queryNormals[normi].z + s_finiteDiffDx;
                _queryPosXZ_minGridSize[arrIdx + 2].z = minGridSize;
            }

            return true;
        }

        /// <summary>
        /// Signal that we're no longer servicing queries. Note this leaves an air bubble in the query buffer.
        /// </summary>
        public void RemoveQueryPoints(int guid)
        {
            _segmentRegistrarRingBuffer.RemoveRegistrations(guid);
        }

        /// <summary>
        /// Remove air bubbles from the query array. Currently this lazily just nukes all the registered
        /// query IDs so they'll be recreated next time (generating garbage). TODO..
        /// </summary>
        public void CompactQueryStorage()
        {
            _segmentRegistrarRingBuffer.ClearAvailable();
        }

        /// <summary>
        /// Copy out displacements, heights, normals. Pass null if info is not required.
        /// </summary>
        protected bool RetrieveResults(int guid, Vector3[] displacements, float[] heights, Vector3[] normals)
        {
            if (_resultSegments == null)
            {
                return false;
            }

            // Check if there are results that came back for this guid
            Vector2Int segment;
            if (!_resultSegments.TryGetValue(guid, out segment))
            {
                // Guid not found - no result
                return false;
            }

            var countPoints = 0;
            if (displacements != null) countPoints = displacements.Length;
            if (heights != null) countPoints = heights.Length;
            if (displacements != null && heights != null) Debug.Assert(displacements.Length == heights.Length);
            var countNorms = (normals != null ? normals.Length : 0);
            var countTotal = countPoints + countNorms * 3;

            if (countPoints > 0)
            {
                // Retrieve Results
                if (displacements != null) _queryResults.Slice(segment.x, countPoints).CopyTo(displacements);

                // Retrieve Result heights
                if (heights != null)
                {
                    var seaLevel = OceanRenderer.Instance.SeaLevel;
                    for (int i = 0; i < countPoints; i++)
                    {
                        heights[i] = seaLevel + _queryResults[i + segment.x].y;
                    }
                }
            }

            if (countNorms > 0)
            {
                int firstNorm = segment.x + countPoints;

                var dx = -Vector3.right * s_finiteDiffDx;
                var dz = -Vector3.forward * s_finiteDiffDx;
                for (int i = 0; i < countNorms; i++)
                {
                    var p = _queryResults[firstNorm + 3 * i + 0];
                    var px = dx + _queryResults[firstNorm + 3 * i + 1];
                    var pz = dz + _queryResults[firstNorm + 3 * i + 2];

                    normals[i] = Vector3.Cross(p - px, p - pz).normalized;
                    normals[i].y *= -1f;
                }
            }

            return true;
        }

        /// <summary>
        /// Compute time derivative of the displacements by calculating difference from last query. More complicated than it would seem - results
        /// may not be available in one or both of the results, or the query locations in the array may change.
        /// </summary>
        protected int CalculateVelocities(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPositions, Vector3[] results)
        {
            // Need at least 2 returned results to do finite difference
            if (_queryResultsTime < 0f || _queryResultsTimeLast < 0f)
            {
                return 1;
            }

            Vector2Int segment;
            if (!_resultSegments.TryGetValue(i_ownerHash, out segment))
            {
                return (int)QueryStatus.RetrieveFailed;
            }

            Vector2Int segmentLast;
            if (!_resultSegmentsLast.TryGetValue(i_ownerHash, out segmentLast))
            {
                return (int)QueryStatus.NotEnoughDataForVels;
            }

            if ((segment.y - segment.x) != (segmentLast.y - segmentLast.x))
            {
                // Number of queries changed - can't handle that
                return (int)QueryStatus.VelocityDataInvalidated;
            }

            var dt = _queryResultsTime - _queryResultsTimeLast;
            if (dt < 0.0001f)
            {
                return (int)QueryStatus.InvalidDtForVelocity;
            }

            var count = results.Length;
            for (var i = 0; i < count; i++)
            {
                results[i] = (_queryResults[i + segment.x] - _queryResultsLast[i + segmentLast.x]) / dt;
            }

            return 0;
        }

        // This needs to run in Update()
        // - It needs to run before OceanRenderer.LateUpdate, because the latter will change the LOD positions/scales, while we will read
        // the last frames displacements.
        // - It should run after FixedUpdate, as physics objects will update query points there. Also it computes the displacement timestamps
        // using Time.time and Time.deltaTime, which would be incorrect if it were in FixedUpdate.
        void Update()
        {
            if (_segmentRegistrarRingBuffer.Current._numQueries > 0)
            {
                ExecuteQueries();

                // Remove oldest requests if we have hit the limit
                while (_requests.Count >= _maxQueryCount)
                {
                    _requests.RemoveAt(0);
                }

                ReadbackRequest request;
                request._dataTimestamp = Time.time - Time.deltaTime;
                request._request = AsyncGPUReadback.Request(_computeBufResults, _dataArrivedAction);
                request._segments = _segmentRegistrarRingBuffer.Current._segments;
                _requests.Add(request);

                _segmentRegistrarRingBuffer.AcquireNew();
            }
        }

        void ExecuteQueries()
        {
            _computeBufQueries.SetData(_queryPosXZ_minGridSize, 0, 0, _segmentRegistrarRingBuffer.Current._numQueries);
            _shaderProcessQueries.SetBuffer(_kernelHandle, sp_queryPositions_minGridSizes, _computeBufQueries);
            BindInputsAndOutputs(_wrapper, _computeBufResults);

            var numGroups = (int)Mathf.Ceil((float)_segmentRegistrarRingBuffer.Current._numQueries / (float)s_computeGroupSize) * s_computeGroupSize;
            _shaderProcessQueries.Dispatch(_kernelHandle, numGroups, 1, 1);
        }

        /// <summary>
        /// Called when a compute buffer has been read back from the GPU to the CPU.
        /// </summary>
        void DataArrived(AsyncGPUReadbackRequest req)
        {
            // Can get callbacks after disable, so detect this.
            if (!_queryResults.IsCreated)
            {
                _requests.Clear();
                return;
            }

            // Remove any error requests
            for (int i = _requests.Count - 1; i >= 0; --i)
            {
                if (_requests[i]._request.hasError)
                {
                    _requests.RemoveAt(i);
                    _segmentRegistrarRingBuffer.ReleaseLast();
                }
            }

            // Find the last request that was completed
            var lastDoneIndex = _requests.Count - 1;
            while (lastDoneIndex >= 0 && !_requests[lastDoneIndex]._request.done)
            {
                --lastDoneIndex;
            }

            // If there is a completed request, process it
            if (lastDoneIndex >= 0)
            {
                // Update "last" results
                LodDataMgr.Swap(ref _queryResults, ref _queryResultsLast);
                _queryResultsTimeLast = _queryResultsTime;
                _resultSegmentsLast = _resultSegments;

                var data = _requests[lastDoneIndex]._request.GetData<Vector3>();
                data.CopyTo(_queryResults);
                _queryResultsTime = _requests[lastDoneIndex]._dataTimestamp;
                _resultSegments = _requests[lastDoneIndex]._segments;
            }

            // Remove all the requests up to the last completed one
            for (int i = lastDoneIndex; i >= 0; --i)
            {
                _requests.RemoveAt(i);
                _segmentRegistrarRingBuffer.ReleaseLast();
            }
        }

        protected virtual void OnEnable()
        {
            _dataArrivedAction = new System.Action<AsyncGPUReadbackRequest>(DataArrived);

            if (_maxQueryCount != OceanRenderer.Instance._simSettingsAnimatedWaves.MaxQueryCount)
            {
                _maxQueryCount = OceanRenderer.Instance._simSettingsAnimatedWaves.MaxQueryCount;
                _queryPosXZ_minGridSize = new Vector3[_maxQueryCount];
            }

            _computeBufQueries = new ComputeBuffer(_maxQueryCount, 12, ComputeBufferType.Default);
            _computeBufResults = new ComputeBuffer(_maxQueryCount, 12, ComputeBufferType.Default);

            _queryResults = new NativeArray<Vector3>(_maxQueryCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _queryResultsLast = new NativeArray<Vector3>(_maxQueryCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            _shaderProcessQueries = ComputeShaderHelpers.LoadShader(QueryShaderName);
            if (_shaderProcessQueries == null)
            {
                enabled = false;
                return;
            }
            _kernelHandle = _shaderProcessQueries.FindKernel(QueryKernelName);
            _wrapper = new PropertyWrapperComputeStandalone(_shaderProcessQueries, _kernelHandle);
        }

        protected virtual void OnDisable()
        {
            _computeBufQueries.Dispose();
            _computeBufResults.Dispose();

            _queryResults.Dispose();
            _queryResultsLast.Dispose();

            _segmentRegistrarRingBuffer.ClearAll();
        }

        public int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, Vector3[] o_resultDisps, Vector3[] o_resultNorms, Vector3[] o_resultVels)
        {
            var result = (int)QueryStatus.OK;

            if (!UpdateQueryPoints(i_ownerHash, i_minSpatialLength, i_queryPoints, o_resultNorms != null ? i_queryPoints : null))
            {
                result |= (int)QueryStatus.PostFailed;
            }

            if (!RetrieveResults(i_ownerHash, o_resultDisps, null, o_resultNorms))
            {
                result |= (int)QueryStatus.RetrieveFailed;
            }

            if (o_resultVels != null)
            {
                result |= CalculateVelocities(i_ownerHash, i_minSpatialLength, i_queryPoints, o_resultVels);
            }

            return result;
        }

        public bool RetrieveSucceeded(int queryStatus)
        {
            return (queryStatus & (int)QueryStatus.RetrieveFailed) == 0;
        }
    }
}
