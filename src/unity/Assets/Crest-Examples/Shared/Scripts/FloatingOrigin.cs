// FloatingOrigin.cs
// Written by Peter Stirling
// 11 November 2010
// Uploaded to Unify Community Wiki on 11 November 2010
// Updated to Unity 5.x particle system by Tony Lovell 14 January, 2016
// fix to ensure ALL particles get moved by Tony Lovell 8 September, 2016
// URL: http://wiki.unity3d.com/index.php/Floating_Origin
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FloatingOrigin : MonoBehaviour
{
    public float threshold = 100.0f;
    public float physicsThreshold = 1000.0f; // Set to zero to disable

#if OLD_PHYSICS
    public float defaultSleepVelocity = 0.14f;
    public float defaultAngularVelocity = 0.14f;
#else
    public float defaultSleepThreshold = 0.14f;
#endif

    ParticleSystem.Particle[] parts = null;

    void LateUpdate()
    {
        Vector3 cameraPosition = gameObject.transform.position;
        cameraPosition.y = 0f;
        if (cameraPosition.magnitude > threshold)
        {
            Object[] objects = FindObjectsOfType(typeof(Transform));
            foreach (Object o in objects)
            {
                Transform t = (Transform)o;
                if (t.parent == null)
                {
                    t.position -= cameraPosition;
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
                    emitterParticles[i].position -= cameraPosition;
                }
                pe.particles = emitterParticles;
            }
#endif

            // new particles... very similar to old version above
            objects = FindObjectsOfType(typeof(ParticleSystem));
            foreach (UnityEngine.Object o in objects)
            {
                ParticleSystem sys = (ParticleSystem)o;

                if (sys.simulationSpace != ParticleSystemSimulationSpace.World)
                    continue;

                int particlesNeeded = sys.maxParticles;

                if (particlesNeeded <= 0)
                    continue;

                bool wasPaused = sys.isPaused;
                bool wasPlaying = sys.isPlaying;

                if (!wasPaused)
                    sys.Pause();

                // ensure a sufficiently large array in which to store the particles
                if (parts == null || parts.Length < particlesNeeded)
                {
                    parts = new ParticleSystem.Particle[particlesNeeded];
                }

                // now get the particles
                int num = sys.GetParticles(parts);

                for (int i = 0; i < num; i++)
                {
                    parts[i].position -= cameraPosition;
                }

                sys.SetParticles(parts, num);

                if (wasPlaying)
                    sys.Play();
            }

            if (physicsThreshold > 0f)
            {
                float physicsThreshold2 = physicsThreshold * physicsThreshold; // simplify check on threshold
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
                        r.sleepAngularVelocity = defaultSleepVelocity;
                        r.sleepVelocity = defaultAngularVelocity;
#else
                        r.sleepThreshold = defaultSleepThreshold;
#endif
                    }
                }
            }

            if(Crest.OceanRenderer.Instance)
            {
                Crest.OceanRenderer.Instance.SetOrigin(cameraPosition);
            }
        }
    }
}
