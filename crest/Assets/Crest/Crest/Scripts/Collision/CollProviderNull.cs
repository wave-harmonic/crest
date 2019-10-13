// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Gives a flat, still ocean.
    /// </summary>
    public class CollProviderNull : ICollProvider
    {
        public AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, SamplingData i_samplingData)
        {
            return AvailabilityResult.DataAvailable;
        }

        public bool ComputeUndisplacedPosition(ref Vector3 i_worldPos, out Vector3 undisplacedWorldPos, float minSpatialLength = 0)
        {
            undisplacedWorldPos = i_worldPos;
            undisplacedWorldPos.y = OceanRenderer.Instance.SeaLevel;
            return true;
        }

        public bool ComputeUndisplacedPosition(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 undisplacedWorldPos)
        {
            undisplacedWorldPos = i_worldPos;
            undisplacedWorldPos.y = OceanRenderer.Instance.SeaLevel;
            return true;
        }

        public bool GetSamplingData(ref Rect i_displacedSamplingArea, float i_minSpatialLength, SamplingData o_samplingData2)
        {
            return true;
        }

        public void ReturnSamplingData(SamplingData i_data)
        {
        }

        public bool SampleDisplacement(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement)
        {
            o_displacement = Vector3.zero;
            return true;
        }

        public void SampleDisplacementVel(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid)
        {
            o_displacement = Vector3.zero;
            o_displacementValid = true;
            o_displacementVel = Vector3.zero;
            o_velValid = true;
        }

        public bool SampleHeight(ref Vector3 i_worldPos, SamplingData i_samplingData, out float o_height)
        {
            o_height = OceanRenderer.Instance.SeaLevel;
            return true;
        }

        public bool SampleNormal(ref Vector3 i_undisplacedWorldPos, SamplingData i_samplingData, out Vector3 o_normal)
        {
            o_normal = Vector3.up;
            return true;
        }

        public int Query(int i_ownerHash, SamplingData i_samplingData, Vector3[] i_queryPoints, Vector3[] o_resultDisps, Vector3[] o_resultNorms, Vector3[] o_resultVels)
        {
            if (o_resultDisps != null)
            {
                for (int i = 0; i < o_resultDisps.Length; i++)
                {
                    o_resultDisps[i] = Vector3.zero;
                }
            }

            if (o_resultNorms != null)
            {
                for (int i = 0; i < o_resultNorms.Length; i++)
                {
                    o_resultNorms[i] = Vector3.up;
                }
            }

            if (o_resultVels != null)
            {
                for (int i = 0; i < o_resultVels.Length; i++)
                {
                    o_resultVels[i] = Vector3.zero;
                }
            }

            return 0;
        }

        public int Query(int i_ownerHash, SamplingData i_samplingData, Vector3[] i_queryPoints, float[] o_resultHeights, Vector3[] o_resultNorms, Vector3[] o_resultVels)
        {
            if (o_resultHeights != null)
            {
                for (int i = 0; i < o_resultHeights.Length; i++)
                {
                    o_resultHeights[i] = 0f;
                }
            }

            if (o_resultNorms != null)
            {
                for (int i = 0; i < o_resultNorms.Length; i++)
                {
                    o_resultNorms[i] = Vector3.up;
                }
            }

            if (o_resultVels != null)
            {
                for (int i = 0; i < o_resultVels.Length; i++)
                {
                    o_resultVels[i] = Vector3.zero;
                }
            }

            return 0;
        }

        public bool RetrieveSucceeded(int queryStatus)
        {
            return true;
        }

        public static readonly CollProviderNull Instance = new CollProviderNull();
    }
}
