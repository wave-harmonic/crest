// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Baked FFT data
    /// </summary>
    public class CollProviderBakedFFT : ICollProvider
    {
        enum QueryStatus
        {
            Success,
            DataMissing,
        }

        public FFTBakedData _data = null;

        const float s_finiteDiffDx = 0.1f;
        const float s_finiteDiffDt = 0.06f;
        const int s_jobBatchSize = 8;

        public CollProviderBakedFFT(FFTBakedData data)
        {
            Debug.Assert(data != null, "Crest: Baked data should not be null.");
            _data = data;
        }

        public int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, Vector3[] o_resultDisps, Vector3[] o_resultNorms, Vector3[] o_resultVels)
        {
            //if (_data == null) return (int)QueryStatus.DataMissing;

            //var t = OceanRenderer.Instance.CurrentTime;

            //if (o_resultDisps != null)
            //{
            //    for (int i = 0; i < o_resultDisps.Length; i++)
            //    {
            //        o_resultDisps[i].x = 0f;
            //        o_resultDisps[i].y = _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t);
            //        o_resultDisps[i].z = 0f;
            //    }
            //}

            //if (o_resultNorms != null)
            //{
            //    for (int i = 0; i < o_resultNorms.Length; i++)
            //    {
            //        float h = _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t);
            //        float h_x = _data.SampleHeight(i_queryPoints[i].x + s_finiteDiffDx, i_queryPoints[i].z, t);
            //        float h_z = _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z + s_finiteDiffDx, t);

            //        o_resultNorms[i].x = h - h_x;
            //        o_resultNorms[i].y = s_finiteDiffDx;
            //        o_resultNorms[i].z = h - h_z;
            //        o_resultNorms[i].Normalize();
            //    }
            //}

            //if (o_resultVels != null)
            //{
            //    for (int i = 0; i < o_resultVels.Length; i++)
            //    {
            //        o_resultVels[i] = Vector3.zero;
            //        o_resultVels[i].y = (_data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t)
            //            - _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t - s_finiteDiffDt)) / s_finiteDiffDt;
            //    }
            //}

            return (int)QueryStatus.Success;
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
            if (_data == null || _data._framesFlattenedNative.Length == 0) 
                return (int)QueryStatus.DataMissing;

            var t = OceanRenderer.Instance.CurrentTime;
            var seaLevel = OceanRenderer.Instance.SeaLevel;

            // Queries processed in groups of 4 for SIMD - 'quads'
            var numQueryQuads = (o_resultHeights.Length + 3) / 4;
            var queryPointsX = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var queryPointsZ = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            // Copy input data. Could be avoided if query api is changed to use NAs.
            for (int i = 0; i < numQueryQuads; i++)
            {
                queryPointsX[i] = new float4(i_queryPoints[i * 4].x, i_queryPoints[i * 4 + 1].x, i_queryPoints[i * 4 + 2].x, i_queryPoints[i * 4 + 3].x);
                queryPointsZ[i] = new float4(i_queryPoints[i * 4].z, i_queryPoints[i * 4 + 1].z, i_queryPoints[i * 4 + 2].z, i_queryPoints[i * 4 + 3].z);
            }

            if (o_resultHeights != null)
            {
                // One thread per quad - per group of 4 queries
                var results = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                // Run job synchronously
                new JobSampleHeight
                {
                    _queryPointsX = queryPointsX,
                    _queryPointsZ = queryPointsZ,
                    _framesFlattened = _data._framesFlattenedNative,
                    _t = t,
                    _params = _data._parameters,
                    _seaLevel = seaLevel,
                    _output = results,
                }.Schedule(numQueryQuads, s_jobBatchSize).Complete();

                // Copy results to output. Could be avoided if query api was changed to NAs.
                for (int i = 0; i < o_resultHeights.Length; i++)
                {
                    o_resultHeights[i] = results[i / 4][i % 4];
                }

                results.Dispose();
            }

            if (o_resultNorms != null)
            {
                // One thread per quad - per group of 4 queries
                var results = new NativeArray<float3>(4 * (o_resultNorms.Length + 3) / 4, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                // Run job synchronously
                new JobComputeNormal
                {
                    _queryPointsX = queryPointsX,
                    _queryPointsZ = queryPointsZ,
                    _framesFlattened = _data._framesFlattenedNative,
                    _output = results,
                    _t = t,
                    _params = _data._parameters,
                }.Schedule(i_queryPoints.Length / 4, s_jobBatchSize).Complete();

                // Copy results to output. Could be avoided if query api was changed to NAs.
                for (int i = 0; i < o_resultNorms.Length; i++)
                {
                    o_resultNorms[i] = results[i];
                }

                results.Dispose();
            }

            if (o_resultVels != null)
            {
                // One thread per quad - per group of 4 queries
                var results = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                // Run job synchronously
                new JobComputeVerticalVelocity
                {
                    _queryPointsX = queryPointsX,
                    _queryPointsZ = queryPointsZ,
                    _framesFlattened = _data._framesFlattenedNative,
                    _t = t,
                    _params = _data._parameters,
                    _output = results,
                }.Schedule(numQueryQuads, s_jobBatchSize).Complete();

                // Copy results to output. Could be avoided if query api was changed to NAs.
                for (int i = 0; i < o_resultHeights.Length; i++)
                {
                    o_resultVels[i].y = results[i / 4][i % 4];

                    o_resultVels[i].x = o_resultVels[i].z = 0f;
                }

                results.Dispose();
            }

            // Clean up query points
            queryPointsX.Dispose();
            queryPointsZ.Dispose();

            return (int)QueryStatus.Success;
        }

        /// <summary>
        /// Job to compute height queries
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
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
        /// Job to compute surface normal queries
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
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
            public NativeArray<float3> _output;

            public void Execute(int quadIndex)
            {
                if (quadIndex >= _queryPointsX.Length) return;

                var x = _queryPointsX[quadIndex];
                var z = _queryPointsZ[quadIndex];

                var height = FFTBakedData.SampleHeightXZT(x, z, _t, _params, in _framesFlattened);
                var height_dx = height - FFTBakedData.SampleHeightXZT(x + s_finiteDiffDx, z, _t, _params, in _framesFlattened);
                var height_dz = height - FFTBakedData.SampleHeightXZT(x, z + s_finiteDiffDx, _t, _params, in _framesFlattened);
                
                _output[math.mad(quadIndex, 4, 0)] = math.normalize(new float3(height_dx.x, s_finiteDiffDx, height_dz.x));
                _output[math.mad(quadIndex, 4, 1)] = math.normalize(new float3(height_dx.y, s_finiteDiffDx, height_dz.y));
                _output[math.mad(quadIndex, 4, 2)] = math.normalize(new float3(height_dx.z, s_finiteDiffDx, height_dz.z));
                _output[math.mad(quadIndex, 4, 3)] = math.normalize(new float3(height_dx.w, s_finiteDiffDx, height_dz.w));
            }
        }

        /// <summary>
        /// Job to compute surface velocity, vertical only as we have height maps
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
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
        }

        public void CleanUp()
        {
        }
    }
}
