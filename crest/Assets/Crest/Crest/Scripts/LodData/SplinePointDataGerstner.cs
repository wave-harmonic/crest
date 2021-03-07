// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Custom spline point data for Gerstner waves
    /// </summary>
    public class SplinePointDataGerstner : MonoBehaviour, ISplinePointCustomData
    {
        [Tooltip("Weight multiplier to scale waves."), SerializeField]
        float _weight = 1f;

        public Vector2 GetData()
        {
            return new Vector2(_weight, 0f);
        }
    }
}
