// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
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
        bool SampleDisplacementVel(ref Vector3 in__worldPos, out Vector3 displacement, out Vector3 displacementVel, float minSpatialLength);

        /// <summary>
        /// Samples ocean surface height at given world position.
        /// </summary>
        bool SampleHeight(ref Vector3 in__worldPos, out float height);

        /// <summary>
        /// Sample ocean normal at an undisplaced world position.
        /// </summary>
        bool SampleNormal(ref Vector3 in__undisplacedWorldPos, out Vector3 normal);
        bool SampleNormal(ref Vector3 in__undisplacedWorldPos, out Vector3 normal, float minSpatialLength);

        /// <summary>
        /// Computes the position which will be displaced to the given world position.
        /// </summary>
        bool ComputeUndisplacedPosition(ref Vector3 in__worldPos, out Vector3 undisplacedWorldPos);

        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this to set up a sampling area and
        /// then use the 'area' sampling functions below.
        /// </summary>
        bool PrewarmForSamplingArea(Rect areaXZ);
        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this to set up a sampling area and
        /// then use the 'area' sampling functions below. Min spatial length is the minimum side length that you
        /// care about - if a boat has dimensions 3m x 2m, set this to 2, and any wavelengths much smaller than 2m
        /// will be ignored.
        /// </summary>
        bool PrewarmForSamplingArea(Rect areaXZ, float minSpatialLength);
        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this after setting up a sampling area.
        /// </summary>
        bool SampleDisplacementInArea(ref Vector3 in__worldPos, out Vector3 displacement);
        /// <summary>
        /// Some collision providers benefit from getting prewarmed - call this after setting up a sampling area.
        /// </summary>
        bool SampleHeightInArea(ref Vector3 in__worldPos, out float height);
    }
}
