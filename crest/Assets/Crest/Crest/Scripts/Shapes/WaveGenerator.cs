// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Unity.Collections.LowLevel.Unsafe;

namespace Crest
{
    public abstract class WaveGenerator
    {
        protected int _resolution = 32;
        protected OceanWaveSpectrum _spectrum;

        static Dictionary<int, WaveGenerator> _generators = new Dictionary<int, WaveGenerator>();

        public abstract RenderTexture Generate(float time, float windSpeedMPS, bool spectrumChanged, int componentsPerOctave, CommandBuffer buf, out int firstSlice, out int lastSlice);

        public override int GetHashCode()
        {
            return GetHash(_resolution, _spectrum);
        }

        static int GetHash(int resolution, OceanWaveSpectrum spectrum)
        {
            int hash = Hashy.CreateHash();
            Hashy.AddInt(resolution, ref hash);
            Hashy.AddObject(spectrum, ref hash);
            return hash;
        }

        public static WaveGenerator GetGenerator(int resolution, OceanWaveSpectrum spectrum)
        {
            var hash = GetHash(resolution, spectrum);

            //Debug.Log(hash + " req from list of " + _generators.Count + ", spec hash " + spectrum.GetHashCode());
            if (!_generators.TryGetValue(hash, out var generator))
            {
                Debug.Log($"New wave generator for res {resolution} and spectrum {spectrum.ToString()}");
                generator = new WaveGeneratorGerstner(resolution, spectrum);
                _generators.Add(hash, generator);
            }

            return generator;
        }
    }

    public class WaveGeneratorGerstner : WaveGenerator
    {
        int _randomSeed = 0; // TODO?

        RenderTexture _waveBuffers;

        ComputeShader _shaderGerstner;
        int _krnlGerstner = -1;

        float _generatedTime = -1f;

        // First cascade of wave buffer that has waves and will be rendered
        int _firstCascade = -1;
        // Last cascade of wave buffer that has waves and will be rendered
        int _lastCascade = -1;

        // Used to populate data on first frame
        bool _firstUpdate = true;

        float _windSpeedWhenGenerated = -1f;

        struct GerstnerCascadeParams
        {
            public int _startIndex;
            public float _cumulativeVariance;
        }
        ComputeBuffer _bufCascadeParams;
        GerstnerCascadeParams[] _cascadeParams = new GerstnerCascadeParams[CASCADE_COUNT + 1];

        // Caution - order here impact performance. Rearranging these to match order
        // they're read in the compute shader made it 50% slower..
        struct GerstnerWaveComponent4
        {
            public Vector4 _twoPiOverWavelength;
            public Vector4 _amp;
            public Vector4 _waveDirX;
            public Vector4 _waveDirZ;
            public Vector4 _omega;
            public Vector4 _phase;
            public Vector4 _chopAmp;
            // Waves are generated in pairs, these values are for the second in the pair
            public Vector4 _amp2;
            public Vector4 _chopAmp2;
            public Vector4 _phase2;
        }
        ComputeBuffer _bufWaveData;
        GerstnerWaveComponent4[] _waveData = new GerstnerWaveComponent4[MAX_WAVE_COMPONENTS / 4];

        // Data for all components
        float[] _wavelengths;
        float[] _amplitudes;
        float[] _amplitudes2;
        float[] _powers;
        float[] _angleDegs;
        float[] _phases;
        float[] _phases2;

        const int CASCADE_COUNT = 16;
        const int MAX_WAVE_COMPONENTS = 1024;

        static readonly int sp_WaveBuffer = Shader.PropertyToID("_WaveBuffer");
        readonly int sp_FirstCascadeIndex = Shader.PropertyToID("_FirstCascadeIndex");
        readonly int sp_TextureRes = Shader.PropertyToID("_TextureRes");
        readonly int sp_CascadeParams = Shader.PropertyToID("_GerstnerCascadeParams");
        readonly int sp_GerstnerWaveData = Shader.PropertyToID("_GerstnerWaveData");
        readonly float _twoPi = 2f * Mathf.PI;
        readonly float _recipTwoPi = 1f / (2f * Mathf.PI);

        public WaveGeneratorGerstner(int resolution, OceanWaveSpectrum spectrum)
        {
            _resolution = resolution;
            _spectrum = spectrum;

            _shaderGerstner = ComputeShaderHelpers.LoadShader("Gerstner");
            _krnlGerstner = _shaderGerstner.FindKernel("Gerstner");

            InitData();
        }

