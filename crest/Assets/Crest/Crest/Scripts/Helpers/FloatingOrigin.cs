// FloatingOrigin.cs
// Written by Peter Stirling
// 11 November 2010
// Uploaded to Unify Community Wiki on 11 November 2010
// Updated to Unity 5.x particle system by Tony Lovell 14 January, 2016
// fix to ensure ALL particles get moved by Tony Lovell 8 September, 2016
//
// URL: http://wiki.unity3d.com/index.php/Floating_Origin
//
// Adjusted to suit Crest by Huw Bowles:
// * Recommend a power-of-2 threshold - this avoids pops in the ocean geometry.
// * Move origin when x or z exceeds threshold (not the dist from origin exceeds threshold). This is required to support the previous point.
// * Notify ocean when origin moves
// * Misc style adjustments to align with Crest
// * Optional lists of components that can be provided to avoid evil FindObjectsOfType() calls
//
// NOTE 1: This thread discusses usage of this script: https://github.com/huwb/crest-oceanrender/issues/150
// NOTE 2: Of particular note - any world space texturing should have a period that tiles perfectly across the teleport distance, otherwise visible
// pops will occur. Example - set teleport radius to 16384m, and set normal map scale to 16m which divides evenly into the teleport radius.

using UnityEngine;

namespace Crest
{
    public interface IFloatingOrigin
    {
        /// <summary>
        /// Set a new origin. This is equivalent to subtracting the new origin position from any world position state.
        /// </summary>
        void SetOrigin(Vector3 newOrigin);
    }

    /// <summary>
    /// This script translates all objects in the world to keep the camera near the origin in order to prevent spatial jittering due to limited
    /// floating-point precision. The script detects when the camera is further than 'threshold' units from the origin in one or more axes, at which
    /// point it moves everything so that the camera is back at the origin. There is also an option to disable physics beyond a certain point. This
    /// script should normally be attached to the viewpoint, typically the main camera.
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Floating Origin")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "other-features.html" + Internal.Constants.HELP_URL_RP + "#floating-origin")]
    public class FloatingOrigin : MonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Tooltip("Use a power of 2 to avoid pops in ocean surface geometry."), SerializeField]
        float _threshold = 16384f;
        [Tooltip("Set to zero to disable."), SerializeField]
        float _physicsThreshold = 1000.0f;

        [SerializeField] float _defaultSleepThreshold = 0.14f;

        [Tooltip("Optionally provide a list of transforms to avoid doing a FindObjectsOfType() call."), SerializeField]
        Transform[] _overrideTransformList = null;
        [Tooltip("Optionally provide a list of particle systems to avoid doing a FindObjectsOfType() call."), SerializeField]
        ParticleSystem[] _overrideParticleSystemList = null;
        [Tooltip("Optionally provide a list of rigidbodies to avoid doing a FindObjectsOfType() call."), SerializeField]
        Rigidbody[] _overrideRigidbodyList = null;
        [Tooltip("Optionally provide a list of Gerstner components to avoid doing a FindObjectsOfType() call."), SerializeField]
        ShapeGerstnerBatched[] _overrideGerstnerList = null;

        ParticleSystem.Particle[] _particleBuffer = null;

        void LateUpdate()
        {
            var newOrigin = Vector3.zero;
            if (Mathf.Abs(transform.position.x) > _threshold) newOrigin.x += transform.position.x;
            if (Mathf.Abs(transform.position.z) > _threshold) newOrigin.z += transform.position.z;

            if (newOrigin != Vector3.zero)
            {
                MoveOrigin(newOrigin);
            }
        }

        void MoveOrigin(Vector3 newOrigin)
        {
            MoveOriginTransforms(newOrigin);
            MoveOriginParticles(newOrigin);
            MoveOriginOcean(newOrigin);

            MoveOriginDisablePhysics();
        }

        /// <summary>
        /// Move transforms to recenter around new origin
        /// </summary>
        void MoveOriginTransforms(Vector3 newOrigin)
        {
            var transforms = (_overrideTransformList != null && _overrideTransformList.Length > 0) ? _overrideTransformList : FindObjectsOfType<Transform>();
            foreach (var t in transforms)
            {
                if (t.parent == null)
                {
                    t.position -= newOrigin;
                }
            }
        }

        /// <summary>
        /// Move all particles that are simulated in world space
        /// </summary>
        void MoveOriginParticles(Vector3 newOrigin)
        {
            var pss = (_overrideParticleSystemList != null && _overrideParticleSystemList.Length > 0) ? _overrideParticleSystemList : FindObjectsOfType<ParticleSystem>();
            foreach (var sys in pss)
            {
                if (sys.main.simulationSpace != ParticleSystemSimulationSpace.World) continue;

                var particlesNeeded = sys.main.maxParticles;
                if (particlesNeeded <= 0) continue;

                var wasPaused = sys.isPaused;
                var wasPlaying = sys.isPlaying;

                if (!wasPaused)
                {
                    sys.Pause();
                }

                // Ensure a sufficiently large array in which to store the particles
                if (_particleBuffer == null || _particleBuffer.Length < particlesNeeded)
                {
                    _particleBuffer = new ParticleSystem.Particle[particlesNeeded];
                }

                // Update the particles
                var num = sys.GetParticles(_particleBuffer);
                for (var i = 0; i < num; i++)
                {
                    _particleBuffer[i].position -= newOrigin;
                }
                sys.SetParticles(_particleBuffer, num);

                if (wasPlaying)
                {
                    sys.Play();
                }
            }
        }

        /// <summary>
        /// Notify ocean of origin shift
        /// </summary>
        void MoveOriginOcean(Vector3 newOrigin)
        {
            if (OceanRenderer.Instance)
            {
                OceanRenderer.Instance._lodTransform.SetOrigin(newOrigin);

                var fos = OceanRenderer.Instance.GetComponentsInChildren<IFloatingOrigin>();
                foreach (var fo in fos)
                {
                    fo.SetOrigin(newOrigin);
                }

                // Gerstner components
                var gerstners = _overrideGerstnerList != null && _overrideGerstnerList.Length > 0 ? _overrideGerstnerList : FindObjectsOfType<ShapeGerstnerBatched>();
                foreach (var gerstner in gerstners)
                {
                    gerstner.SetOrigin(newOrigin);
                }
            }
        }

        /// <summary>
        /// Disable physics outside radius
        /// </summary>
        void MoveOriginDisablePhysics()
        {
            if (_physicsThreshold > 0f)
            {
                var physicsThreshold2 = _physicsThreshold * _physicsThreshold;
                var rbs = (_overrideRigidbodyList != null && _overrideRigidbodyList.Length > 0) ? _overrideRigidbodyList : FindObjectsOfType<Rigidbody>();
                foreach (var rb in rbs)
                {
                    if (rb.gameObject.transform.position.sqrMagnitude > physicsThreshold2)
                    {
                        rb.sleepThreshold = float.MaxValue;
                    }
                    else
                    {
                        rb.sleepThreshold = _defaultSleepThreshold;
                    }
                }
            }
        }
    }
}
