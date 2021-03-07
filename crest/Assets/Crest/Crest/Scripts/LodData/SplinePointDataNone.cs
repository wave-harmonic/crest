// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEngine;

namespace Crest
{
    public class SplinePointDataNone : MonoBehaviour, ISplinePointCustomData
    {
        public Vector2 GetData()
        {
            return Vector2.zero;
        }
    }
}
