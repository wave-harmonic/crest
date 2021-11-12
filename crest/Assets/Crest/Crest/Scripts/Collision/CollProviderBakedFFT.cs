// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if CREST_UNITY_MATHEMATICS

using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Collision provider for baked FFT data
    /// </summary>
    public class CollProviderBakedFFT : ICollProvider
    {
        enum QueryStatus
        {
            /// <summary>
            /// Valid query results returned
            /// </summary>
            Success,
            /// <summary>
            /// No results to return - before first round of results have been processed
            /// </summary>
            ResultsNotReadyYet,
            /// <summary>
            /// Data needs to be rebaked - data reference null
            /// </summary>
            CollisionDataMissing,
            /// <summary>
            /// Out of query data space. Increase CollProviderBakedFFT.MAX_QUERY_QUADS to allow
            /// more queries to be executed
            /// </summary>
            TooManyQueries,
        }

        public FFTBakedData _data = null;

        const float s_finiteDiffDx = 0.1f;
        const float s_finiteDiffDt = 0.06f;
        const int s_jobBatchSize = 8;

        const int MAX_QUERY_QUADS = 8192;

        /// <summary>
        /// Data for a particular query type, such as heights or normals.
        /// </summary>
        class QueryData
        {
            // Segment int3 is (index of first query, index of last query, last queried frame count)
            public Dictionary<int, int3> _segmentRegistryNewQueries = new Dictionary<int, int3>();
            public Dictionary<int, int3> _segmentRegistryQueriesInProgress = new Dictionary<int, int3>();
            public Dictionary<int, int3> _segmentRegistryQueriesResults = new Dictionary<int, int3>();

            // Double buffered query input and output data. Query data can vary in count
            // for different query types. Each is double buffered so that jobs can run
            // while new query input data is registered.
            public NativeArray<float4>[] _queryPositionQuadsX = new NativeArray<float4>[2];
            public NativeArray<float4>[] _queryPositionQuadsZ = new NativeArray<float4>[2];
            public int _lastQueryQuadIndex = 0;
            public NativeArray<float4>[] _resultQuads0 = new NativeArray<float4>[2];
            public NativeArray<float4>[] _resultQuads1 = new NativeArray<float4>[2];
            public NativeArray<float4>[] _resultQuads2 = new NativeArray<float4>[2];

            /// <summary>
            /// Updates the query positions (creates space for them the first time). If the query count doesn't match a new set of query
            /// position data will be created. This will force any running jobs to complete. The jobs will be kicked off in LateUpdate,
            /// so this should be called before the kick-off, such as from Update.
            /// </summary>
            public int RegisterQueryPoints(int ownerHash, Vector3[] queryPoints, int dataToWriteThisFrame)
            {
                var numQuads = (queryPoints.Length + 3) / 4;

                // Get segment to find place to write to for next jobs
                var segmentRetrieved = false;
                int3 querySegment_frameAdded;
                if (_segmentRegistryNewQueries.TryGetValue(ownerHash, out querySegment_frameAdded))
                {
                    // make sure segment size matches our query count
                    var segmentSize = querySegment_frameAdded[1] - querySegment_frameAdded[0];
                    if (segmentSize == numQuads)
                    {
                        // All good
                        segmentRetrieved = true;

                        // Update timestamp to keep the query fresh
                        querySegment_frameAdded.z = Time.frameCount;
                        _segmentRegistryNewQueries[ownerHash] = querySegment_frameAdded;
                    }
                    else
                    {
                        // Query count does not match segment - remove it. The segment will be recreated below.
                        _segmentRegistryNewQueries.Remove(ownerHash);
                    }
                }

                // If no segment was retrieved, add one if there is space
                if (!segmentRetrieved)
                {
                    if (_lastQueryQuadIndex + numQuads > MAX_QUERY_QUADS)
                    {
                        Debug.LogError("Crest: Out of query data space. Increase CollProviderBakedFFT.MAX_QUERY_QUADS" +
                            "to allow more queries to be executed.");
                        return (int)QueryStatus.TooManyQueries;
                    }

                    querySegment_frameAdded = new int3(_lastQueryQuadIndex, _lastQueryQuadIndex + numQuads, Time.frameCount);
                    _segmentRegistryNewQueries.Add(ownerHash, querySegment_frameAdded);
                    _lastQueryQuadIndex += numQuads;
                }

                // Copy input data. Could be avoided if query api is changed to use NAs.
                for (var i = 0; i < queryPoints.Length; i++)
                {
                    var quadIdx = i / 4;
                    var outIdx = quadIdx + querySegment_frameAdded.x;

                    var xQuad = _queryPositionQuadsX[dataToWriteThisFrame][outIdx];
                    var zQuad = _queryPositionQuadsZ[dataToWriteThisFrame][outIdx];

                    var quadComp = i % 4;
                    xQuad[quadComp] = queryPoints[i].x;
                    zQuad[quadComp] = queryPoints[i].z;

                    _queryPositionQuadsX[dataToWriteThisFrame][outIdx] = xQuad;
                    _queryPositionQuadsZ[dataToWriteThisFrame][outIdx] = zQuad;
                }

                return (int)QueryStatus.Success;
            }

            // Called after jobs have been scheduled. Resets ready to collect next set of
            // queries.
            public void Flip()
            {
                // Cycle the segment registries

                // Results become the next query input (last stage cycles back to first)
                var nextQueries = _segmentRegistryQueriesResults;
                // In progress queries become results
                _segmentRegistryQueriesResults = _segmentRegistryQueriesInProgress;
                // Newly collected queries are now being processed
                _segmentRegistryQueriesInProgress = _segmentRegistryNewQueries;
                // The old results become the new queries
                _segmentRegistryNewQueries = nextQueries;

                // Clear so if something stops querying it's cleaned out
                _segmentRegistryNewQueries.Clear();

                // Reset counter ready to receive queries
                _lastQueryQuadIndex = 0;

                // Copy the registrations across from the previous frame. This makes queries persistent. This is needed because
                // queries are often made from FixedUpdate(), and at high framerates this may not be called, which would mean
                // the query would get lost and this leads to stuttering and other artifacts.
                foreach (var registration in _segmentRegistryQueriesInProgress)
                {
                    var age = Time.frameCount - registration.Value.z;

                    // If query has not been used in a while, throw it away
                    if (age < 10)
                    {
                        // Bring query segment across. Update indices which will compact the array.
                        int3 newSegment;
                        newSegment.x = _lastQueryQuadIndex;
                        newSegment.y = newSegment.x + (registration.Value.y - registration.Value.x);
                        newSegment.z = registration.Value.z;

                        _lastQueryQuadIndex = newSegment.y + 1;

                        _segmentRegistryNewQueries.Add(registration.Key, newSegment);
                    }
                }
            }
        }

        QueryData _queryDataHeights = new QueryData();
        QueryData _queryDataDisps = new QueryData();
        QueryData _queryDataNorms = new QueryData();
        QueryData _queryDataVels = new QueryData();

        /// <summary>
        /// Query data double buffered, this gives index of buffer to write new query data to.
        /// </summary>
        int _dataBeingUsedByJobs = 1;
        /// <summary>
        /// Handle for all jobs.
        /// </summary>
        JobHandle _jobHandle;

        public CollProviderBakedFFT(FFTBakedData data)
        {
            Debug.Assert(data != null, "Crest: Baked data should not be null.");
            _data = data;

            for (var i = 0; i < 2; i++)
            {
                _queryDataHeights._queryPositionQuadsX[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataHeights._queryPositionQuadsZ[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataHeights._resultQuads0[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);

                _queryDataDisps._queryPositionQuadsX[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataDisps._queryPositionQuadsZ[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataDisps._resultQuads0[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataDisps._resultQuads1[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataDisps._resultQuads2[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);

                _queryDataNorms._queryPositionQuadsX[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataNorms._queryPositionQuadsZ[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataNorms._resultQuads0[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataNorms._resultQuads1[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataNorms._resultQuads2[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);

                _queryDataVels._queryPositionQuadsX[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataVels._queryPositionQuadsZ[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
                _queryDataVels._resultQuads0[i] = new NativeArray<float4>(MAX_QUERY_QUADS, Allocator.Persistent);
            }
        }

        bool RetrieveHeights(int i_ownerHash, float[] o_resultHeights)
        {
            // Return data - get segment from finished jobs
            if (o_resultHeights != null && _queryDataHeights._segmentRegistryQueriesResults.TryGetValue(i_ownerHash, out var computedQuerySegment))
            {
                // Copy results to output. Could be avoided if query api was changed to NAs.
                for (int i = 0; i < o_resultHeights.Length; i++)
                {
                    var quadIdx = computedQuerySegment.x + i / 4;
                    o_resultHeights[i] = _queryDataHeights._resultQuads0[1 - _dataBeingUsedByJobs][quadIdx][i % 4];
                }
                return true;
            }
            return false;
        }

        bool RetrieveDisps(int i_ownerHash, Vector3[] o_resultDisps)
        {
            // Return data - get segment from finished jobs
            if (o_resultDisps != null && _queryDataDisps._segmentRegistryQueriesResults.TryGetValue(i_ownerHash, out var computedQuerySegment))
            {
                // Copy results to output. Could be avoided if query api was changed to NAs.
                Vector3 disp;
                for (int i = 0; i < o_resultDisps.Length; i++)
                {
                    var quadIdx = computedQuerySegment.x + i / 4;
                    disp.x = _queryDataDisps._resultQuads0[1 - _dataBeingUsedByJobs][quadIdx][i % 4];
                    disp.y = _queryDataDisps._resultQuads1[1 - _dataBeingUsedByJobs][quadIdx][i % 4];
                    disp.z = _queryDataDisps._resultQuads2[1 - _dataBeingUsedByJobs][quadIdx][i % 4];
                    o_resultDisps[i] = disp;
                }
                return true;
            }
            return false;
        }

        bool RetrieveNorms(int i_ownerHash, Vector3[] o_resultNorms)
        {
            if (o_resultNorms != null && _queryDataNorms._segmentRegistryQueriesResults.TryGetValue(i_ownerHash, out var computedQuerySegment))
            {
                // Copy results to output. Could be avoided if query api was changed to NAs.
                Vector3 norm;
                for (int i = 0; i < o_resultNorms.Length; i++)
                {
                    var quadIdx = computedQuerySegment.x + i / 4;
                    norm.x = _queryDataNorms._resultQuads0[1 - _dataBeingUsedByJobs][quadIdx][i % 4];
                    norm.y = _queryDataNorms._resultQuads1[1 - _dataBeingUsedByJobs][quadIdx][i % 4];
                    norm.z = _queryDataNorms._resultQuads2[1 - _dataBeingUsedByJobs][quadIdx][i % 4];
                    o_resultNorms[i] = norm;
                }
                return true;
            }
            return false;
        }

        bool RetrieveVels(int i_ownerHash, Vector3[] o_resultVels)
        {
            if (o_resultVels != null && _queryDataVels._segmentRegistryQueriesResults.TryGetValue(i_ownerHash, out var computedQuerySegment))
            {
                // Copy results to output. Could be avoided if query api was changed to NAs.
                Vector3 vel = Vector3.zero;
                for (int i = 0; i < o_resultVels.Length; i++)
                {
                    var quadIdx = computedQuerySegment.x + i / 4;
                    vel.y = _queryDataVels._resultQuads0[1 - _dataBeingUsedByJobs][quadIdx][i % 4];
                    o_resultVels[i] = vel;
                }
                return true;
            }
            return false;
        }

        public int Query(
            int i_ownerHash,
            float i_minSpatialLength,
            Vector3[] i_queryPoints,
            float[] o_resultHeights,
            Vector3[] o_resultNorms,
            Vector3[] o_resultVels
            )
        {
            var dataCopiedOutHeights = RetrieveHeights(i_ownerHash, o_resultHeights);
            var dataCopiedOutNorms = RetrieveNorms(i_ownerHash, o_resultNorms);
            var dataCopiedOutVels = RetrieveVels(i_ownerHash, o_resultVels);

            if (o_resultHeights != null)
            {
                _queryDataHeights.RegisterQueryPoints(i_ownerHash, i_queryPoints, 1 - _dataBeingUsedByJobs);
            }
            if (o_resultNorms != null)
            {
                _queryDataNorms.RegisterQueryPoints(i_ownerHash, i_queryPoints, 1 - _dataBeingUsedByJobs);
            }
            if (o_resultVels != null)
            {
                _queryDataVels.RegisterQueryPoints(i_ownerHash, i_queryPoints, 1 - _dataBeingUsedByJobs);
            }

            var allCopied = (dataCopiedOutHeights || o_resultHeights == null)
                && (dataCopiedOutNorms || o_resultNorms == null)
                && (dataCopiedOutVels || o_resultVels == null);

            return allCopied ? (int)QueryStatus.Success : (int)QueryStatus.ResultsNotReadyYet;
        }

        public int Query(
            int i_ownerHash,
            float i_minSpatialLength,
            Vector3[] i_queryPoints,
            Vector3[] o_resultDisps,
            Vector3[] o_resultNorms,
            Vector3[] o_resultVels
            )
        {
            var dataCopiedOutDisps = RetrieveDisps(i_ownerHash, o_resultDisps);
            var dataCopiedOutNorms = RetrieveNorms(i_ownerHash, o_resultNorms);
            var dataCopiedOutVels = RetrieveVels(i_ownerHash, o_resultVels);

            if (o_resultDisps != null)
            {
                _queryDataDisps.RegisterQueryPoints(i_ownerHash, i_queryPoints, 1 - _dataBeingUsedByJobs);
            }
            if (o_resultNorms != null)
            {
                _queryDataNorms.RegisterQueryPoints(i_ownerHash, i_queryPoints, 1 - _dataBeingUsedByJobs);
            }
            if (o_resultVels != null)
            {
                _queryDataVels.RegisterQueryPoints(i_ownerHash, i_queryPoints, 1 - _dataBeingUsedByJobs);
            }

            var allCopied = (dataCopiedOutDisps || o_resultDisps == null)
                && (dataCopiedOutNorms || o_resultNorms == null)
                && (dataCopiedOutVels || o_resultVels == null);

            return allCopied ? (int)QueryStatus.Success : (int)QueryStatus.ResultsNotReadyYet;
        }

        /// <summary>
        /// Run the jobs
        /// </summary>
        /// <returns>True if jobs kicked off, false if jobs already running.</returns>
        bool ScheduleJobs()
        {
            Debug.Assert(_jobHandle.IsCompleted, "Crest: Expected _jobHandle to be completed before scheduling new jobs.");

            var t = OceanRenderer.Instance.CurrentTime;

            if (_queryDataHeights._lastQueryQuadIndex > 0)
            {
                _jobHandle = JobHandle.CombineDependencies(_jobHandle, new JobSampleHeight
                {
                    _queryPointsX = _queryDataHeights._queryPositionQuadsX[_dataBeingUsedByJobs],
                    _queryPointsZ = _queryDataHeights._queryPositionQuadsZ[_dataBeingUsedByJobs],
                    _framesFlattened = _data._framesFlattenedNative,
                    _t = t,
                    _params = _data._parameters,
                    _seaLevel = OceanRenderer.Instance.SeaLevel,
                    _output = _queryDataHeights._resultQuads0[_dataBeingUsedByJobs],
                }.Schedule(_queryDataHeights._lastQueryQuadIndex, s_jobBatchSize));
            }

            if (_queryDataDisps._lastQueryQuadIndex > 0)
            {
                _jobHandle = JobHandle.CombineDependencies(_jobHandle, new JobSampleDisplacement
                {
                    _queryPointsX = _queryDataDisps._queryPositionQuadsX[_dataBeingUsedByJobs],
                    _queryPointsZ = _queryDataDisps._queryPositionQuadsZ[_dataBeingUsedByJobs],
                    _framesFlattened = _data._framesFlattenedNative,
                    _t = t,
                    _params = _data._parameters,
                    _outputX = _queryDataDisps._resultQuads0[_dataBeingUsedByJobs],
                    _outputY = _queryDataDisps._resultQuads1[_dataBeingUsedByJobs],
                    _outputZ = _queryDataDisps._resultQuads2[_dataBeingUsedByJobs],
                }.Schedule(_queryDataDisps._lastQueryQuadIndex, s_jobBatchSize));
            }

            if (_queryDataNorms._lastQueryQuadIndex > 0)
            {
                _jobHandle = JobHandle.CombineDependencies(_jobHandle, new JobComputeNormal
                {
                    _queryPointsX = _queryDataNorms._queryPositionQuadsX[_dataBeingUsedByJobs],
                    _queryPointsZ = _queryDataNorms._queryPositionQuadsZ[_dataBeingUsedByJobs],
                    _framesFlattened = _data._framesFlattenedNative,
                    _outputNormalX = _queryDataNorms._resultQuads0[_dataBeingUsedByJobs],
                    _outputNormalY = _queryDataNorms._resultQuads1[_dataBeingUsedByJobs],
                    _outputNormalZ = _queryDataNorms._resultQuads2[_dataBeingUsedByJobs],
                    _t = t,
                    _params = _data._parameters,
                }.Schedule(_queryDataNorms._lastQueryQuadIndex, s_jobBatchSize));
            }

            if (_queryDataVels._lastQueryQuadIndex > 0)
            {
                _jobHandle = JobHandle.CombineDependencies(_jobHandle, new JobComputeVerticalVelocity
                {
                    _queryPointsX = _queryDataVels._queryPositionQuadsX[_dataBeingUsedByJobs],
                    _queryPointsZ = _queryDataVels._queryPositionQuadsZ[_dataBeingUsedByJobs],
                    _framesFlattened = _data._framesFlattenedNative,
                    _t = t,
                    _params = _data._parameters,
                    _output = _queryDataVels._resultQuads0[_dataBeingUsedByJobs],
                }.Schedule(_queryDataVels._lastQueryQuadIndex, s_jobBatchSize));
            }

            // The schedule calls put the jobs on a local queue. This ensures they are sent off
            // to worker threads for processing.
            JobHandle.ScheduleBatchedJobs();

            return true;
        }

        /// <summary>
        /// Job to compute height queries
        /// </summary>
#if CREST_UNITY_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        private struct JobSampleHeight : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<float4> _queryPointsX;
            [ReadOnly]
            public NativeArray<float4> _queryPointsZ;

            [ReadOnly]
            public NativeArray<half> _framesFlattened;

            [ReadOnly]
            public float _t;

            [ReadOnly]
            public FFTBakedDataParameters _params;

            [ReadOnly]
            public float _seaLevel;

            [WriteOnly]
            public NativeArray<float4> _output;

            public void Execute(int quadIndex)
            {
                if (quadIndex >= _queryPointsX.Length) return;

                _output[quadIndex] = _seaLevel + FFTBakedData.SampleHeightXZT(_queryPointsX[quadIndex], _queryPointsZ[quadIndex], _t, _params, in _framesFlattened);
            }
        }

        /// <summary>
        /// Job to compute displacement
        /// </summary>
#if CREST_UNITY_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        private struct JobSampleDisplacement : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<float4> _queryPointsX;
            [ReadOnly]
            public NativeArray<float4> _queryPointsZ;

            [ReadOnly]
            public NativeArray<half> _framesFlattened;

            [ReadOnly]
            public float _t;

            [ReadOnly]
            public FFTBakedDataParameters _params;

            [WriteOnly]
            public NativeArray<float4> _outputX;
            [WriteOnly]
            public NativeArray<float4> _outputY;
            [WriteOnly]
            public NativeArray<float4> _outputZ;

            public void Execute(int quadIndex)
            {
                if (quadIndex >= _queryPointsX.Length) return;

                FFTBakedData.SampleDisplacementXZT(_queryPointsX[quadIndex], _queryPointsZ[quadIndex], _t, _params, in _framesFlattened,
                    out var dispX, out var dispY, out var dispZ);

                _outputX[quadIndex] = dispX;
                _outputY[quadIndex] = dispY;
                _outputZ[quadIndex] = dispZ;
            }
        }

        /// <summary>
        /// Job to compute surface normal queries
        /// </summary>
#if CREST_UNITY_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        private struct JobComputeNormal : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<float4> _queryPointsX;
            [ReadOnly]
            public NativeArray<float4> _queryPointsZ;

            [ReadOnly]
            public NativeArray<half> _framesFlattened;

            [ReadOnly]
            public float _t;

            [ReadOnly]
            public FFTBakedDataParameters _params;

            [WriteOnly]
            public NativeArray<float4> _outputNormalX;
            [WriteOnly]
            public NativeArray<float4> _outputNormalY;
            [WriteOnly]
            public NativeArray<float4> _outputNormalZ;

            public void Execute(int quadIndex)
            {
                if (quadIndex >= _queryPointsX.Length) return;

                var x = _queryPointsX[quadIndex];
                var z = _queryPointsZ[quadIndex];

                var height = FFTBakedData.SampleHeightXZT(x, z, _t, _params, in _framesFlattened);
                var height_dx = height - FFTBakedData.SampleHeightXZT(x + s_finiteDiffDx, z, _t, _params, in _framesFlattened);
                var height_dz = height - FFTBakedData.SampleHeightXZT(x, z + s_finiteDiffDx, _t, _params, in _framesFlattened);

                var normal0 = math.normalize(new float3(height_dx[0], s_finiteDiffDx, height_dz[0]));
                var normal1 = math.normalize(new float3(height_dx[1], s_finiteDiffDx, height_dz[1]));
                var normal2 = math.normalize(new float3(height_dx[2], s_finiteDiffDx, height_dz[2]));
                var normal3 = math.normalize(new float3(height_dx[3], s_finiteDiffDx, height_dz[3]));

                _outputNormalX[quadIndex] = new float4(normal0[0], normal1[0], normal2[0], normal3[0]);
                _outputNormalY[quadIndex] = new float4(normal0[1], normal1[1], normal2[1], normal3[1]);
                _outputNormalZ[quadIndex] = new float4(normal0[2], normal1[2], normal2[2], normal3[2]);
            }
        }

        /// <summary>
        /// Job to compute surface velocity. Currently vertical only, this could likely be extended to return full 3D velocity.
        /// </summary>
#if CREST_UNITY_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        private struct JobComputeVerticalVelocity : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<float4> _queryPointsX;
            [ReadOnly]
            public NativeArray<float4> _queryPointsZ;

            [ReadOnly]
            public NativeArray<half> _framesFlattened;

            [ReadOnly]
            public float _t;

            [ReadOnly]
            public FFTBakedDataParameters _params;

            [WriteOnly]
            public NativeArray<float4> _output;

            public void Execute(int quadIndex)
            {
                if (quadIndex >= _queryPointsX.Length) return;

                _output[quadIndex] =
                    (FFTBakedData.SampleHeightXZT(_queryPointsX[quadIndex], _queryPointsZ[quadIndex], _t, _params, in _framesFlattened)
                    - FFTBakedData.SampleHeightXZT(_queryPointsX[quadIndex], _queryPointsZ[quadIndex], _t - s_finiteDiffDt, _params, in _framesFlattened))
                    / s_finiteDiffDt;
            }
        }

        public bool RetrieveSucceeded(int queryStatus)
        {
            return queryStatus == (int)QueryStatus.Success;
        }

        public void UpdateQueries()
        {
            // Ensure jobs are done
            _jobHandle.Complete();

            _dataBeingUsedByJobs = 1 - _dataBeingUsedByJobs;

            // Line up jobs
            ScheduleJobs();

            // Flip data being used by queries vs data being processed by jobs
            _queryDataDisps.Flip();
            _queryDataHeights.Flip();
            _queryDataNorms.Flip();
            _queryDataVels.Flip();
        }

        public void CleanUp()
        {
            // Ensure jobs are done
            _jobHandle.Complete();

            for (var i = 0; i < 2; i++)
            {
                _queryDataHeights._queryPositionQuadsX[i].Dispose();
                _queryDataHeights._queryPositionQuadsZ[i].Dispose();
                if (_queryDataHeights._resultQuads0[i].IsCreated) _queryDataHeights._resultQuads0[i].Dispose();
                if (_queryDataHeights._resultQuads1[i].IsCreated) _queryDataHeights._resultQuads1[i].Dispose();
                if (_queryDataHeights._resultQuads2[i].IsCreated) _queryDataHeights._resultQuads2[i].Dispose();

                _queryDataDisps._queryPositionQuadsX[i].Dispose();
                _queryDataDisps._queryPositionQuadsZ[i].Dispose();
                if (_queryDataDisps._resultQuads0[i].IsCreated) _queryDataDisps._resultQuads0[i].Dispose();
                if (_queryDataDisps._resultQuads1[i].IsCreated) _queryDataDisps._resultQuads1[i].Dispose();
                if (_queryDataDisps._resultQuads2[i].IsCreated) _queryDataDisps._resultQuads2[i].Dispose();

                _queryDataNorms._queryPositionQuadsX[i].Dispose();
                _queryDataNorms._queryPositionQuadsZ[i].Dispose();
                if (_queryDataNorms._resultQuads0[i].IsCreated) _queryDataNorms._resultQuads0[i].Dispose();
                if (_queryDataNorms._resultQuads1[i].IsCreated) _queryDataNorms._resultQuads1[i].Dispose();
                if (_queryDataNorms._resultQuads2[i].IsCreated) _queryDataNorms._resultQuads2[i].Dispose();

                _queryDataVels._queryPositionQuadsX[i].Dispose();
                _queryDataVels._queryPositionQuadsZ[i].Dispose();
                if (_queryDataVels._resultQuads0[i].IsCreated) _queryDataVels._resultQuads0[i].Dispose();
                if (_queryDataVels._resultQuads1[i].IsCreated) _queryDataVels._resultQuads1[i].Dispose();
                if (_queryDataVels._resultQuads2[i].IsCreated) _queryDataVels._resultQuads2[i].Dispose();
            }
        }
    }
}

#endif // CREST_UNITY_MATHEMATICS
