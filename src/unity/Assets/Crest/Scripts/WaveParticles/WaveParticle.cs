using UnityEngine;
using UnityEngine.Assertions;

public struct WaveParticle
{
    public const float RADIUS = 0.2f;
    public const float PARTICLE_SPEED = 1f;
    public static readonly WaveParticle DEAD_PARTICLE = new WaveParticle(new Vector2(0, 0), new Vector2(0, 0), 0);

    // TODO: look into calculating this more intelligently
    public const int STRIDE = (4 * sizeof(float)) + sizeof(float);

    public readonly Vector2 origin;
    public readonly Vector2 velocity;
    public readonly float amplitude;

    public static WaveParticle createWaveParticle(Vector2 origin, Vector2 velocity, float amplitude)
    {
        return new WaveParticle(origin, velocity.normalized, amplitude);
    }

    public static WaveParticle createWaveParticleUnvalidated(Vector2 origin, Vector2 velocity, float amplitude)
    {
        Assert.AreApproximatelyEqual(velocity.magnitude, 1f);
        return new WaveParticle(origin, velocity, amplitude);
    }

    private WaveParticle(Vector2 origin, Vector2 velocity, float amplitude)
    {
        this.origin = origin;
        this.velocity = velocity;
        this.amplitude = amplitude;
    }

    public Vector2 getPosition(float time)
    {
        return origin + (time * PARTICLE_SPEED * velocity);
    }
}