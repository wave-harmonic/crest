// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;

namespace Crest
{
    /// <summary>
    /// Gerstner ocean waves.
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Shape Gerstner")]
    public partial class ShapeGerstner : ShapeWaves
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Header("Wave Conditions")]

        [Tooltip("Each Gerstner wave is actually a pair of waves travelling in opposite directions (similar to FFT). This weight is applied to the wave travelling in against-wind direction. Set to 0 to obtain simple single waves."), Range(0f, 1f)]
        public float _reverseWaveWeight = 0.5f;

        [Header("Generation Settings")]
        [Delayed, Tooltip("How many wave components to generate in each octave.")]
        public int _componentsPerOctave = 8;

        [Tooltip("Change to get a different set of waves.")]
        public int _randomSeed = 0;


        // Debug
        [Space(10)]

        [SerializeField]
        DebugFields _debug = new DebugFields();
        protected override DebugFields DebugSettings => _debug;


        protected override int MinimumResolution => 8;
        protected override int MaximumResolution => 64;

        float _windSpeedWhenGenerated = -1f;

        const int MAX_WAVE_COMPONENTS = 1024;

        // Data for all components
        float[] _wavelengths;
        float[] _amplitudes;
        float[] _amplitudes2;
        float[] _powers;
        float[] _angleDegs;
        float[] _phases;
        float[] _phases2;

        [HideInInspector]
        public RenderTexture _waveBuffers;

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

        ComputeShader _shaderGerstner;
        int _krnlGerstner = -1;

        readonly int sp_FirstCascadeIndex = Shader.PropertyToID("_FirstCascadeIndex");
        readonly int sp_TextureRes = Shader.PropertyToID("_TextureRes");
        readonly int sp_CascadeParams = Shader.PropertyToID("_GerstnerCascadeParams");
        readonly int sp_GerstnerWaveData = Shader.PropertyToID("_GerstnerWaveData");

        readonly float _twoPi = 2f * Mathf.PI;
        readonly float _recipTwoPi = 1f / (2f * Mathf.PI);

        internal static readonly CrestSortedList<int, ShapeGerstner> Instances = new CrestSortedList<int, ShapeGerstner>(Helpers.SiblingIndexComparison);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            Instances.Clear();
        }

        void InitData()
        {
            if (_waveBuffers == null)
            {
                _waveBuffers = new RenderTexture(_resolution, _resolution, 0, GraphicsFormat.R16G16B16A16_SFloat);
            }
            else
            {
                _waveBuffers.Release();
            }

            {
                _waveBuffers.width = _waveBuffers.height = _resolution;
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
            }

            _bufCascadeParams?.Release();
            _bufWaveData?.Release();

            _bufCascadeParams = new ComputeBuffer(CASCADE_COUNT + 1, UnsafeUtility.SizeOf<GerstnerCascadeParams>());
            _bufWaveData = new ComputeBuffer(MAX_WAVE_COMPONENTS / 4, UnsafeUtility.SizeOf<GerstnerWaveComponent4>());

            _shaderGerstner = ComputeShaderHelpers.LoadShader("Gerstner");
            _krnlGerstner = _shaderGerstner.FindKernel("Gerstner");
        }

        public override float MinWavelength(int cascadeIdx)
        {
            var diameter = 0.5f * (1 << cascadeIdx);
            var texelSize = diameter / _resolution;
            // Nyquist rate x 2, for higher quality
            return texelSize * 4f;
        }

        public override void CrestUpdate(CommandBuffer buf)
        {
            if (_waveBuffers == null || _resolution != _waveBuffers.width || _bufCascadeParams == null || _bufWaveData == null)
            {
                InitData();
            }

            var windSpeed = WindSpeed;
            if (_firstUpdate || UpdateDataEachFrame || windSpeed != _windSpeedWhenGenerated)
            {
                UpdateWaveData(windSpeed);
                _windSpeedWhenGenerated = windSpeed;
            }

            base.CrestUpdate(buf);

            _matGenerateWaves.SetVector(sp_AxisX, PrimaryWaveDirection);
            // Seems like shader errors cause this to unbind if I don't set it every frame. Could be an editor only issue.
            _matGenerateWaves.SetTexture(sp_WaveBuffer, _waveBuffers);

            // If some cascades have waves in them, generate
            if (_firstCascade != -1 && _lastCascade != -1)
            {
                UpdateGenerateWaves(buf);
            }
        }

        void SliceUpWaves(float windSpeed)
        {
            // Do not filter cascades if blending as the blend operation might be skipped.
            _firstCascade = BlendMode == ShapeBlendMode.Blend ? 0 : _lastCascade = -1;

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
            //Debug.Log($"Crest: {cascadeIdx}: start {_cascadeParams[cascadeIdx]._startIndex} minWL {minWl}");

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

                    //Debug.Log($"Crest: {cascadeIdx}: start {_cascadeParams[cascadeIdx]._startIndex} minWL {minWl}");
                }
                if (cascadeIdx == CASCADE_COUNT) break;

                {
                    // Pack into vector elements
                    int vi = outputIdx / 4;
                    int ei = outputIdx - vi * 4;

                    _waveData[vi]._amp[ei] = _amplitudes[componentIdx];
                    _waveData[vi]._amp2[ei] = _amplitudes2[componentIdx];

                    float chopScale = _activeSpectrum._chopScales[componentIdx / _componentsPerOctave];
                    _waveData[vi]._chopAmp[ei] = -chopScale * _activeSpectrum._chop * _amplitudes[componentIdx];
                    _waveData[vi]._chopAmp2[ei] = -chopScale * _activeSpectrum._chop * _amplitudes2[componentIdx];

                    float angle = Mathf.Deg2Rad * _angleDegs[componentIdx];
                    float dx = Mathf.Cos(angle);
                    float dz = Mathf.Sin(angle);

                    float gravityScale = _activeSpectrum._gravityScales[(componentIdx) / _componentsPerOctave];
                    float gravity = OceanRenderer.Instance.Gravity * _activeSpectrum._gravityScale;
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

            _lastCascade = BlendMode == ShapeBlendMode.Blend ? CASCADE_COUNT - 1 : cascadeIdx;

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
                //Debug.Log($"Crest: {cascadeIdx}: start {_cascadeParams[cascadeIdx]._startIndex} minWL {minWl}");
            }

            _lastCascade = CASCADE_COUNT - 1;

            // Compute a measure of variance, cumulative from low cascades to high
            for (int i = 0; i < CASCADE_COUNT; i++)
            {
                // Accumulate from lower cascades
                _cascadeParams[i]._cumulativeVariance = i > 0 ? _cascadeParams[i - 1]._cumulativeVariance : 0f;

                var wl = MinWavelength(i) * 1.5f;
                var octaveIndex = OceanWaveSpectrum.GetOctaveIndex(wl);
                octaveIndex = Mathf.Min(octaveIndex, _activeSpectrum._chopScales.Length - 1);

                // Heuristic - horiz disp is roughly amp*chop, divide by wavelength to normalize
                var amp = _activeSpectrum.GetAmplitude(wl, 1f, windSpeed, out _);
                var chop = _activeSpectrum._chopScales[octaveIndex];
                float amp_over_wl = chop * amp / wl;
                _cascadeParams[i]._cumulativeVariance += amp_over_wl;
            }
            _cascadeParams[CASCADE_COUNT]._cumulativeVariance = _cascadeParams[CASCADE_COUNT - 1]._cumulativeVariance;

            _bufCascadeParams.SetData(_cascadeParams);
            _bufWaveData.SetData(_waveData);
        }

        void UpdateGenerateWaves(CommandBuffer buf)
        {
            buf.SetComputeFloatParam(_shaderGerstner, sp_TextureRes, _waveBuffers.width);
            buf.SetComputeFloatParam(_shaderGerstner, OceanRenderer.sp_crestTime, OceanRenderer.Instance.CurrentTime);
            buf.SetComputeIntParam(_shaderGerstner, sp_FirstCascadeIndex, _firstCascade);
            buf.SetComputeBufferParam(_shaderGerstner, _krnlGerstner, sp_CascadeParams, _bufCascadeParams);
            buf.SetComputeBufferParam(_shaderGerstner, _krnlGerstner, sp_GerstnerWaveData, _bufWaveData);
            buf.SetComputeTextureParam(_shaderGerstner, _krnlGerstner, sp_WaveBuffer, _waveBuffers);

            buf.DispatchCompute(_shaderGerstner, _krnlGerstner, _waveBuffers.width / LodDataMgr.THREAD_GROUP_SIZE_X, _waveBuffers.height / LodDataMgr.THREAD_GROUP_SIZE_Y, _lastCascade - _firstCascade + 1);
        }

        /// <summary>
        /// Resamples wave spectrum
        /// </summary>
        /// <param name="windSpeed">Wind speed in m/s</param>
        public void UpdateWaveData(float windSpeed)
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            _activeSpectrum.GenerateWaveData(_componentsPerOctave, ref _wavelengths, ref _angleDegs);

            UpdateAmplitudes();

            // Won't run every time so put last in the random sequence
            if (_phases == null || _phases.Length != _wavelengths.Length || _phases2 == null || _phases2.Length != _wavelengths.Length)
            {
                InitPhases();
            }

            Random.state = randomStateBkp;

            SliceUpWaves(windSpeed);
        }

        void UpdateAmplitudes()
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

            var windSpeed = WindSpeed;

            for (int i = 0; i < _wavelengths.Length; i++)
            {
                var amp = _activeSpectrum.GetAmplitude(_wavelengths[i], _componentsPerOctave, windSpeed, out _powers[i]);
                _amplitudes[i] = Random.value * amp;
                _amplitudes2[i] = Random.value * amp * _reverseWaveWeight;
            }
        }

        void InitPhases()
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            var totalComps = _componentsPerOctave * OceanWaveSpectrum.NUM_OCTAVES;
            _phases = new float[totalComps];
            _phases2 = new float[totalComps];
            for (var octave = 0; octave < OceanWaveSpectrum.NUM_OCTAVES; octave++)
            {
                for (var i = 0; i < _componentsPerOctave; i++)
                {
                    var index = octave * _componentsPerOctave + i;
                    var rnd = (i + Random.value) / _componentsPerOctave;
                    _phases[index] = 2f * Mathf.PI * rnd;

                    var rnd2 = (i + Random.value) / _componentsPerOctave;
                    _phases2[index] = 2f * Mathf.PI * rnd2;
                }
            }

            Random.state = randomStateBkp;
        }

        protected override void ReportMaxDisplacement()
        {
            if (_activeSpectrum._chopScales.Length != OceanWaveSpectrum.NUM_OCTAVES)
            {
                Debug.LogError($"Crest: OceanWaveSpectrum {_activeSpectrum.name} is out of date, please open this asset and resave in editor.", _activeSpectrum);
            }

            float ampSum = 0f;
            for (int i = 0; i < _wavelengths.Length; i++)
            {
                ampSum += _amplitudes[i] * _activeSpectrum._chopScales[i / _componentsPerOctave];
            }

            // Apply weight or will cause popping due to scale change.
            ampSum *= _weight;

            _maxHorizDisp = ampSum * _activeSpectrum._chop;
            _maxVertDisp = ampSum;
            _maxWavesDisp = ampSum;

            if (IsGlobalWaves)
            {
                OceanRenderer.Instance.ReportMaxDisplacementFromShape(ampSum * _activeSpectrum._chop, ampSum, ampSum);
            }
        }

        protected override void OnEnable()
        {
            Instances.Add(transform.GetSiblingIndex(), this);

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Instances.Remove(this);

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

            if (_waveBuffers != null)
            {
                Helpers.Destroy(_waveBuffers);
                _waveBuffers = null;
            }
        }

        protected override void DestroySharedResources() {}

#if UNITY_EDITOR
        void OnGUI()
        {
            if (_debug._drawSlicesInEditor && _waveBuffers != null && _waveBuffers.IsCreated())
            {
                OceanDebugGUI.DrawTextureArray(_waveBuffers, 8, 0.5f);
            }
        }
#endif
    }
}
