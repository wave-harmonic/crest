// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEngine;

namespace Crest
{
    public class SplinePointDataFoam : MonoBehaviour, ISplinePointCustomData
    {
        public float _foamAmount = 1f;

        public Vector2 GetData()
        {
            return new Vector2(_foamAmount, 0f);
        }
    }
}
