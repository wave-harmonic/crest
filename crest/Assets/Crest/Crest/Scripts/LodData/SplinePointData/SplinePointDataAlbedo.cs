// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Spline;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Custom spline point data for albedo
    /// </summary>
    [AddComponentMenu("")]
    public class SplinePointDataAlbedo : MonoBehaviour, ISplinePointCustomData
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public const float k_defaultAlpha = 1f;

        [Tooltip("Alpha value for albedo input."), SerializeField]
        float _alpha = k_defaultAlpha;

        public Vector2 GetData()
        {
            return new Vector2(_alpha, 0f);
        }
    }
}
