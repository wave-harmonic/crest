// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Unity.Collections;
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
            if (_data == null) return (int)QueryStatus.DataMissing;

            var t = OceanRenderer.Instance.CurrentTime;

            if (o_resultDisps != null)
            {
                for (int i = 0; i < o_resultDisps.Length; i++)
                {
                    o_resultDisps[i].x = 0f;
                    o_resultDisps[i].y = _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t);
                    o_resultDisps[i].z = 0f;
                }
            }

            if (o_resultNorms != null)
            {
                for (int i = 0; i < o_resultNorms.Length; i++)
                {
                    float h = _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t);
                    float h_x = _data.SampleHeight(i_queryPoints[i].x + s_finiteDiffDx, i_queryPoints[i].z, t);
                    float h_z = _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z + s_finiteDiffDx, t);

                    o_resultNorms[i].x = h - h_x;
                    o_resultNorms[i].y = s_finiteDiffDx;
                    o_resultNorms[i].z = h - h_z;
                    o_resultNorms[i].Normalize();
                }
            }

            if (o_resultVels != null)
            {
                for (int i = 0; i < o_resultVels.Length; i++)
                {
                    o_resultVels[i] = Vector3.zero;
                    o_resultVels[i].y = (_data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t)
                        - _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t - s_finiteDiffDt)) / s_finiteDiffDt;
                }
            }

            return (int)QueryStatus.Success;
        }

        public int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, float[] o_resultHeights, Vector3[] o_resultNorms, Vector3[] o_resultVels)
        {
            if (_data == null) return (int)QueryStatus.DataMissing;

            var t = OceanRenderer.Instance.CurrentTime;
            var seaLevel = OceanRenderer.Instance.SeaLevel;

            if (o_resultHeights != null)
            {
                for (int i = 0; i < o_resultHeights.Length; i++)
                {
                    o_resultHeights[i] = seaLevel + _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t);
                }
            }

            if (o_resultNorms != null)
            {
                for (int i = 0; i < o_resultNorms.Length; i++)
                {
                    float h = _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t);
                    float h_x = _data.SampleHeight(i_queryPoints[i].x + s_finiteDiffDx, i_queryPoints[i].z, t);
                    float h_z = _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z + s_finiteDiffDx, t);

                    o_resultNorms[i].x = h - h_x;
                    o_resultNorms[i].y = s_finiteDiffDx;
                    o_resultNorms[i].z = h - h_z;
                    o_resultNorms[i].Normalize();
                }
            }

            if (o_resultVels != null)
            {
                for (int i = 0; i < o_resultVels.Length; i++)
                {
                    // 3D velocities not available (if we only bake height)
                    o_resultVels[i].x = 0f;
                    o_resultVels[i].y = (_data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t)
                        - _data.SampleHeight(i_queryPoints[i].x, i_queryPoints[i].z, t - s_finiteDiffDt)) / s_finiteDiffDt;
                    o_resultVels[i].z = 0f;
                }
            }

            return (int)QueryStatus.Success;
        }
        
        public static int QueryBurst(
            ref NativeArray<Vector3> i_queryPoints,
            ref NativeArray<float> o_resultHeights,
            ref NativeArray<Vector3> o_resultNorms,
            ref NativeArray<Vector3> o_resultVels,
            in FFTBakedDataParameters bakedDataParameters,
            in NativeArray<float> framesFlattened, 
            float t, 
            float seaLevel)
        {
            if (framesFlattened.Length == 0) 
                return (int)QueryStatus.DataMissing;

            if (o_resultHeights.Length > 0)
            {
                for (int i = 0; i < o_resultHeights.Length; i++)
                {
                    o_resultHeights[i] = seaLevel + FFTBakedData.SampleHeightBurst(i_queryPoints[i].x, i_queryPoints[i].z, t, bakedDataParameters, in framesFlattened);
                }
            }

            if (o_resultNorms.Length > 0)
            {
                for (int i = 0; i < o_resultNorms.Length; i++)
                {
                    float h = FFTBakedData.SampleHeightBurst(i_queryPoints[i].x, i_queryPoints[i].z, t, bakedDataParameters, framesFlattened);
                    float h_x = FFTBakedData.SampleHeightBurst(i_queryPoints[i].x + s_finiteDiffDx, i_queryPoints[i].z, t, bakedDataParameters, in framesFlattened);
                    float h_z = FFTBakedData.SampleHeightBurst(i_queryPoints[i].x, i_queryPoints[i].z + s_finiteDiffDx, t, bakedDataParameters, in framesFlattened);

                    var normal = new Vector3(h - h_x, s_finiteDiffDx, h - h_z);
                    normal.Normalize();
                    o_resultNorms[i] = normal;
                }
            }

            if (o_resultVels.Length > 0)
            {
                for (int i = 0; i < o_resultVels.Length; i++)
                {
                    // 3D velocities not available (if we only bake height)
                    o_resultVels[i] = new Vector3(0f, 
                        (FFTBakedData.SampleHeightBurst(i_queryPoints[i].x, i_queryPoints[i].z, t, bakedDataParameters, in framesFlattened)
                         - FFTBakedData.SampleHeightBurst(i_queryPoints[i].x, i_queryPoints[i].z, t - s_finiteDiffDt, bakedDataParameters, in framesFlattened)) / s_finiteDiffDt,
                        0f);
                }
            }

            return (int)QueryStatus.Success;
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