        void InitData()
        {
            _waveBuffers = new RenderTexture(_resolution, _resolution, 0, GraphicsFormat.R16G16B16A16_SFloat);
            _waveBuffers.wrapMode = TextureWrapMode.Clamp;
            _waveBuffers.antiAliasing = 1;
            _waveBuffers.filterMode = FilterMode.Bilinear;
            _waveBuffers.anisoLevel = 0;
            _waveBuffers.useMipMap = false;
            _waveBuffers.name = "GerstnerCascades";
            _waveBuffers.dimension = TextureDimension.Tex2DArray;
            _waveBuffers.volumeDepth = CASCADE_COUNT;
            _waveBuffers.enableRandomWrite = true;
            _waveBuffers.Create();

            _bufCascadeParams = new ComputeBuffer(CASCADE_COUNT + 1, UnsafeUtility.SizeOf<GerstnerCascadeParams>());
            _bufWaveData = new ComputeBuffer(MAX_WAVE_COMPONENTS / 4, UnsafeUtility.SizeOf<GerstnerWaveComponent4>());
        }

        public override RenderTexture Generate(float time, float windSpeedMPS, bool spectrumChanged, int componentsPerOctave, CommandBuffer buf, out int firstSlice, out int lastSlice)
        {
            if (windSpeedMPS != _windSpeedWhenGenerated)
            {
                spectrumChanged = true;
                _windSpeedWhenGenerated = windSpeedMPS;
            }

            if (_waveBuffers == null || _bufCascadeParams == null || _bufWaveData == null)
            {
                InitData();
            }

            if (_firstUpdate || spectrumChanged)
            {
                UpdateWaveData(windSpeedMPS, componentsPerOctave);
            }

            // If some cascades have waves in them, generate
            //if (_firstCascade != -1 && _lastCascade != -1)
            //{
            //    //UpdateGenerateWaves(buf);
            //}

            if (time != _generatedTime || _firstUpdate)
            {
                //Debug.Log(GetHashCode() + ": GEN " + time);
                buf.SetComputeFloatParam(_shaderGerstner, sp_TextureRes, _waveBuffers.width);
                buf.SetComputeFloatParam(_shaderGerstner, OceanRenderer.sp_crestTime, time);
                buf.SetComputeIntParam(_shaderGerstner, sp_FirstCascadeIndex, _firstCascade);
                buf.SetComputeBufferParam(_shaderGerstner, _krnlGerstner, sp_CascadeParams, _bufCascadeParams);
                buf.SetComputeBufferParam(_shaderGerstner, _krnlGerstner, sp_GerstnerWaveData, _bufWaveData);
                buf.SetComputeTextureParam(_shaderGerstner, _krnlGerstner, sp_WaveBuffer, _waveBuffers);

                buf.DispatchCompute(_shaderGerstner, _krnlGerstner, _waveBuffers.width / LodDataMgr.THREAD_GROUP_SIZE_X, _waveBuffers.height / LodDataMgr.THREAD_GROUP_SIZE_Y, _lastCascade - _firstCascade + 1);

                _generatedTime = time;
            }

            _firstUpdate = false;

            firstSlice = _firstCascade;
            lastSlice = _lastCascade;

            return _waveBuffers;
        }

        /// <summary>
        /// Resamples wave spectrum
        /// </summary>
        /// <param name="windSpeed">Wind speed in m/s</param>
        public void UpdateWaveData(float windSpeed, int componentsPerOctave)
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            _spectrum.GenerateWaveData(componentsPerOctave, ref _wavelengths, ref _angleDegs);

            UpdateAmplitudes(windSpeed, componentsPerOctave);

            // Won't run every time so put last in the random sequence
            if (_phases == null || _phases.Length != _wavelengths.Length || _phases2 == null || _phases2.Length != _wavelengths.Length)
            {
                InitPhases(componentsPerOctave);
            }

            Random.state = randomStateBkp;

            SliceUpWaves(windSpeed, componentsPerOctave);
        }

        void UpdateAmplitudes(float windSpeedMPS, int componentsPerOctave)
        {
            if (_amplitudes == null || _amplitudes.Length != _wavelengths.Length)
            {
                _amplitudes = new float[_wavelengths.Length];
            }
            if (_amplitudes2 == null || _amplitudes2.Length != _wavelengths.Length)
            {
                _amplitudes2 = new float[_wavelengths.Length];
            }
            if (_powers == null || _powers.Length != _wavelengths.Length)
            {
                _powers = new float[_wavelengths.Length];
            }

            for (int i = 0; i < _wavelengths.Length; i++)
            {
                // TODO _weight should move to generation material??
                var amp = /*_weight **/ _spectrum.GetAmplitude(_wavelengths[i], componentsPerOctave, windSpeedMPS, out _powers[i]);
                _amplitudes[i] = Random.value * amp;
                _amplitudes2[i] = Random.value * amp * 0.5f;
            }
        }

