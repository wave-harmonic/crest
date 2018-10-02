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
    private const string CURRENT_FRAME = "currentFrame";

    private const string CURRENT_HEAD = "currentHead";
    private int _currentHead = 0;

    // The number of particles we wish to store
    private const string PARTICLE_CONTAINER_SIZE = "particleContainerSize";
    private int _particleContainerSize;

    private const string WAVE_PARTICLE_RADIUS = "waveParticleRadius";
    private const string NUM_SUBDIVISIONS = "numSubdivisions";
    private const string KILL_THRESHOLD = "killThreshold";
    private const string PARTICLE_SPEED = "particleSpeed";
    private const string HORI_RES = "horiRes";
    private const string VERT_RES = "vertRes";
    private const string PLANE_WIDTH = "planeWidth";
    private const string PLANE_HEIGHT = "planeHeight";
    private const string FRAME_CYCLE_LENGTH = "frameCycleLength";
    private const string NUM_PARTICLES_TO_ADD = "numParticlesToAdd";

    private const string PARTICLE_INDICES_TO_SUBDIVIDE_BUFFER = "particleIndicesToSubdivideBuffer";
    private const string PARTICLES_TO_ADD_APPEND_BUFFER = "particlesToAddAppendBuffer";
    private const string PARTICLES_TO_ADD_CONSUME_BUFFER = "particlesToAddConsumeBuffer";
    private const string SUBDIVISION_DATA = "subdivisionData";
    private const string PENDING_PARTICLES_BUFFER = "pendingParticlesBuffer";

    private float _waveParticleKillThreshold;

    private const string SPLAT_TEXTURE = "splatTexture";
    private RenderTexture _splatTexture;

    private const string WAVE_PARTICLE_BUFFER = "waveParticleBuffer";
    private const string WAVE_PARTICLE_BUFFER_RW = "waveParticleBufferRW";
    private ComputeBuffer _waveParticlesBuffer;

    private const string COMMIT_PARTICLES = "CommitParticles";
    private const string SPLAT_PARTICLES = "SplatParticles";
    private const string SUBDIVIDE_PARTICLES = "SubdivideParticles";
    private const string ADD_SUBDIVIDED_PARTICLES = "AddSubdividedParticles";

    private ComputeShader _gpuCommitParticles;
    private ComputeShader _gpuSplatParticles;
    private ComputeShader _gpuSubdivideParticles;

    private int kernel_SplatParticles;
    private int kernel_SubdivideParticles;
    private int kernel_CommmitParticles;
    private int kernel_AddSubdividedParticles;

    /// <summary>
    ///
    /// The way we handle subdivision events, is through the use of two arrays.
    ///
    /// One which is indexed by frame-numbers (_subdivisionEvents), and one
    /// which is a one-to-one mapping of the array of particles on the GPU
    /// (_particleEvents).
    ///
    /// When we add a particle to the system, we pre-compute the frame
    /// within-which it will subdivide. We then add a pointer its index in
    /// both the GPU buffer storing all the wave particles (_waveParticlesBuffer)
    /// and its entry in the _particleEvents array. (The index is the same for
    /// both as these datastructues are bijective.)
    ///
    /// If there is already a pointer to a WaveParticle in _subdivisionEvents at
    /// the given frame, EventElement entries in _particleEvents are used to
    /// create a linked-list of which particels are due to subdivide on any given
    /// frame.
    ///
    /// If there already is an entry in _particleEvents at the index we want to use
    /// that means that there is a living particle currently in that position on the
    /// GPU. TODO: explain how this is cleverly handled.
    /// </summary>
    private struct EventElement
    {
        public int frontIndex;
        public int backIndex;
        public static EventElement getDeadEvent()
        {
            EventElement e;
            e.backIndex = int.MinValue;
            e.frontIndex = _NullParticleIndex;
            return e;
        }
        public bool isDead()
        {
            return backIndex == int.MinValue;
        }
    }
    private EventElement[] _particleEvents;
    private const int _NullParticleIndex = -1;
    private int[] _subdivisionEvents = new int[WaveParticle.FRAME_CYCLE_LENGTH];

    private const string PENDING_PARTICLES_HEAD = "pendingParticlesHead";
    private int _pendingParticlesHead = 0;
    private WaveParticle[] _pendingParticles;

    public void Initialise(int numParticles, float waveParticleKillThreshold)
    {
        _particleContainerSize = numParticles;
        _waveParticleKillThreshold = waveParticleKillThreshold;
        _splatTexture = null;

        // TODO: write Compute shader to clear this buffer
        _waveParticlesBuffer = new ComputeBuffer((int)_particleContainerSize, WaveParticle.STRIDE);

        _gpuCommitParticles = Resources.Load<ComputeShader>(COMMIT_PARTICLES);
        _gpuSplatParticles = Resources.Load<ComputeShader>(SPLAT_PARTICLES);
        _gpuSubdivideParticles = Resources.Load<ComputeShader>(SUBDIVIDE_PARTICLES);

        kernel_CommmitParticles = _gpuCommitParticles.FindKernel(COMMIT_PARTICLES);
        kernel_SplatParticles = _gpuSplatParticles.FindKernel(SPLAT_PARTICLES);
        kernel_SubdivideParticles = _gpuSubdivideParticles.FindKernel(SUBDIVIDE_PARTICLES);
        kernel_AddSubdividedParticles = _gpuSubdivideParticles.FindKernel(ADD_SUBDIVIDED_PARTICLES);

        _particleEvents = new EventElement[_particleContainerSize];
        for (int i = 0; i < _particleEvents.Length; i++)
        {
            _particleEvents[i] = EventElement.getDeadEvent();
        }
        for (int i = 0; i < _subdivisionEvents.Length; i++)
        {
            _subdivisionEvents[i] = _NullParticleIndex;
        }

        _pendingParticles = new WaveParticle[numParticles / 10];
    }

    public void setWaveParticleKillThreshold(float waveParticleKillThreshold)
    {
        _waveParticleKillThreshold = waveParticleKillThreshold;
    }

    public void addParticle(WaveParticle particle)

    {
        ///
        /// Calculate when WaveParticle is due to subdivide
        ///
        int subdivisionFrame;
        {
            // if the dispersion angle is 0, the subdivide particle at latest possilbe time
            if (particle.dispersionAngle == 0f)
            {
                subdivisionFrame = particle.startingFrame + WaveParticle.FRAME_CYCLE_LENGTH;
            }
            else
            {
                float timeToSubdivision = WaveParticle.RADIUS * 0.5f / (WaveParticle.PARTICLE_SPEED * particle.dispersionAngle);
                int framesToSubdivision = (Mathf.RoundToInt(timeToSubdivision / Time.fixedDeltaTime));
                if (framesToSubdivision > WaveParticle.FRAME_CYCLE_LENGTH)
                {
                    framesToSubdivision = WaveParticle.FRAME_CYCLE_LENGTH;
                }
                subdivisionFrame = (framesToSubdivision + particle.startingFrame) % WaveParticle.FRAME_CYCLE_LENGTH;
            }
        }

        ///
        /// Using the calculated subdivision frame, add the current particle as an event in that frame
        ///
        addSubdivisionEvent((_currentHead + _pendingParticlesHead) % _particleContainerSize, subdivisionFrame);

        ///
        /// Add the current particle to the to-be-committed buffer
        ///
        /// Commit the current buffer if it ends up full
        ///
        _pendingParticles[_pendingParticlesHead++] = particle;
        if (_pendingParticlesHead >= _pendingParticles.Length)
        {
            commitParticles();
        }
    }

    public void addSubdivisionEvent(int particleIndex, int subdivisionFrame)
    {
        {   // Make sure that another particle isn't "Alive" here
            EventElement existingParticle = _particleEvents[particleIndex];
            if (!existingParticle.isDead())
            {
                // Perform cleanup
                if (existingParticle.backIndex < 0)
                {
                    int frameIndex = -1 * existingParticle.backIndex;
                    _subdivisionEvents[frameIndex] = existingParticle.frontIndex;
                }
                else
                {
                    _particleEvents[existingParticle.backIndex].frontIndex = existingParticle.frontIndex;
                }
            }
        }

        EventElement newParticle;
        newParticle.backIndex = subdivisionFrame * -1;
        newParticle.frontIndex = _subdivisionEvents[subdivisionFrame];
        if (_subdivisionEvents[subdivisionFrame] != _NullParticleIndex)
        {
            _particleEvents[_subdivisionEvents[subdivisionFrame]].backIndex = particleIndex;
        }
        _particleEvents[particleIndex] = newParticle;
        _subdivisionEvents[subdivisionFrame] = particleIndex;
    }

    /// <summary>
    /// Commit the particles currently stored in _pendingParticles to the main GPU buffer.
    /// </summary>
    public void commitParticles()
    {
        if (_pendingParticlesHead == 0) { return; }
        ///
        /// Use the _pendingParticlesBuffer to fill up the main _waveParticleBuffer
        ///
        ComputeBuffer pendingParticlesBuffer = new ComputeBuffer(_pendingParticlesHead + 1, WaveParticle.STRIDE);
        pendingParticlesBuffer.SetData(_pendingParticles);

        _gpuCommitParticles.SetBuffer(kernel_CommmitParticles, PENDING_PARTICLES_BUFFER, pendingParticlesBuffer);
        _gpuCommitParticles.SetBuffer(kernel_CommmitParticles, WAVE_PARTICLE_BUFFER, _waveParticlesBuffer);
        _gpuCommitParticles.SetInt(CURRENT_HEAD, _currentHead);
        _gpuCommitParticles.SetInt(PARTICLE_CONTAINER_SIZE, _particleContainerSize);
        _gpuCommitParticles.SetInt(PENDING_PARTICLES_HEAD, _pendingParticlesHead);
        // TODO: Investigate the arithmetic below
        _gpuCommitParticles.Dispatch(kernel_CommmitParticles, (_pendingParticlesHead + 1 + THREAD_GROUPS_X) / THREAD_GROUPS_X, 1, 1);

        pendingParticlesBuffer.GetData(_pendingParticles);

        pendingParticlesBuffer.Release();

        // Set the new current head
        _currentHead = (_pendingParticlesHead + _currentHead) % _particleContainerSize;
        _pendingParticlesHead = 0;
    }

    /// <summary>
    /// Apply the subdivisions based on which particles need subdividing this frame.
    ///
    /// This happens in two Compute Shader steps:
    ///
    ///   - The first takes all particles to be subdived and calculates the details of which particles will be created
    ///
    ///   - The second takes all the newly created particles and adds them to the total pool of Wave Particles
    ///     It also creates a buffer of data that can be used to calculate future subdivision events that will occur
    ///     as a result of the new wave particles.
    ///
    /// </summary>
    /// <param name="currentFrame"></param>
    public void calculateSubdivisions(int currentFrame)
    {
        // Commit all pending particles that are stored in CPU memory to the GPU.
        commitParticles();

        ///
        /// Count the number of subdivisions that will applied this frame.
        ///
        int numSubdivisions = 0;
        {
            int particleIndex = _subdivisionEvents[currentFrame];
            while (particleIndex != _NullParticleIndex)
            {
                numSubdivisions++;
                particleIndex = _particleEvents[particleIndex].frontIndex;
            }
        }

        // If no subdivisions are required, return
        if (numSubdivisions == 0)
        {
            return;
        }

        ///
        /// Get the indices of all the particles that will be subdivided this frame.
        ///
        int[] particleIndicesToSubdivide = new int[numSubdivisions];
        {
            int particleIndex = _subdivisionEvents[currentFrame];
            for (int i = 0; i < particleIndicesToSubdivide.Length; i++)
            {
                particleIndicesToSubdivide[i] = particleIndex;
                particleIndex = _particleEvents[particleIndex].frontIndex;
                _particleEvents[particleIndicesToSubdivide[i]] = EventElement.getDeadEvent();
            }
            _subdivisionEvents[currentFrame] = _NullParticleIndex;
        }

        ///
        /// Create buffers for storing particles to be subdivided and the resulting subdivisions.
        ///
        ComputeBuffer particleIndicesToSubdivideBuffer = new ComputeBuffer(numSubdivisions, sizeof(int));
        particleIndicesToSubdivideBuffer.SetData(particleIndicesToSubdivide);
        // Create an append buffer for the newly created particles
        ComputeBuffer particlesToAddBuffer = new ComputeBuffer(numSubdivisions * 3, WaveParticle.STRIDE, ComputeBufferType.Append);
        particlesToAddBuffer.SetCounterValue(0);

        // Load particles to be subdivided into this buffer, and copy it out to get future subdivision details???
        _gpuSubdivideParticles.SetInt(NUM_SUBDIVISIONS, numSubdivisions);
        _gpuSubdivideParticles.SetFloat(KILL_THRESHOLD, _waveParticleKillThreshold);
        _gpuSubdivideParticles.SetBuffer(kernel_SubdivideParticles, PARTICLE_INDICES_TO_SUBDIVIDE_BUFFER, particleIndicesToSubdivideBuffer);
        _gpuSubdivideParticles.SetBuffer(kernel_SubdivideParticles, WAVE_PARTICLE_BUFFER, _waveParticlesBuffer);
        _gpuSubdivideParticles.SetBuffer(kernel_SubdivideParticles, PARTICLES_TO_ADD_APPEND_BUFFER, particlesToAddBuffer);
        _gpuSubdivideParticles.Dispatch(kernel_SubdivideParticles, (numSubdivisions + THREAD_GROUPS_X) / THREAD_GROUPS_X, 1, 1);

        int numParticlesToAdd = particlesToAddBuffer.count;
        ComputeBuffer subdivisionDataBuffer = new ComputeBuffer(numParticlesToAdd, sizeof(int) * 2);

        _gpuSubdivideParticles.SetInt(CURRENT_HEAD, _currentHead);
        _gpuSubdivideParticles.SetInt(PARTICLE_CONTAINER_SIZE, _particleContainerSize);
        _gpuSubdivideParticles.SetInt(FRAME_CYCLE_LENGTH, WaveParticle.FRAME_CYCLE_LENGTH);
        _gpuSubdivideParticles.SetInt(NUM_PARTICLES_TO_ADD, numParticlesToAdd);
        _gpuSubdivideParticles.SetFloat(WAVE_PARTICLE_RADIUS, WaveParticle.RADIUS);
        _gpuSubdivideParticles.SetFloat(FIXED_DELTA_TIME, Time.fixedDeltaTime);
        _gpuSubdivideParticles.SetFloat(PARTICLE_SPEED, WaveParticle.PARTICLE_SPEED);

        _gpuSubdivideParticles.SetBuffer(kernel_AddSubdividedParticles, PARTICLES_TO_ADD_CONSUME_BUFFER, particlesToAddBuffer);
        _gpuSubdivideParticles.SetBuffer(kernel_AddSubdividedParticles, SUBDIVISION_DATA, subdivisionDataBuffer);
        _gpuSubdivideParticles.SetBuffer(kernel_AddSubdividedParticles, WAVE_PARTICLE_BUFFER, _waveParticlesBuffer);

        _gpuSubdivideParticles.Dispatch(kernel_AddSubdividedParticles, (numParticlesToAdd + THREAD_GROUPS_X) / THREAD_GROUPS_X, 1, 1);

        _currentHead = (_currentHead + numParticlesToAdd) % _particleContainerSize;
        int[,] subdivisionData = new int[numParticlesToAdd, 2];
        subdivisionDataBuffer.GetData(subdivisionData);
        for (int i = 0; i < numParticlesToAdd; i++)
        {
            int particleIndex = subdivisionData[i, 0];
            int subdivisionFrame = subdivisionData[i, 1];
            addSubdivisionEvent(particleIndex, subdivisionFrame);
        }

        subdivisionDataBuffer.Release();
        particlesToAddBuffer.Release();
        particleIndicesToSubdivideBuffer.Release();
    }

    public void splatParticles(int currentFrame, ref ExtendedHeightField pointMap)
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
        _gpuSplatParticles.SetTexture(kernel_SplatParticles, SPLAT_TEXTURE, _splatTexture);
        _gpuSplatParticles.SetBuffer(kernel_SplatParticles, WAVE_PARTICLE_BUFFER, _waveParticlesBuffer);
        _gpuSplatParticles.SetFloat(FIXED_DELTA_TIME, Time.fixedDeltaTime);
        _gpuSplatParticles.SetInt(CURRENT_FRAME, currentFrame);
        _gpuSplatParticles.SetFloat(PARTICLE_SPEED, WaveParticle.PARTICLE_SPEED);
        _gpuSplatParticles.SetInt(HORI_RES, pointMap.HoriRes);
        _gpuSplatParticles.SetInt(VERT_RES, pointMap.VertRes);
        _gpuSplatParticles.SetFloat(PLANE_WIDTH, pointMap.Width);
        _gpuSplatParticles.SetFloat(PLANE_HEIGHT, pointMap.Height);

        ///
        /// Dispatch the kernel that splats wave particles to the _splatTexture
        ///
        _gpuSplatParticles.Dispatch(kernel_SplatParticles, ((int)_particleContainerSize) / THREAD_GROUPS_X, 1, 1);
    }

    public void OnDestroy()
    {
        _waveParticlesBuffer.Release();
    }
}
