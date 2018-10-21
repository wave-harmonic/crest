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
        bool SampleDisplacement(ref Vector3 in__worldPos, out Vector3 displacement);
        bool SampleDisplacement(ref Vector3 in__worldPos, out Vector3 displacement, float minSpatialLength);
        void SampleDisplacementVel(ref Vector3 in__worldPos, out Vector3 displacement, out bool displacementValid, out Vector3 displacementVel, out bool velValid, float minSpatialLength);

        /// <summary>
        /// Samples ocean surface height at given world position.
        /// </summary>
        bool SampleHeight(ref Vector3 in__worldPos, out float height);
        bool SampleHeight(ref Vector3 in__worldPos, out float height, float minSpatialLength);

        /// <summary>
        /// Sample ocean normal at an undisplaced world position.
        /// </summary>
        bool SampleNormal(ref Vector3 in__undisplacedWorldPos, out Vector3 normal);
        bool SampleNormal(ref Vector3 in__undisplacedWorldPos, out Vector3 normal, float minSpatialLength);

        /// <summary>
        /// Computes the position which will be displaced to the given world position.
        /// </summary>
        bool ComputeUndisplacedPosition(ref Vector3 in__worldPos, out Vector3 undisplacedWorldPos, float minSpatialLength);

        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this to set up a sampling area and
        /// then use the 'area' sampling functions below.
        /// </summary>
        bool PrewarmForSamplingArea(Rect areaXZ);
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
        bool SampleDisplacementInArea(ref Vector3 in__worldPos, out Vector3 displacement);
        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this after setting up a sampling area using the Prewarm function.
        /// </summary>
        void SampleDisplacementVelInArea(ref Vector3 in__worldPos, out Vector3 displacement, out bool displacementValid, out Vector3 displacementVel, out bool velValid);
        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this after setting up a sampling area using the Prewarm function.
        /// </summary>
        bool SampleNormalInArea(ref Vector3 in__undisplacedWorldPos, out Vector3 normal);

        // These 'InArea' variants cannot exist because these perform a dynamic search and the area cannot be predicted in advance
        //bool SampleNormalInArea(ref Vector3 in__undisplacedWorldPos, out Vector3 normal);
        //bool ComputeUndisplacedPositionInArea(ref Vector3 in__worldPos, out Vector3 undisplacedWorldPos);

        AvailabilityResult CheckAvailability(ref Vector3 in__worldPos, float minSpatialLength);
    }
}
