// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// No data. This should not be attached to any spline point, but is used as a symbol
    /// in the code when no data is required.
    /// </summary>
    [AddComponentMenu("")]
    public class SplinePointDataNone : MonoBehaviour, ISplinePointCustomData
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public Vector2 GetData()
        {
            return Vector2.zero;
        }
    }
}
