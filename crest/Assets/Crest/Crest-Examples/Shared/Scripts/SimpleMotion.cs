// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest.Examples
{
    /// <summary>
    /// Moves this transform.
    /// </summary>
    public class SimpleMotion : MonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Header("Translation")]
        public Vector3 _velocity;

        void Update()
        {
            // Translation
            {
                transform.position += _velocity * Time.deltaTime;
            }
        }
    }
}
