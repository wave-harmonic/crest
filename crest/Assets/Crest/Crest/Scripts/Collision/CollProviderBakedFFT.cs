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
    /// Collision provider for baked FFT data
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
            if (_data == null || _data._framesFlattenedNative.Length == 0)
                return (int)QueryStatus.DataMissing;

            var t = OceanRenderer.Instance.CurrentTime;

            // Queries processed in groups of 4 for SIMD - 'quads'
            var numQueryQuads = (o_resultDisps.Length + 3) / 4;
            var queryPointsX = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var queryPointsZ = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.ClearMemory);

            // Copy input data. Could be avoided if query api is changed to use NAs.
            for (int i = 0; i < i_queryPoints.Length; i++)
            {
                var quadIdx = i / 4;
                var xQuad = queryPointsX[quadIdx];
                var zQuad = queryPointsZ[quadIdx];

                var quadComp = i % 4;
                xQuad[quadComp] = i_queryPoints[i].x;
                zQuad[quadComp] = i_queryPoints[i].z;

                queryPointsX[quadIdx] = xQuad;
                queryPointsZ[quadIdx] = zQuad;
            }

            if (o_resultDisps != null)
            {
                // One thread per quad - per group of 4 queries
                var resultsX = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var resultsY = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var resultsZ = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                // Run job synchronously
                new JobSampleDisplacement
                {
                    _queryPointsX = queryPointsX,
                    _queryPointsZ = queryPointsZ,
                    _framesFlattened = _data._framesFlattenedNative,
                    _t = t,
                    _params = _data._parameters,
                    _outputX = resultsX,
                    _outputY = resultsY,
                    _outputZ = resultsZ,
                }.Schedule(numQueryQuads, s_jobBatchSize).Complete();

                // Copy results to output. Could be avoided if query api was changed to NAs.
                for (int i = 0; i < o_resultDisps.Length; i++)
                {
                    o_resultDisps[i].x = resultsX[i / 4][i % 4];
                    o_resultDisps[i].y = resultsY[i / 4][i % 4];
                    o_resultDisps[i].z = resultsZ[i / 4][i % 4];
                }

                resultsX.Dispose();
                resultsY.Dispose();
                resultsZ.Dispose();
            }

            if (o_resultNorms != null)
            {
                var normalX = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var normalY = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var normalZ = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                // Run job synchronously
                new JobComputeNormal
                {
                    _queryPointsX = queryPointsX,
                    _queryPointsZ = queryPointsZ,
                    _framesFlattened = _data._framesFlattenedNative,
                    _outputNormalX = normalX,
                    _outputNormalY = normalY,
                    _outputNormalZ = normalZ,
                    _t = t,
                    _params = _data._parameters,
                }.Schedule(numQueryQuads, s_jobBatchSize).Complete();

                // Copy results to output. Could be avoided if query api was changed to NAs.
                for (int i = 0; i < o_resultNorms.Length; i++)
                {
                    var quad = i / 4;
                    var quadComp = i % 4;

                    Vector3 norm;
                    norm.x = normalX[quad][quadComp];
                    norm.y = normalY[quad][quadComp];
                    norm.z = normalZ[quad][quadComp];
                    o_resultNorms[i] = norm;
                }

                normalX.Dispose();
                normalY.Dispose();
                normalZ.Dispose();
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
                for (int i = 0; i < o_resultVels.Length; i++)
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
            var queryPointsX = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var queryPointsZ = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.ClearMemory);

            // Copy input data. Could be avoided if query api is changed to use NAs.
            for (var i = 0; i < i_queryPoints.Length; i++)
            {
                var quadIdx = i / 4;
                var xQuad = queryPointsX[quadIdx];
                var zQuad = queryPointsZ[quadIdx];

                var quadComp = i % 4;
                xQuad[quadComp] = i_queryPoints[i].x;
                zQuad[quadComp] = i_queryPoints[i].z;

                queryPointsX[quadIdx] = xQuad;
                queryPointsZ[quadIdx] = zQuad;
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
                var normalX = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var normalY = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var normalZ = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                // Run job synchronously
                new JobComputeNormal
                {
                    _queryPointsX = queryPointsX,
                    _queryPointsZ = queryPointsZ,
                    _framesFlattened = _data._framesFlattenedNative,
                    _outputNormalX = normalX,
                    _outputNormalY = normalY,
                    _outputNormalZ = normalZ,
                    _t = t,
                    _params = _data._parameters,
                }.Schedule(numQueryQuads, s_jobBatchSize).Complete();

                // Copy results to output. Could be avoided if query api was changed to NAs.
                for (int i = 0; i < o_resultNorms.Length; i++)
                {
                    var quad = i / 4;
                    var quadComp = i % 4;

                    Vector3 norm;
                    norm.x = normalX[quad][quadComp];
                    norm.y = normalY[quad][quadComp];
                    norm.z = normalZ[quad][quadComp];
                    o_resultNorms[i] = norm;
                }

                normalX.Dispose();
                normalY.Dispose();
                normalZ.Dispose();
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
                for (int i = 0; i < o_resultVels.Length; i++)
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
        /// Job to compute displacement
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
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
