// FloatingOrigin.cs
// Written by Peter Stirling
// 11 November 2010
// Uploaded to Unify Community Wiki on 11 November 2010
// Updated to Unity 5.x particle system by Tony Lovell 14 January, 2016
// fix to ensure ALL particles get moved by Tony Lovell 8 September, 2016
//
// Adjusted to suit Crest by Huw Bowles:
// * Recommend a power-of-2 threshold - this avoids pops in the ocean geometry.
// * Move origin when x or z exceeds threshold (not the dist from origin exceeds threshold). This is required to support the previous point.
// * Notify ocean when origin moves
// * Misc style
//
// URL: http://wiki.unity3d.com/index.php/Floating_Origin

using UnityEngine;

public interface IFloatingOrigin
{
    void SetOrigin(Vector3 newOrigin);
}

public class FloatingOrigin : MonoBehaviour
{
    [Tooltip("Use a power of 2 to avoid pops in ocean surface geometry."), SerializeField]
    float _threshold = 16384f;
    [Tooltip("Set to zero to disable."), SerializeField]
    float _physicsThreshold = 1000.0f;

#if OLD_PHYSICS
    [SerializeField] float _defaultSleepVelocity = 0.14f;
    [SerializeField] float _defaultAngularVelocity = 0.14f;
#else
    [SerializeField] float _defaultSleepThreshold = 0.14f;
#endif

    ParticleSystem.Particle[] _parts = null;

    void MoveOrigin(Vector3 translationToRemove)
    {
        Object[] objects = FindObjectsOfType(typeof(Transform));
        foreach (Object o in objects)
        {
            Transform t = (Transform)o;
            if (t.parent == null)
            {
                t.position -= translationToRemove;
            }
        }

#if SUPPORT_OLD_PARTICLE_SYSTEM
        // move active particles from old Unity particle system that are active in world space
        objects = FindObjectsOfType(typeof(ParticleEmitter));
        foreach (Object o in objects)
        {
            ParticleEmitter pe = (ParticleEmitter)o;
 
            // if the particle is not in world space, the logic above should have moved them already
	        if (!pe.useWorldSpace)
		        continue;
 
            Particle[] emitterParticles = pe.particles;
            for(int i = 0; i < emitterParticles.Length; ++i)
            {
                emitterParticles[i].position -= translationToRemove;
            }
            pe.particles = emitterParticles;
        }
#endif

        // new particles... very similar to old version above
        objects = FindObjectsOfType(typeof(ParticleSystem));
        foreach (UnityEngine.Object o in objects)
        {
            ParticleSystem sys = (ParticleSystem)o;

            if (sys.main.simulationSpace != ParticleSystemSimulationSpace.World)
                continue;

            int particlesNeeded = sys.main.maxParticles;

            if (particlesNeeded <= 0)
                continue;

            bool wasPaused = sys.isPaused;
            bool wasPlaying = sys.isPlaying;

            if (!wasPaused)
                sys.Pause();

            // ensure a sufficiently large array in which to store the particles
            if (_parts == null || _parts.Length < particlesNeeded)
            {
                _parts = new ParticleSystem.Particle[particlesNeeded];
            }

            // now get the particles
            int num = sys.GetParticles(_parts);

            for (int i = 0; i < num; i++)
            {
                _parts[i].position -= translationToRemove;
            }

            sys.SetParticles(_parts, num);

            if (wasPlaying)
                sys.Play();
        }

        if (_physicsThreshold > 0f)
        {
            float physicsThreshold2 = _physicsThreshold * _physicsThreshold; // simplify check on threshold
            objects = FindObjectsOfType(typeof(Rigidbody));
            foreach (UnityEngine.Object o in objects)
            {
                Rigidbody r = (Rigidbody)o;
                if (r.gameObject.transform.position.sqrMagnitude > physicsThreshold2)
                {
#if OLD_PHYSICS
                    r.sleepAngularVelocity = float.MaxValue;
                    r.sleepVelocity = float.MaxValue;
#else
                    r.sleepThreshold = float.MaxValue;
#endif
                }
                else
                {
#if OLD_PHYSICS
                    r.sleepAngularVelocity = _defaultSleepVelocity;
                    r.sleepVelocity = _defaultAngularVelocity;
#else
                    r.sleepThreshold = _defaultSleepThreshold;
#endif
                }
            }
        }

        if (Crest.OceanRenderer.Instance)
        {
            Crest.OceanRenderer.Instance.SetOrigin(translationToRemove);
        }
    }

    void LateUpdate()
    {
        var toRemove = Vector3.zero;
        if (Mathf.Abs(transform.position.x) > _threshold) toRemove.x += transform.position.x;
        if (Mathf.Abs(transform.position.z) > _threshold) toRemove.z += transform.position.z;

        if (toRemove != Vector3.zero)
        {
            MoveOrigin(toRemove);
        }
    }
}