        void InitPhases(int componentsPerOctave)
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            var totalComps = componentsPerOctave * OceanWaveSpectrum.NUM_OCTAVES;
            _phases = new float[totalComps];
            _phases2 = new float[totalComps];
            for (var octave = 0; octave < OceanWaveSpectrum.NUM_OCTAVES; octave++)
            {
                for (var i = 0; i < componentsPerOctave; i++)
                {
                    var index = octave * componentsPerOctave + i;
                    var rnd = (i + Random.value) / componentsPerOctave;
                    _phases[index] = 2f * Mathf.PI * rnd;

                    var rnd2 = (i + Random.value) / componentsPerOctave;
                    _phases2[index] = 2f * Mathf.PI * rnd2;
                }
            }

            Random.state = randomStateBkp;
        }

        void SliceUpWaves(float windSpeed, int componentsPerOctave)
        {
            _firstCascade = _lastCascade = -1;

            var cascadeIdx = 0;
            var componentIdx = 0;
            var outputIdx = 0;
            _cascadeParams[0]._startIndex = 0;

            // Seek forward to first wavelength that is big enough to render into current cascades
            var minWl = MinWavelength(cascadeIdx);
            while (componentIdx < _wavelengths.Length && _wavelengths[componentIdx] < minWl)
            {
                componentIdx++;
            }
            //Debug.Log($"{cascadeIdx}: start {_cascadeParams[cascadeIdx]._startIndex} minWL {minWl}");

            for (; componentIdx < _wavelengths.Length; componentIdx++)
            {
                // Skip small amplitude waves
                while (componentIdx < _wavelengths.Length && _amplitudes[componentIdx] < 0.001f)
                {
                    componentIdx++;
                }
                if (componentIdx >= _wavelengths.Length) break;

                // Check if we need to move to the next cascade
                while (cascadeIdx < CASCADE_COUNT && _wavelengths[componentIdx] >= 2f * minWl)
                {
                    // Wrap up this cascade and begin next

                    // Fill remaining elements of current vector4 with 0s
                    int vi = outputIdx / 4;
                    int ei = outputIdx - vi * 4;

                    while (ei != 0)
                    {
                        _waveData[vi]._twoPiOverWavelength[ei] = 1f;
                        _waveData[vi]._amp[ei] = 0f;
                        _waveData[vi]._waveDirX[ei] = 0f;
                        _waveData[vi]._waveDirZ[ei] = 0f;
                        _waveData[vi]._omega[ei] = 0f;
                        _waveData[vi]._phase[ei] = 0f;
                        _waveData[vi]._phase2[ei] = 0f;
                        _waveData[vi]._chopAmp[ei] = 0f;
                        _waveData[vi]._amp2[ei] = 0f;
                        _waveData[vi]._chopAmp2[ei] = 0f;
                        ei = (ei + 1) % 4;
                        outputIdx++;
                    }

                    if (outputIdx > 0 && _firstCascade == -1) _firstCascade = cascadeIdx;

                    cascadeIdx++;
                    _cascadeParams[cascadeIdx]._startIndex = outputIdx / 4;
                    minWl *= 2f;

                    //Debug.Log($"{cascadeIdx}: start {_cascadeParams[cascadeIdx]._startIndex} minWL {minWl}");
                }
                if (cascadeIdx == CASCADE_COUNT) break;

                {
                    // Pack into vector elements
                    int vi = outputIdx / 4;
                    int ei = outputIdx - vi * 4;

                    _waveData[vi]._amp[ei] = _amplitudes[componentIdx];
                    _waveData[vi]._amp2[ei] = _amplitudes2[componentIdx];

                    float chopScale = _spectrum._chopScales[componentIdx / componentsPerOctave];
                    _waveData[vi]._chopAmp[ei] = -chopScale * _spectrum._chop * _amplitudes[componentIdx];
                    _waveData[vi]._chopAmp2[ei] = -chopScale * _spectrum._chop * _amplitudes2[componentIdx];

                    float angle = Mathf.Deg2Rad * _angleDegs[componentIdx];
                    float dx = Mathf.Cos(angle);
                    float dz = Mathf.Sin(angle);

                    float gravityScale = _spectrum._gravityScales[(componentIdx) / componentsPerOctave];
                    float gravity = OceanRenderer.Instance.Gravity * _spectrum._gravityScale;
                    float C = Mathf.Sqrt(_wavelengths[componentIdx] * gravity * gravityScale * _recipTwoPi);
                    float k = _twoPi / _wavelengths[componentIdx];

                    // Constrain wave vector (wavelength and wave direction) to ensure wave tiles across domain
                    {
                        float kx = k * dx;
                        float kz = k * dz;
                        var diameter = 0.5f * (1 << cascadeIdx);

                        // Number of times wave repeats across domain in x and z
                        float n = kx / (_twoPi / diameter);
                        float m = kz / (_twoPi / diameter);
                        // Ensure the wave repeats an integral number of times across domain
                        kx = _twoPi * Mathf.Round(n) / diameter;
                        kz = _twoPi * Mathf.Round(m) / diameter;

                        // Compute new wave vector and direction
                        k = Mathf.Sqrt(kx * kx + kz * kz);
                        dx = kx / k;
                        dz = kz / k;
                    }

                    _waveData[vi]._twoPiOverWavelength[ei] = k;
                    _waveData[vi]._waveDirX[ei] = dx;
                    _waveData[vi]._waveDirZ[ei] = dz;

                    // Repeat every 2pi to keep angle bounded - helps precision on 16bit platforms
                    _waveData[vi]._omega[ei] = k * C;
                    _waveData[vi]._phase[ei] = Mathf.Repeat(_phases[componentIdx], Mathf.PI * 2f);
                    _waveData[vi]._phase2[ei] = Mathf.Repeat(_phases2[componentIdx], Mathf.PI * 2f);

                    outputIdx++;
                }
            }

            _lastCascade = cascadeIdx;

            {
                // Fill remaining elements of current vector4 with 0s
                int vi = outputIdx / 4;
                int ei = outputIdx - vi * 4;

                while (ei != 0)
                {
                    _waveData[vi]._twoPiOverWavelength[ei] = 1f;
                    _waveData[vi]._amp[ei] = 0f;
                    _waveData[vi]._waveDirX[ei] = 0f;
                    _waveData[vi]._waveDirZ[ei] = 0f;
                    _waveData[vi]._omega[ei] = 0f;
                    _waveData[vi]._phase[ei] = 0f;
                    _waveData[vi]._phase2[ei] = 0f;
                    _waveData[vi]._chopAmp[ei] = 0f;
                    _waveData[vi]._amp2[ei] = 0f;
                    _waveData[vi]._chopAmp2[ei] = 0f;
                    ei = (ei + 1) % 4;
                    outputIdx++;
                }
            }

            while (cascadeIdx < CASCADE_COUNT)
            {
                cascadeIdx++;
                minWl *= 2f;
                _cascadeParams[cascadeIdx]._startIndex = outputIdx / 4;
                //Debug.Log($"{cascadeIdx}: start {_cascadeParams[cascadeIdx]._startIndex} minWL {minWl}");
            }

            _lastCascade = CASCADE_COUNT - 1;

            // Compute a measure of variance, cumulative from low cascades to high
            for (int i = 0; i < CASCADE_COUNT; i++)
            {
                // Accumulate from lower cascades
                _cascadeParams[i]._cumulativeVariance = i > 0 ? _cascadeParams[i - 1]._cumulativeVariance : 0f;

                var wl = MinWavelength(i) * 1.5f;
                var octaveIndex = OceanWaveSpectrum.GetOctaveIndex(wl);
                octaveIndex = Mathf.Min(octaveIndex, _spectrum._chopScales.Length - 1);

                // Heuristic - horiz disp is roughly amp*chop, divide by wavelength to normalize
                var amp = _spectrum.GetAmplitude(wl, 1f, windSpeed, out _);
                var chop = _spectrum._chopScales[octaveIndex];
                float amp_over_wl = chop * amp / wl;
                _cascadeParams[i]._cumulativeVariance += amp_over_wl;
            }
            _cascadeParams[CASCADE_COUNT]._cumulativeVariance = _cascadeParams[CASCADE_COUNT - 1]._cumulativeVariance;

            _bufCascadeParams.SetData(_cascadeParams);
            _bufWaveData.SetData(_waveData);
        }

        /// <summary>
        /// Min wavelength for a cascade in the wave buffer. Does not depend on viewpoint.
        /// </summary>
        public float MinWavelength(int cascadeIdx)
        {
            var diameter = 0.5f * (1 << cascadeIdx);
            var texelSize = diameter / _resolution;
            return texelSize * OceanRenderer.Instance.MinTexelsPerWave;
        }

        // TODO how should this be again
        void Dispose()
        {
            if (_bufCascadeParams != null && _bufCascadeParams.IsValid())
            {
                _bufCascadeParams.Dispose();
                _bufCascadeParams = null;
            }
            if (_bufWaveData != null && _bufWaveData.IsValid())
            {
                _bufWaveData.Dispose();
                _bufWaveData = null;
            }
        }
    }
}
