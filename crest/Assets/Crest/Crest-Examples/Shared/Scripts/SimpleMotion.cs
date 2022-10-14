// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest.Examples
{
    /// <summary>
    /// Moves this transform.
    /// </summary>
    public class SimpleMotion : CustomMonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public bool _resetOnDisable;

        [Header("Translation")]
        public Vector3 _velocity;

        [Header("Rotation")]
        public Vector3 _angularVelocity;

        Vector3 _oldPosition;
        Quaternion _oldRotation;

        void OnEnable()
        {
            _oldPosition = transform.position;
            _oldRotation = transform.rotation;
        }

        void OnDisable()
        {
            if (_resetOnDisable)
            {
                transform.position = _oldPosition;
                transform.rotation = _oldRotation;
            }
        }

        void Update()
        {
            // Translation
            {
                transform.position += _velocity * Time.deltaTime;
            }

            // Rotation
            {
                transform.rotation *= Quaternion.Euler(_angularVelocity * Time.deltaTime);
            }
        }
    }
}
