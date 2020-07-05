// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Interface for an object that returns ocean surface displacement and height.
    /// </summary>
    public interface IFlowProvider
    {
        /// <summary>
        /// Query water flow data (horizontal motion) at a set of points.
        /// </summary>
        /// <param name="i_ownerHash">Unique ID for calling code. Typically acquired by calling GetHashCode().</param>
        /// <param name="i_minSpatialLength">The min spatial length of the object, such as the width of a boat. Useful for filtering out detail when not needed. Set to 0 to get full available detail.</param>
        /// <param name="i_queryPoints">The world space points that will be queried.</param>
        /// <param name="o_resultVels">Water surface flow velocities at the query positions.</param>
        int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, Vector3[] o_resultFlows);

        /// <summary>
        /// Check if query results could be retrieved successfully using return code from Query() function
        /// </summary>
        bool RetrieveSucceeded(int queryStatus);

        /// <summary>
        /// Per frame update callback
        /// </summary>
        void UpdateQueries();

        /// <summary>
        /// On destroy, to cleanup resources
        /// </summary>
        void CleanUp();
    }
}
