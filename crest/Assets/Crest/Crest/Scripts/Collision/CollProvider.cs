// Crest Ocean System

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
    /// Interface for an object that returns ocean surface displacement and height.
    /// </summary>
    public interface ICollProvider
    {
        /// <summary>
        /// Query water physical data at a set of points. Pass in null to any out parameters that are not required.
        /// </summary>
        /// <param name="i_ownerHash">Unique ID for calling code. Typically acquired by calling GetHashCode().</param>
        /// <param name="i_minSpatialLength">The min spatial length of the object, such as the width of a boat. Useful for filtering out detail when not needed. Set to 0 to get full available detail.</param>
        /// <param name="i_queryPoints">The world space points that will be queried.</param>
        /// <param name="o_resultHeights">Float array of water heights at the query positions. Pass null if this information is not required.</param>
        /// <param name="o_resultNorms">Water normals at the query positions. Pass null if this information is not required.</param>
        /// <param name="o_resultVels">Water surface velocities at the query positions. Pass null if this information is not required.</param>
        int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, float[] o_resultHeights, Vector3[] o_resultNorms, Vector3[] o_resultVels);

        /// <summary>
        /// Query water physical data at a set of points. Pass in null to any out parameters that are not required.
        /// </summary>
        /// <param name="i_ownerHash">Unique ID for calling code. Typically acquired by calling GetHashCode().</param>
        /// <param name="i_minSpatialLength">The min spatial length of the object, such as the width of a boat. Useful for filtering out detail when not needed. Set to 0 to get full available detail.</param>
        /// <param name="i_queryPoints">The world space points that will be queried.</param>
        /// <param name="o_resultDisps">Displacement vectors for water surface points that will displace to the XZ coordinates of the query points. Water heights are given by sea level plus the y component of the displacement.</param>
        /// <param name="o_resultNorms">Water normals at the query positions. Pass null if this information is not required.</param>
        /// <param name="o_resultVels">Water surface velocities at the query positions. Pass null if this information is not required.</param>
        int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, Vector3[] o_resultDisps, Vector3[] o_resultNorms, Vector3[] o_resultVels);

        /// <summary>
        /// Check if query results could be retrieved successfully using return code from Query() function
        /// </summary>
        bool RetrieveSucceeded(int queryStatus);

        /// <summary>
        /// Run diagnostics at a position.
        /// </summary>
        AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, float i_minSpatialLength);
    }
}
