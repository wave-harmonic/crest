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

        public int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, float[] o_resultHeights, Vector3[] o_resultNorms, Vector3[] o_resultVels)
        {
            //if (_data == null) return (int)QueryStatus.DataMissing;

            //var t = OceanRenderer.Instance.CurrentTime;
            //var seaLevel = OceanRenderer.Instance.SeaLevel;

            //if (o_resultHeights != null)
            //{
            //    for (int i = 0; i < o_resultHeights.Length; i++)
            //    {
            //        o_resultHeights[i] = seaLevel + _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t);
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
            //        // 3D velocities not available (if we only bake height)
            //        o_resultVels[i].x = 0f;
            //        o_resultVels[i].y = (_data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t)
            //            - _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t - s_finiteDiffDt)) / s_finiteDiffDt;
            //        o_resultVels[i].z = 0f;
            //    }
            //}

            return (int)QueryStatus.Success;
        }
        
        public int QueryBurst(
            int i_ownerHash,
            Vector3[] i_queryPoints,
            float[] o_resultHeights
            //ref NativeArray<float3> i_queryPoints,
            //ref NativeArray<float> o_resultHeights
            //ref NativeArray<float3> o_resultNorms,
            //ref NativeArray<float3> o_resultVels
            //in FFTBakedDataParameters bakedDataParameters,
            //in NativeArray<float> framesFlattened, 
            //float t,
            //float seaLevel
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

            if (o_resultHeights.Length > 0)
            {
                // One thread per quad - per group of 4 queries
                var results = new NativeArray<float4>(numQueryQuads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                // Seems to work fairly well in my tests
                var batchSize = 12;

                // Run height sample job synchronously
                new JobSampleHeightFromFFTData
                {
                    _queryPointsX = queryPointsX,
                    _queryPointsZ = queryPointsZ,
                    _framesFlattened = _data._framesFlattenedNative,
                    _t = t,
                    _params = _data._parameters,
                    _seaLevel = seaLevel,
                    _output = results,
                }.Schedule(numQueryQuads, batchSize).Complete();

                // Copy results to output. Could be avoided if query api was changed to NAs.
                for (int i = 0; i < o_resultHeights.Length; i++)
                {
                    o_resultHeights[i] = results[i / 4][i % 4];
                }

                results.Dispose();
            }

            //if (o_resultNorms.Length > 0)
            //{
            //    for (int i = 0; i < o_resultNorms.Length; i++)
            //    {
            //        float h = FFTBakedData.SampleHeightBurst(i_queryPoints[i].x, i_queryPoints[i].z, t, bakedDataParameters, framesFlattened);
            //        float h_x = FFTBakedData.SampleHeightBurst(i_queryPoints[i].x + s_finiteDiffDx, i_queryPoints[i].z, t, bakedDataParameters, in framesFlattened);
            //        float h_z = FFTBakedData.SampleHeightBurst(i_queryPoints[i].x, i_queryPoints[i].z + s_finiteDiffDx, t, bakedDataParameters, in framesFlattened);

            //        var normal = new Vector3(h - h_x, s_finiteDiffDx, h - h_z);
            //        normal.Normalize();
            //        o_resultNorms[i] = normal;
            //    }
            //}

            //if (o_resultVels.Length > 0)
            //{
            //    for (int i = 0; i < o_resultVels.Length; i++)
            //    {
            //        // 3D velocities not available (if we only bake height)
            //        o_resultVels[i] = new Vector3(0f, 
            //            (FFTBakedData.SampleHeightBurst(i_queryPoints[i].x, i_queryPoints[i].z, t, bakedDataParameters, in framesFlattened)
            //             - FFTBakedData.SampleHeightBurst(i_queryPoints[i].x, i_queryPoints[i].z, t - s_finiteDiffDt, bakedDataParameters, in framesFlattened)) / s_finiteDiffDt,
            //            0f);
            //    }
            //}

            queryPointsX.Dispose();
            queryPointsZ.Dispose();

            return (int)QueryStatus.Success;
        }

        /// <summary>
        /// Job to 
        /// </summary>
        // Set CompileSynchronously to true to make sure that the method will not be compiled asynchronously
        // but on the first schedule
        [BurstCompile(CompileSynchronously = true)]
        private struct JobSampleHeightFromFFTData : IJobParallelFor
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

                _output[quadIndex] = _seaLevel + FFTBakedData.SampleHeightBurst(_queryPointsX[quadIndex], _queryPointsZ[quadIndex], _t, _params, in _framesFlattened);
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
