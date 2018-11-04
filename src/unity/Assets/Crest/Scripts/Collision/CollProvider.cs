// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
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
        /// Samples displacement of ocean surface from the given world position.
        /// </summary>
        bool SampleDisplacement(ref Vector3 i_worldPos, out Vector3 o_displacement, float minSpatialLength = 0f);
        void SampleDisplacementVel(ref Vector3 i_worldPos, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid, float minSpatialLength = 0f);

        /// <summary>
        /// Samples ocean surface height at given world position.
        /// </summary>
        bool SampleHeight(ref Vector3 i_worldPos, out float height, float minSpatialLength = 0f);

        /// <summary>
        /// Sample ocean normal at an undisplaced world position.
        /// </summary>
        bool SampleNormal(ref Vector3 in__undisplacedWorldPos, out Vector3 o_normal, float minSpatialLength = 0f);

        /// <summary>
        /// Computes the position which will be displaced to the given world position.
        /// </summary>
        bool ComputeUndisplacedPosition(ref Vector3 i_worldPos, out Vector3 undisplacedWorldPos, float minSpatialLength = 0f);

        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this to set up a sampling area and
        /// then use the 'area' sampling functions below. Min spatial length is the minimum side length that you
        /// care about. For e.g. if a boat has dimensions 3m x 2m, set this to 2, and then suitable wavelengths will
        /// be preferred.
        /// </summary>
        bool PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength);
        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this after setting up a sampling area using the Prewarm function.
        /// </summary>
        bool SampleDisplacementInArea(ref Vector3 i_worldPos, out Vector3 o_displacement);
        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this after setting up a sampling area using the Prewarm function.
        /// </summary>
        void SampleDisplacementVelInArea(ref Vector3 i_worldPos, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid);
        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this after setting up a sampling area using the Prewarm function.
        /// </summary>
        bool SampleNormalInArea(ref Vector3 in__undisplacedWorldPos, out Vector3 o_normal);

        // NOTE: These 'InArea' variants cannot exist because these perform a dynamic search and the area cannot be predicted in advance
        //bool SampleNormalInArea(ref Vector3 in__undisplacedWorldPos, out Vector3 o_normal);
        //bool ComputeUndisplacedPositionInArea(ref Vector3 i_worldPos, out Vector3 undisplacedWorldPos);

        AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, float minSpatialLength);
    }
}
