using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System;

// TODO: insert many assertions into this class.
public class WaveParticlesSystem
{
    /// TODO: give an explanation of this
    private const int THREAD_GROUPS_X = 8;

    private const string FIXED_DELTA_TIME = "fixedDeltaTime";
    private const string TIME = "time";

    private const string CURRENT_HEAD = "currentHead";
    private int _currentHead = 0;

    // The number of particles we wish to store
    private const string PARTICLE_CONTAINER_SIZE = "particleContainerSize";
    private int _particleContainerSize;

    private const string WAVE_PARTICLE_RADIUS = "waveParticleRadius";
    private const string PARTICLE_SPEED = "particleSpeed";
    private const string HORI_RES = "horiRes";
    private const string VERT_RES = "vertRes";
    private const string PLANE_WIDTH = "planeWidth";
    private const string PLANE_HEIGHT = "planeHeight";
    private const string NUM_PARTICLES_TO_ADD = "numParticlesToAdd";

    private const string SUBDIVISION_DATA = "subdivisionData";

    private const string SPLAT_TEXTURE = "splatTexture";
    private RenderTexture _splatTexture;

    private const string WAVE_PARTICLE_BUFFER = "waveParticleBuffer";
    private const string WAVE_PARTICLE_BUFFER_RW = "waveParticleBufferRW";
    private ComputeBuffer _waveParticlesBuffer;

    private const string SPLAT_PARTICLES = "SplatParticles";
    private const string SPLAT_PARTICLES_MODULUS = "SplatParticlesModulus";

    private ComputeShader _gpuSplatParticles;

    private int kernel_SplatParticlesModulus;

    public void Initialise(WaveParticle[] particles, float waveParticleKillThreshold)
    {
        _particleContainerSize = particles.Length;
        _splatTexture = null;

        // TODO: write Compute shader to clear this buffer
        _waveParticlesBuffer = new ComputeBuffer((int)_particleContainerSize, WaveParticle.STRIDE);

        _gpuSplatParticles = Resources.Load<ComputeShader>(SPLAT_PARTICLES);

        kernel_SplatParticlesModulus = _gpuSplatParticles.FindKernel(SPLAT_PARTICLES_MODULUS);

        _waveParticlesBuffer = new ComputeBuffer(_particleContainerSize, WaveParticle.STRIDE);
        _waveParticlesBuffer.SetData(particles);
    }

    public void splatParticlesModulus(float time, ref ExtendedHeightField pointMap)
    {

        ///
        /// Initialise _splatTexture if it hasn't been yet.
        ///
        if (_splatTexture == null)
        {
            _splatTexture = pointMap.textureHeightMap;
        }

        if (!_splatTexture.IsCreated())
        {
            _splatTexture.Create();
        }
        ///
        /// Initialise the SplatParticles GPU Compute Kernel to splat the particles to a texture
        ///
        _gpuSplatParticles.SetTexture(kernel_SplatParticlesModulus, SPLAT_TEXTURE, _splatTexture);
        _gpuSplatParticles.SetBuffer(kernel_SplatParticlesModulus, WAVE_PARTICLE_BUFFER, _waveParticlesBuffer);
        _gpuSplatParticles.SetFloat(FIXED_DELTA_TIME, Time.fixedDeltaTime);
        _gpuSplatParticles.SetFloat(TIME, time);
        _gpuSplatParticles.SetFloat(PARTICLE_SPEED, WaveParticle.PARTICLE_SPEED);
        _gpuSplatParticles.SetInt(HORI_RES, pointMap.HoriRes);
        _gpuSplatParticles.SetInt(VERT_RES, pointMap.VertRes);
        _gpuSplatParticles.SetFloat(PLANE_WIDTH, pointMap.Width);
        _gpuSplatParticles.SetFloat(PLANE_HEIGHT, pointMap.Height);

        ///
        /// Dispatch the kernel that splats wave particles to the _splatTexture
        ///
        _gpuSplatParticles.Dispatch(kernel_SplatParticlesModulus, ((int)_particleContainerSize) / THREAD_GROUPS_X, 1, 1);
    }

    public void OnDestroy()
    {
        _waveParticlesBuffer.Release();
    }
}
