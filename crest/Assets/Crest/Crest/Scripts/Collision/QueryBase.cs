﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Potential improvements
// - Half return values
// - Half minGridSize

using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
#if CREST_BURST_QUERY
    public static class QueryHelper
    {
        /// <summary>
        /// Pass this to query methods where you want to skip computing/fetching the result.
        /// Do not edit this value or use elsewhere.
        /// </summary>
        public static NativeArray<Vector3> s_Skip = default;
    }
#endif

    /// <summary>
    /// Provides heights and other physical data about the water surface. Works by uploading query positions to GPU and computing
    /// the data and then transferring back the results asynchronously. An exception to this is water surface velocities - these can
    /// not be computed on the GPU and are instead computed on the CPU by retaining last frames' query results and computing finite diffs.
    /// </summary>
#if CREST_BURST_QUERY
    [BurstCompile]
#endif
    public abstract class QueryBase
    {
        protected int _kernelHandle;

        protected abstract string QueryShaderName { get; }
        protected abstract string QueryKernelName { get; }

        // 4 was enough for a long time, but Linux setups seems to demand 7
        const int s_maxRequests = 7;
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
#if CREST_BURST_QUERY
        NativeArray<Vector3> _queryPosXZ_minGridSize;
#else
        Vector3[] _queryPosXZ_minGridSize = new Vector3[MAX_QUERY_COUNT_DEFAULT];
#endif

        /// <summary>
        /// Holds information about all query points. Maps from unique hash code to position in point array.
        /// </summary>
#if CREST_BURST_QUERY
        struct SegmentRegistrar
#else
        class SegmentRegistrar
#endif
        {
            // Map from guids to (segment start index, segment end index, frame number when query was made)
#if CREST_BURST_QUERY
            public NativeParallelHashMap<int, Vector3Int> _segments;
#else
            public Dictionary<int, Vector3Int> _segments = new Dictionary<int, Vector3Int>();
#endif
            public int _numQueries;
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

#if CREST_BURST_QUERY
            public ref SegmentRegistrar Current => ref _segments[_segmentAcquire];
#else
            public SegmentRegistrar Current => _segments[_segmentAcquire];
#endif

            public SegmentRegistrarRingBuffer()
            {
                for (int i = 0; i < _segments.Length; i++)
                {
                    _segments[i] = new SegmentRegistrar();
#if CREST_BURST_QUERY
                    _segments[i]._segments = new NativeParallelHashMap<int, Vector3Int>(16, Allocator.Persistent);
#endif
                }
            }

            public void AcquireNew()
            {
                var lastIndex = _segmentAcquire;

                {
                    var newSegmentAcquire = (_segmentAcquire + 1) % _segments.Length;

                    if (newSegmentAcquire == _segmentRelease)
                    {
                        // The last index has incremented and landed on the first index. This shouldn't happen normally, but
                        // can happen if the Scene and Game view are not visible, in which case async readbacks dont get processed
                        // and the pipeline blocks up.
#if !UNITY_EDITOR
                        Debug.LogError("Crest: Query ring buffer exhausted. Please report this to developers.");
#endif
                        return;
                    }

                    _segmentAcquire = newSegmentAcquire;
                }

                // Copy the registrations across from the previous frame. This makes queries persistent. This is needed because
                // queries are often made from FixedUpdate(), and at high framerates this may not be called, which would mean
                // the query would get lost and this leads to stuttering and other artifacts.
                {
                    _segments[_segmentAcquire]._numQueries = 0;
                    _segments[_segmentAcquire]._segments.Clear();

                    foreach (var segment in _segments[lastIndex]._segments)
                    {
                        var age = Time.frameCount - segment.Value.z;

                        // Don't keep queries around if they have not be active in the last 10 frames
                        if (age < 10)
                        {
                            // Compute a new segment range - we may have removed some segments that were too old, so this ensures
                            // we have a nice compact array of queries each frame rather than accumulating persistent air bubbles
                            var newSegment = segment.Value;
                            newSegment.x = _segments[_segmentAcquire]._numQueries;
                            newSegment.y = newSegment.x + (segment.Value.y - segment.Value.x);
                            _segments[_segmentAcquire]._numQueries = newSegment.y + 1;

                            _segments[_segmentAcquire]._segments.Add(segment.Key, newSegment);
                        }
                    }
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
#if CREST_BURST_QUERY
                    _segments[i]._segments.Dispose();
#endif
                }
            }
        }

        SegmentRegistrarRingBuffer _segmentRegistrarRingBuffer = new SegmentRegistrarRingBuffer();

        NativeArray<Vector3> _queryResults;
        float _queryResultsTime = -1f;
#if CREST_BURST_QUERY
        NativeParallelHashMap<int, Vector3Int> _resultSegments;
#else
        Dictionary<int, Vector3Int> _resultSegments;
#endif

        NativeArray<Vector3> _queryResultsLast;
        float _queryResultsTimeLast = -1f;
#if CREST_BURST_QUERY
        NativeParallelHashMap<int, Vector3Int> _resultSegmentsLast;
#else
        Dictionary<int, Vector3Int> _resultSegmentsLast;
#endif

        struct ReadbackRequest
        {
            public AsyncGPUReadbackRequest _request;
            public float _dataTimestamp;
#if CREST_BURST_QUERY
            public NativeParallelHashMap<int, Vector3Int> _segments;
#else
            public Dictionary<int, Vector3Int> _segments;
#endif
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

        public QueryBase()
        {
            _dataArrivedAction = new System.Action<AsyncGPUReadbackRequest>(DataArrived);

            if (_maxQueryCount != OceanRenderer.Instance._lodDataAnimWaves.Settings.MaxQueryCount)
            {
                _maxQueryCount = OceanRenderer.Instance._lodDataAnimWaves.Settings.MaxQueryCount;
#if CREST_BURST_QUERY
                _queryPosXZ_minGridSize = new NativeArray<Vector3>(_maxQueryCount, Allocator.Persistent);
            }
            else
            {
                _queryPosXZ_minGridSize = new NativeArray<Vector3>(MAX_QUERY_COUNT_DEFAULT, Allocator.Persistent);
            }
#else
                _queryPosXZ_minGridSize = new Vector3[_maxQueryCount];
            }
#endif

            _computeBufQueries = new ComputeBuffer(_maxQueryCount, 12, ComputeBufferType.Default);
            _computeBufResults = new ComputeBuffer(_maxQueryCount, 12, ComputeBufferType.Default);

            _queryResults = new NativeArray<Vector3>(_maxQueryCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _queryResultsLast = new NativeArray<Vector3>(_maxQueryCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            _shaderProcessQueries = ComputeShaderHelpers.LoadShader(QueryShaderName);
            if (_shaderProcessQueries == null)
            {
                Debug.LogError($"Crest: Could not load Query compute shader {QueryShaderName}");
                return;
            }
            _kernelHandle = _shaderProcessQueries.FindKernel(QueryKernelName);
            _wrapper = new PropertyWrapperComputeStandalone(_shaderProcessQueries, _kernelHandle);
        }

        protected abstract void BindInputsAndOutputs(PropertyWrapperComputeStandalone wrapper, ComputeBuffer resultsBuffer);

        /// <summary>
        /// Takes a unique request ID and some world space XZ positions, and computes the displacement vector that lands at this position,
        /// to a good approximation. The world space height of the water at that position is then SeaLevel + displacement.y.
        /// </summary>
#if CREST_BURST_QUERY
        protected bool UpdateQueryPoints(int i_ownerHash, float i_minSpatialLength, NativeArray<Vector3> queryPoints, NativeArray<Vector3> queryNormals, bool useNormals)
        {
            unsafe
            {
                var args = new UQPArgs()
                {
                    i_ownerHash = i_ownerHash,
                    i_minSpatialLength = i_minSpatialLength,
                    queryPoints = queryPoints,
                    queryNormals = queryNormals,
                    useNormals = (byte)(useNormals ? 1 : 0),
                    sqrrbNumQueries = _segmentRegistrarRingBuffer.Current._numQueries,
                    _maxQueryCount = _maxQueryCount,
                    sqrrbCurrentSegments = _segmentRegistrarRingBuffer.Current._segments,
                    _queryPosXZ_minGridSize = _queryPosXZ_minGridSize
                };
                var ret = UpdateQueryPoints(UnsafeUtility.AddressOf(ref args));

                _segmentRegistrarRingBuffer.Current._numQueries = args.sqrrbNumQueries;
                _segmentRegistrarRingBuffer.Current._segments = args.sqrrbCurrentSegments;
                _queryPosXZ_minGridSize = args._queryPosXZ_minGridSize;
                return ret;
            }
        }

        /*
         * The below little dance is required to pass native collections as arguments to burst-compiled static functions
         * on the main thread in unity 22.1 and below. It mirrors what the job system does internally for job structs.
         * Basically, in 22.1 and below, native collections all have DisposeSentinel objects in them, which
         * _technically_ makes them managed objects, and which Burst will therefore not accept as arguments to a
         * Burst-entrypoint static function.
         *
         * However, Burst can compile code *using* native collections just fine, which is how bursted jobs can work.
         * We just need to trick it into having access to them, which we do via UnsafeUtility.AsRef.
         *
         * The only difference between what this does and what the job system does is that the job system sets the
         * disposesentinel fields to null when it copies the job struct to the worker threads, whereas we aren't copying
         * them anyway, so we just leave them alone.
         *
         * In 22.2, DisposeSentinel is no longer a class, and so this can likely be revisited.
         */
        struct UQPArgs
        {
            public int i_ownerHash;
            public float i_minSpatialLength;
            public NativeArray<Vector3> queryPoints;
            public NativeArray<Vector3> queryNormals;
            public byte useNormals;
            public int sqrrbNumQueries;
            public int _maxQueryCount;
            public NativeParallelHashMap<int, Vector3Int> sqrrbCurrentSegments;
            public NativeArray<Vector3> _queryPosXZ_minGridSize;
        }

        [BurstCompile]
        protected static unsafe bool UpdateQueryPoints(void* args)
        {
            ref var realargs = ref UnsafeUtility.AsRef<UQPArgs>(args);

            if (realargs.queryPoints.Length + realargs.sqrrbNumQueries > realargs._maxQueryCount)
            {
                Debug.LogError($"Crest: Max query count ({realargs._maxQueryCount}) exceeded, increase the max query count in the Animated Waves Settings to support a higher number of queries.");
                return false;
            }

            var segmentRetrieved = false;

            // We'll send in 3 points to get normals
            var countPts = realargs.queryPoints.Length;
            var countNorms = (realargs.useNormals != 0 ? realargs.queryNormals.Length : 0);
            var countTotal = countPts + countNorms * 3;

            if (realargs.sqrrbCurrentSegments.TryGetValue(realargs.i_ownerHash, out var segment))
            {
                var segmentSize = segment.y - segment.x + 1;
                if (segmentSize == countTotal)
                {
                    // Update frame count
                    segment.z = Time.frameCount;
                    realargs.sqrrbCurrentSegments[realargs.i_ownerHash] = segment;

                    segmentRetrieved = true;
                }
                else
                {
                    realargs.sqrrbCurrentSegments.Remove(realargs.i_ownerHash);
                }
            }

            if (countTotal == 0)
            {
                // No query data
                return false;
            }

            if (!segmentRetrieved)
            {
                if (realargs.sqrrbCurrentSegments.Count() >= s_maxGuids)
                {
                    Debug.LogError("Crest: Too many guids registered with CollProviderCompute. Increase s_maxGuids.");
                    return false;
                }

                segment.x = realargs.sqrrbNumQueries;
                segment.y = segment.x + countTotal - 1;
                segment.z = Time.frameCount;
                realargs.sqrrbCurrentSegments.Add(realargs.i_ownerHash, segment);

                realargs.sqrrbNumQueries += countTotal;

                //Debug.Log("Crest: Added points for " + guid);
            }

            // The smallest wavelengths should repeat no more than twice across the smaller spatial length. Unless we're
            // in the last LOD - then this is the best we can do.
            float minWavelength = realargs.i_minSpatialLength / 2f;
            float samplesPerWave = 2f;
            float minGridSize = minWavelength / samplesPerWave;

            if (countPts + segment.x > realargs._queryPosXZ_minGridSize.Length)
            {
                Debug.LogError("Crest: Too many wave height queries. Increase Max Query Count in the Animated Waves Settings.");
                return false;
            }

            Vector3 tmp;
            for (int pointi = 0; pointi < countPts; pointi++)
            {
                tmp = realargs._queryPosXZ_minGridSize[pointi + segment.x];
                tmp.x = realargs.queryPoints[pointi].x;
                tmp.y = realargs.queryPoints[pointi].z;
                tmp.z = minGridSize;
                realargs._queryPosXZ_minGridSize[pointi + segment.x] = tmp;
            }

            // To compute each normal, post 3 query points
            for (int normi = 0; normi < countNorms; normi++)
            {
                var arrIdx = segment.x + countPts + 3 * normi;

                tmp = realargs._queryPosXZ_minGridSize[arrIdx + 0];
                tmp.x = realargs.queryNormals[normi].x;
                tmp.y = realargs.queryNormals[normi].z;
                tmp.z = minGridSize;
                realargs._queryPosXZ_minGridSize[arrIdx + 0] = tmp;

                tmp = realargs._queryPosXZ_minGridSize[arrIdx + 1];
                tmp.x = realargs.queryNormals[normi].x + s_finiteDiffDx;
                tmp.y = realargs.queryNormals[normi].z;
                tmp.z = minGridSize;
                realargs._queryPosXZ_minGridSize[arrIdx + 1] = tmp;

                tmp = realargs._queryPosXZ_minGridSize[arrIdx + 2];
                tmp.x = realargs.queryNormals[normi].x;
                tmp.y = realargs.queryNormals[normi].z + s_finiteDiffDx;
                tmp.z = minGridSize;
                realargs._queryPosXZ_minGridSize[arrIdx + 2] = tmp;
            }

            return true;
        }
#else
        protected bool UpdateQueryPoints(int i_ownerHash, float i_minSpatialLength, Vector3[] queryPoints, Vector3[] queryNormals)
        {
            if (queryPoints.Length + _segmentRegistrarRingBuffer.Current._numQueries > _maxQueryCount)
            {
                Debug.LogError($"Crest: Max query count ({_maxQueryCount}) exceeded, increase the max query count in the Animated Waves Settings to support a higher number of queries.");
                return false;
            }

            var segmentRetrieved = false;

            // We'll send in 3 points to get normals
            var countPts = (queryPoints != null ? queryPoints.Length : 0);
            var countNorms = (queryNormals != null ? queryNormals.Length : 0);
            var countTotal = countPts + countNorms * 3;

            if (_segmentRegistrarRingBuffer.Current._segments.TryGetValue(i_ownerHash, out var segment))
            {
                var segmentSize = segment.y - segment.x + 1;
                if (segmentSize == countTotal)
                {
                    // Update frame count
                    segment.z = Time.frameCount;
                    _segmentRegistrarRingBuffer.Current._segments[i_ownerHash] = segment;

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
                    Debug.LogError("Crest: Too many guids registered with CollProviderCompute. Increase s_maxGuids.");
                    return false;
                }

                segment.x = _segmentRegistrarRingBuffer.Current._numQueries;
                segment.y = segment.x + countTotal - 1;
                segment.z = Time.frameCount;
                _segmentRegistrarRingBuffer.Current._segments.Add(i_ownerHash, segment);

                _segmentRegistrarRingBuffer.Current._numQueries += countTotal;

                //Debug.Log("Crest: Added points for " + guid);
            }

            // The smallest wavelengths should repeat no more than twice across the smaller spatial length. Unless we're
            // in the last LOD - then this is the best we can do.
            float minWavelength = i_minSpatialLength / 2f;
            float samplesPerWave = 2f;
            float minGridSize = minWavelength / samplesPerWave;

            if (countPts + segment.x > _queryPosXZ_minGridSize.Length)
            {
                Debug.LogError("Crest: Too many wave height queries. Increase Max Query Count in the Animated Waves Settings.");
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
#endif

        /// <summary>
        /// Signal that we're no longer servicing queries. Note this leaves an air bubble in the query buffer.
        /// </summary>
        public void RemoveQueryPoints(int guid)
        {
            _segmentRegistrarRingBuffer.RemoveRegistrations(guid);
        }

        /// <summary>
        /// Remove air bubbles from the query array. Currently this lazily just nukes all the registered
        /// query IDs so they'll be recreated next time (generating garbage).
        /// </summary>
        public void CompactQueryStorage()
        {
            _segmentRegistrarRingBuffer.ClearAvailable();
        }

        /// <summary>
        /// Copy out displacements, heights, normals. Pass null if info is not required.
        /// </summary>
#if CREST_BURST_QUERY
        protected bool RetrieveResults(int guid, NativeArray<Vector3> displacements, NativeArray<float> heights, NativeArray<Vector3> normals)
#else
        protected bool RetrieveResults(int guid, Vector3[] displacements, float[] heights, Vector3[] normals)
#endif
        {
#if CREST_BURST_QUERY
            if (!_resultSegments.IsCreated)
#else
            if (_resultSegments == null)
#endif
            {
                return false;
            }

            // Check if there are results that came back for this guid
            if (!_resultSegments.TryGetValue(guid, out var segment))
            {
                // Guid not found - no result
                return false;
            }

            var countPoints = 0;
#if CREST_BURST_QUERY
            if (displacements.Length > 0) countPoints = displacements.Length;
            if (heights.Length > 0) countPoints = heights.Length;
            if (displacements.Length > 0 && heights.Length > 0) Debug.Assert(displacements.Length == heights.Length);
            var countNorms = normals.Length;
#else
            if (displacements != null) countPoints = displacements.Length;
            if (heights != null) countPoints = heights.Length;
            if (displacements != null && heights != null) Debug.Assert(displacements.Length == heights.Length);
            var countNorms = (normals != null ? normals.Length : 0);
#endif

            if (countPoints > 0)
            {
                // Retrieve Results
#if CREST_BURST_QUERY
                if (displacements.Length > 0) _queryResults.Slice(segment.x, countPoints).CopyTo(displacements);
#else
                if (displacements != null) _queryResults.Slice(segment.x, countPoints).CopyTo(displacements);
#endif

                // Retrieve Result heights
#if CREST_BURST_QUERY
                if (heights.Length > 0)
#else
                if (heights != null)
#endif
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

                    var tmp = Vector3.Cross(p - px, p - pz).normalized;
                    tmp.y *= -1f;
                    normals[i] = tmp;
                }
            }

            return true;
        }

        /// <summary>
        /// Compute time derivative of the displacements by calculating difference from last query. More complicated than it would seem - results
        /// may not be available in one or both of the results, or the query locations in the array may change.
        /// </summary>
#if CREST_BURST_QUERY
        protected unsafe int CalculateVelocities(int i_ownerHash, NativeArray<Vector3> results)
        {
            var args = new CVArgs()
            {
                i_ownerHash = i_ownerHash,
                _queryResultsTime = _queryResultsTime,
                _queryResultsTimeLast = _queryResultsTimeLast,
                _resultSegments = _resultSegments,
                _resultSegmentsLast = _resultSegmentsLast,
                results = results,
                _queryResults = _queryResults,
                _queryResultsLast = _queryResultsLast
            };
            return CalculateVelocities(UnsafeUtility.AddressOf(ref args));
        }

        // See comment on UQPArgs as to why we are doing this dance.
        struct CVArgs
        {
            public int i_ownerHash;
            public float _queryResultsTime;
            public float _queryResultsTimeLast;
            public NativeParallelHashMap<int, Vector3Int> _resultSegments;
            public NativeParallelHashMap<int, Vector3Int> _resultSegmentsLast;
            public NativeArray<Vector3> results;
            public NativeArray<Vector3> _queryResults;
            public NativeArray<Vector3> _queryResultsLast;
        }

        [BurstCompile]
        protected static unsafe int CalculateVelocities(void* args)
        {
            ref var realargs = ref UnsafeUtility.AsRef<CVArgs>(args);
            // Need at least 2 returned results to do finite difference
            if (realargs._queryResultsTime < 0f || realargs._queryResultsTimeLast < 0f)
            {
                return 1;
            }

            if (!realargs._resultSegments.TryGetValue(realargs.i_ownerHash, out var segment))
            {
                return (int)QueryStatus.RetrieveFailed;
            }

            if (!realargs._resultSegmentsLast.TryGetValue(realargs.i_ownerHash, out var segmentLast))
            {
                return (int)QueryStatus.NotEnoughDataForVels;
            }

            if ((segment.y - segment.x) != (segmentLast.y - segmentLast.x))
            {
                // Number of queries changed - can't handle that
                return (int)QueryStatus.VelocityDataInvalidated;
            }

            var dt = realargs._queryResultsTime - realargs._queryResultsTimeLast;
            if (dt < 0.0001f)
            {
                return (int)QueryStatus.InvalidDtForVelocity;
            }

            var count = realargs.results.Length;
            for (var i = 0; i < count; i++)
            {
                realargs.results[i] = (realargs._queryResults[i + segment.x] - realargs._queryResultsLast[i + segmentLast.x]) / dt;
            }

            return 0;
        }
#else
        protected int CalculateVelocities(int i_ownerHash, Vector3[] results)
        {
            // Need at least 2 returned results to do finite difference
            if (_queryResultsTime < 0f || _queryResultsTimeLast < 0f)
            {
                return 1;
            }

            if (!_resultSegments.TryGetValue(i_ownerHash, out var segment))
            {
                return (int)QueryStatus.RetrieveFailed;
            }

            if (!_resultSegmentsLast.TryGetValue(i_ownerHash, out var segmentLast))
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
#endif

        public void UpdateQueries()
        {
            if (_segmentRegistrarRingBuffer.Current._numQueries > 0)
            {
                ExecuteQueries();

                // Remove oldest requests if we have hit the limit
                while (_requests.Count >= s_maxRequests)
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

            var numGroups = (_segmentRegistrarRingBuffer.Current._numQueries + s_computeGroupSize - 1) / s_computeGroupSize;
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
                Helpers.Swap(ref _queryResults, ref _queryResultsLast);
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

        public void CleanUp()
        {
            _computeBufQueries.Dispose();
            _computeBufResults.Dispose();

            _queryResults.Dispose();
            _queryResultsLast.Dispose();

#if CREST_BURST_QUERY
            _queryPosXZ_minGridSize.Dispose();
#endif

            _segmentRegistrarRingBuffer.ClearAll();
        }

#if CREST_BURST_QUERY
        public int Query(int i_ownerHash, float i_minSpatialLength, ref NativeArray<Vector3> i_queryPoints, ref NativeArray<Vector3> o_resultDisps, ref NativeArray<Vector3> o_resultNorms, ref NativeArray<Vector3> o_resultVels)
#else
        public int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, Vector3[] o_resultDisps, Vector3[] o_resultNorms, Vector3[] o_resultVels)
#endif
        {
            var result = (int)QueryStatus.OK;

#if CREST_BURST_QUERY
            var useDisplacement = o_resultDisps.Length > 0;
            var useNormal = o_resultNorms.Length > 0;
            var useVelocity = o_resultVels.Length > 0;
#else
            var useDisplacement = o_resultDisps?.Length > 0;
            var useNormal = o_resultNorms?.Length > 0;
            var useVelocity = o_resultVels?.Length > 0;
#endif

#if CREST_BURST_QUERY
            if (!UpdateQueryPoints(i_ownerHash, i_minSpatialLength, i_queryPoints, i_queryPoints, useNormal))
#else
            if (!UpdateQueryPoints(i_ownerHash, i_minSpatialLength, i_queryPoints, useNormal ? i_queryPoints : null))
#endif
            {
                result |= (int)QueryStatus.PostFailed;
            }

#if CREST_BURST_QUERY
            if (!RetrieveResults(i_ownerHash, o_resultDisps, default, o_resultNorms))
#else
            if (!RetrieveResults(i_ownerHash, o_resultDisps, null, o_resultNorms))
#endif
            {
                result |= (int)QueryStatus.RetrieveFailed;
            }

            if (useVelocity)
            {
                result |= CalculateVelocities(i_ownerHash, o_resultVels);
            }

            return result;
        }

        public bool RetrieveSucceeded(int queryStatus)
        {
            return (queryStatus & (int)QueryStatus.RetrieveFailed) == 0;
        }

#if CREST_BURST_QUERY
        public int ResultGuidCount => _resultSegments.Count();
#else
        public int ResultGuidCount => _resultSegments != null ? _resultSegments.Count : 0;
#endif

        public int RequestCount => _requests != null ? _requests.Count : 0;
    }
}
