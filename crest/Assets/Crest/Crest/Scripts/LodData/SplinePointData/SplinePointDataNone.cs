// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// No data. This should not be attached to any apline point, but is used as a symbol
    /// in the code when no data is required.
    /// </summary>
    [AddComponentMenu("")]
    public class SplinePointDataNone : MonoBehaviour, ISplinePointCustomData
    {
        public Vector2 GetData()
        {
            return Vector2.zero;
        }
    }
}
