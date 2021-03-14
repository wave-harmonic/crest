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
        public int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, Vector3[] o_resultDisps, Vector3[] o_resultNorms, Vector3[] o_resultVels)
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

        public int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, float[] o_resultHeights, Vector3[] o_resultNorms, Vector3[] o_resultVels)
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

        public void UpdateQueries()
        {
        }

        public void CleanUp()
        {
        }

        public readonly static CollProviderNull Instance = new CollProviderNull();
    }
}
