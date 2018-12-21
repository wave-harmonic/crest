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

        public static readonly CollProviderNull Instance = new CollProviderNull();
    }
}
