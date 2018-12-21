// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Collision diagnostics result.
    /// </summary>
    public enum AvailabilityResult
    {
        /// <summary>
        /// Collision data available, good to go
        /// </summary>
        DataAvailable,
        /// <summary>
        /// Collision provider is not fully initialised.
        /// </summary>
        NotInitialisedYet,
        /// <summary>
        /// There is no data available (yet) that covers the query position. This might be because the query was made
        /// before async data started flowing back to the CPU, or the query position may be outside the largest LOD.
        /// </summary>
        NoDataAtThisPosition,
        /// <summary>
        /// A min spatial width was specified with the expectation that wavelengths much smaller than this width would
        /// be filtered out. There is currently no LOD big enough that filters out these wavelengths. Data will still
        /// be return but it will contain wavelengths smaller than expected.
        /// </summary>
        NoLODsBigEnoughToFilterOutWavelengths,
        /// <summary>
        /// This should never be hit, and indicates that the validation logic is broken.
        /// </summary>
        ValidationFailed,
    }

    /// <summary>
    /// Sampling state used to speed up queries.
    /// </summary>
    public class SamplingData
    {
        public object _tag = null;
        public float _minSpatialLength = -1f;
    }

    /// <summary>
    /// Interface for an object that returns ocean surface displacement and height.
    /// </summary>
    public interface ICollProvider
    {
        /// <summary>
        /// Computes sampling state.
        /// </summary>
        /// <param name="i_displacedSamplingArea">The XZ rect in world space that bounds any collision queries.</param>
        /// <param name="i_minSpatialLength">Minimum width or length that we care about. Used to filter out high frequency waves as an optimisation.</param>
        /// <param name="o_samplingData">Result. Needs to be new'd in advance - passing a null pointer is not valid.</param>
        bool GetSamplingData(ref Rect i_displacedSamplingArea, float i_minSpatialLength, SamplingData o_samplingData);

        /// <summary>
        /// Clear sampling data state, call this when done with a state.
        /// </summary>
        void ReturnSamplingData(SamplingData i_data);

        /// <summary>
        /// Samples displacement of ocean surface from the given world position.
        /// </summary>
        bool SampleDisplacement(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement);
        void SampleDisplacementVel(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid);

        /// <summary>
        /// Samples ocean surface height at given world position.
        /// </summary>
        bool SampleHeight(ref Vector3 i_worldPos, SamplingData i_samplingData, out float o_height);

        /// <summary>
        /// Sample ocean normal at an undisplaced world position.
        /// </summary>
        bool SampleNormal(ref Vector3 i_undisplacedWorldPos, SamplingData i_samplingData, out Vector3 o_normal);

        /// <summary>
        /// Computes the position which will be displaced to the given world position.
        /// </summary>
        bool ComputeUndisplacedPosition(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 undisplacedWorldPos);

        /// <summary>
        /// Run diagnostics at a position.
        /// </summary>
        AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, SamplingData i_samplingData);
    }
}
