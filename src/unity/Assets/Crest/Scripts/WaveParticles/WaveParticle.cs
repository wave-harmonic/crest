using UnityEngine;
using UnityEngine.Assertions;

public struct WaveParticle
{
    public const float RADIUS = 0.2f;
    // TODO: fix faster speeds not being properly handled :(
    public const float PARTICLE_SPEED = 1f;
    public static readonly WaveParticle DEAD_PARTICLE = new WaveParticle(new Vector2(0, 0), new Vector2(0, 0), 0, 0, 0);

	// TODO: look into calculating this more intelligently
    public const int STRIDE = (4 * sizeof(float)) + (2 * sizeof(float)) + sizeof(uint);
    // TODO: This should be a property of wave particle speed and longest time between subdivisions!
    public const int FRAME_CYCLE_LENGTH = 10000;

    public readonly Vector2 origin;
    public readonly Vector2 velocity;
    public readonly float amplitude;
    public readonly float dispersionAngle;
    public readonly int startingFrame;

    public static WaveParticle createWaveParticle(Vector2 origin, Vector2 velocity, float amplitude, float dispersionAngle, int startingFrame)
    {
        return new WaveParticle(origin, velocity.normalized, amplitude, dispersionAngle, startingFrame);
    }

    public static WaveParticle createWaveParticleUnvalidated(Vector2 origin, Vector2 velocity, float amplitude, float dispersionAngle, int startingFrame)
    {
        Assert.AreApproximatelyEqual(velocity.magnitude, 1f);
        return new WaveParticle(origin, velocity, amplitude, dispersionAngle, startingFrame);
    }

    private WaveParticle(Vector2 origin, Vector2 velocity, float amplitude, float dispersionAngle, int startingFrame)
    {
        startingFrame = startingFrame % FRAME_CYCLE_LENGTH;
        this.origin = origin;
        this.velocity = velocity;
        this.startingFrame = startingFrame;
        this.amplitude = amplitude;
        this.dispersionAngle = dispersionAngle;
    }

    public Vector2 getPosition(int currentFrame)
    {
        currentFrame = currentFrame % FRAME_CYCLE_LENGTH;
        float t = (Time.fixedDeltaTime * (float)((currentFrame + (FRAME_CYCLE_LENGTH - startingFrame)) % FRAME_CYCLE_LENGTH));
        return origin + (t * PARTICLE_SPEED * velocity);
    }

    public bool isDead()
    {
        return velocity.magnitude == 0f;
    }
}