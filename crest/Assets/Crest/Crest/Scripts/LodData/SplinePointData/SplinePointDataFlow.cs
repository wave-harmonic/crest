// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Custom spline point data for flow
    /// </summary>
    [AddComponentMenu("")]
    public class SplinePointDataFlow : MonoBehaviour, ISplinePointCustomData
    {
        public const float k_defaultSpeed = 2f;

        [Tooltip("Flow velocity (speed of flow in direction of spline). Can be negative to flip direction."), SerializeField]
        float _flowVelocity = k_defaultSpeed;

        public Vector2 GetData()
        {
            return new Vector2(_flowVelocity, 0f);
        }
    }
}
