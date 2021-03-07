// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEngine;

namespace Crest
{
    public class SplinePointDataGerstner : MonoBehaviour, ISplinePointCustomData
    {
        public float _weight = 1f;

        public Vector2 GetData()
        {
            return new Vector2(_weight, 0f);
        }
    }
}
