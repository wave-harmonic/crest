// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Gives a stationary ocean (no horizontal flow).
    /// </summary>
    public class FlowProviderNull : IFlowProvider
    {
        public int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, Vector3[] o_resultFlows)
        {
            if (o_resultFlows != null)
            {
                for (int i = 0; i < o_resultFlows.Length; i++)
                {
                    o_resultFlows[i] = Vector3.zero;
                }
            }

            return 0;
        }

        public bool RetrieveSucceeded(int queryStatus)
        {
            return true;
        }

        public void UpdateQueries()
        {
        }

        public void CleanUp()
        {
        }

        public readonly static CollProviderNull Instance = new CollProviderNull();
    }
}
