// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
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
        // Tag is only used by displacement texture readback. In the future this class could be removed completely and replaced with
        // just the min spatial length float
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
        [Obsolete("This API is deprecated. Use the 'Query' APIs instead.")]
        bool SampleDisplacement(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement);
        [Obsolete("This API is deprecated. Use the 'Query' APIs instead.")]
        void SampleDisplacementVel(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid);

        /// <summary>
        /// Samples ocean surface height at given world position.
        /// </summary>
        [Obsolete("This API is deprecated. Use the 'Query' APIs instead.")]
        bool SampleHeight(ref Vector3 i_worldPos, SamplingData i_samplingData, out float o_height);

        /// <summary>
        /// Sample ocean normal at an undisplaced world position.
        /// </summary>
        [Obsolete("This API is deprecated. Use the 'Query' APIs instead.")]
        bool SampleNormal(ref Vector3 i_undisplacedWorldPos, SamplingData i_samplingData, out Vector3 o_normal);

        /// <summary>
        /// Computes the position which will be displaced to the given world position.
        /// </summary>
        [Obsolete("This API is deprecated. Use the 'Query' APIs instead.")]
        bool ComputeUndisplacedPosition(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 undisplacedWorldPos);

        /// <summary>
        /// Query water physical data at a set of points. Pass in null to any out parameters that are not required.
        /// </summary>
        /// <param name="i_ownerHash">Unique ID for calling code. Typically acquired by calling GetHashCode().</param>
        /// <param name="i_samplingData">Sampling data to inform sampling, obtained by calling GetSamplingData().</param>
        /// <param name="i_queryPoints">The world space points that will be queried.</param>
        /// <param name="o_resultHeights">Float array of water heights at the query positions. Pass null if this information is not required.</param>
        /// <param name="o_resultNorms">Water normals at the query positions. Pass null if this information is not required.</param>
        /// <param name="o_resultVels">Water surface velocities at the query positions. Pass null if this information is not required.</param>
        int Query(int i_ownerHash, SamplingData i_samplingData, Vector3[] i_queryPoints, float[] o_resultHeights, Vector3[] o_resultNorms, Vector3[] o_resultVels);

        /// <summary>
        /// Query water physical data at a set of points. Pass in null to any out parameters that are not required.
        /// </summary>
        /// <param name="i_ownerHash">Unique ID for calling code. Typically acquired by calling GetHashCode().</param>
        /// <param name="i_samplingData">Sampling data to inform sampling, obtained by calling GetSamplingData().</param>
        /// <param name="i_queryPoints">The world space points that will be queried.</param>
        /// <param name="o_resultDisps">Displacement vectors for water surface points that will displace to the XZ coordinates of the query points. Water heights are given by sea level plus the y component of the displacement.</param>
        /// <param name="o_resultNorms">Water normals at the query positions. Pass null if this information is not required.</param>
        /// <param name="o_resultVels">Water surface velocities at the query positions. Pass null if this information is not required.</param>
        int Query(int i_ownerHash, SamplingData i_samplingData, Vector3[] i_queryPoints, Vector3[] o_resultDisps, Vector3[] o_resultNorms, Vector3[] o_resultVels);

        /// <summary>
        /// Check if query results could be retrieved successfully using return code from Query() function
        /// </summary>
        bool RetrieveSucceeded(int queryStatus);

        /// <summary>
        /// Run diagnostics at a position.
        /// </summary>
        AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, SamplingData i_samplingData);
    }
}
