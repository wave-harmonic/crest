// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using Crest.Spline;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    /// <summary>
    /// Gerstner ocean waves.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Shape Gerstner")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "wave-conditions.html" + Internal.Constants.HELP_URL_RP)]
    public partial class ShapeGerstner : MonoBehaviour, LodDataMgrAnimWaves.IShapeUpdatable
        , ISplinePointCustomDataSetup
#if UNITY_EDITOR
        , IReceiveSplinePointOnDrawGizmosSelectedMessages
#endif
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Tooltip("The spectrum that defines the ocean surface shape. Assign asset of type Crest/Ocean Waves Spectrum."), Embedded]
        public OceanWaveSpectrum _spectrum;
        OceanWaveSpectrum _activeSpectrum = null;

        [Tooltip("When true, the wave spectrum is evaluated once on startup in editor play mode and standalone builds, rather than every frame. This is less flexible but reduces the performance cost significantly."), SerializeField]
        bool _spectrumFixedAtRuntime = true;

        [Tooltip("Primary wave direction heading (deg). This is the angle from x axis in degrees that the waves are oriented towards. If a spline is being used to place the waves, this angle is relative ot the spline."), Range(-180, 180)]
        public float _waveDirectionHeadingAngle = 0f;
        public Vector2 PrimaryWaveDirection => new Vector2(Mathf.Cos(Mathf.PI * _waveDirectionHeadingAngle / 180f), Mathf.Sin(Mathf.PI * _waveDirectionHeadingAngle / 180f));

        [Tooltip("When true, uses the wind speed on this component rather than the wind speed from the Ocean Renderer component.")]
        public bool _overrideGlobalWindSpeed = false;
        [Tooltip("Wind speed in km/h. Controls wave conditions."), Range(0, 150f, power: 2f), Predicated("_overrideGlobalWindSpeed")]
        public float _windSpeed = 20f;

        [Tooltip("Multiplier for these waves to scale up/down."), Range(0f, 1f)]
        public float _weight = 1f;

        [Tooltip("How much these waves respect the shallow water attenuation setting in the Animated Waves Settings. Set to 0 to ignore shallow water."), SerializeField, Range(0f, 1f)]
        float _respectShallowWaterAttenuation = 1f;

        [Header("Generation Settings")]
        [Delayed, Tooltip("How many wave components to generate in each octave.")]
        public int _componentsPerOctave = 8;

        [Tooltip("Change to get a different set of waves.")]
        public int _randomSeed = 0;

        [Tooltip("Resolution to use for wave generation buffers. Low resolutions are more efficient but can result in noticeable patterns in the shape."), Delayed]
        public int _resolution = 32;

        [Tooltip("In Editor, shows the wave generation buffers on screen."), SerializeField]
#pragma warning disable 414
        bool _debugDrawSlicesInEditor = false;
#pragma warning restore 414

        [Header("Spline settings")]
        [SerializeField]
        bool _overrideSplineSettings = false;
        [SerializeField, Predicated("_overrideSplineSettings"), DecoratedField]
        float _radius = 20f;
        [SerializeField, Predicated("_overrideSplineSettings"), Delayed]
        int _subdivisions = 1;

        [SerializeField]
        float _featherWaveStart = 0.1f;

        Mesh _meshForDrawingWaves;

        float _windSpeedWhenGenerated = -1f;

        public class GerstnerBatch : ILodDataInput
        {
            ShapeGerstner _gerstner;

            Material _material;
            Mesh _mesh;

            int _waveBufferSliceIndex;

            public GerstnerBatch(ShapeGerstner gerstner, float wavelength, int waveBufferSliceIndex, Material material, Mesh mesh)
            {
                _gerstner = gerstner;
                Wavelength = wavelength;
                _waveBufferSliceIndex = waveBufferSliceIndex;
                _mesh = mesh;
                _material = material;
            }

            // The ocean input system uses this to decide which lod this batch belongs in
            public float Wavelength { get; private set; }

            public bool Enabled { get => true; set { } }

            public void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
            {
                var finalWeight = weight * _gerstner._weight;
                if (finalWeight > 0f)
                {
                    buf.SetGlobalInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                    buf.SetGlobalFloat(RegisterLodDataInputBase.sp_Weight, finalWeight);
                    buf.SetGlobalInt(sp_WaveBufferSliceIndex, _waveBufferSliceIndex);
                    buf.SetGlobalFloat(sp_AverageWavelength, Wavelength * 1.5f);

                    // Either use a full screen quad, or a provided mesh renderer to draw the waves
                    if (_mesh == null)
                    {
                        buf.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
                    }
                    else if (_material != null)
                    {
                        buf.DrawMesh(_mesh, _gerstner.transform.localToWorldMatrix, _material);
                    }
                }
            }
        }

        const int CASCADE_COUNT = 16;
        const int MAX_WAVE_COMPONENTS = 1024;

        GerstnerBatch[] _batches = null;

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

        // First cascade of wave buffer that has waves and will be rendered
        int _firstCascade = -1;
        // Last cascade of wave buffer that has waves and will be rendered
        int _lastCascade = -1;

        // Used to populate data on first frame
        bool _firstUpdate = true;

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

        // Active material.
        Material _matGenerateWaves;
        // Cache material options.
        Material _matGenerateWavesGlobal;
        Material _matGenerateWavesGeometry;

        readonly int sp_FirstCascadeIndex = Shader.PropertyToID("_FirstCascadeIndex");
        readonly int sp_TextureRes = Shader.PropertyToID("_TextureRes");
        readonly int sp_CascadeParams = Shader.PropertyToID("_GerstnerCascadeParams");
        readonly int sp_GerstnerWaveData = Shader.PropertyToID("_GerstnerWaveData");
        static readonly int sp_WaveBuffer = Shader.PropertyToID("_WaveBuffer");
        static readonly int sp_WaveBufferSliceIndex = Shader.PropertyToID("_WaveBufferSliceIndex");
        static readonly int sp_AverageWavelength = Shader.PropertyToID("_AverageWavelength");
        static readonly int sp_RespectShallowWaterAttenuation = Shader.PropertyToID("_RespectShallowWaterAttenuation");
        static readonly int sp_FeatherWaveStart = Shader.PropertyToID("_FeatherWaveStart");
        static readonly int sp_MaximumAttenuationDepth = Shader.PropertyToID("_MaximumAttenuationDepth");
        readonly int sp_AxisX = Shader.PropertyToID("_AxisX");

        readonly float _twoPi = 2f * Mathf.PI;
        readonly float _recipTwoPi = 1f / (2f * Mathf.PI);

        internal static readonly CrestSortedList<int, ShapeGerstner> Instances = new CrestSortedList<int, ShapeGerstner>(new SiblingIndexComparer());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            Instances.Clear();
        }

        void InitData()
        {
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
            }

            _bufCascadeParams = new ComputeBuffer(CASCADE_COUNT + 1, UnsafeUtility.SizeOf<GerstnerCascadeParams>());
            _bufWaveData = new ComputeBuffer(MAX_WAVE_COMPONENTS / 4, UnsafeUtility.SizeOf<GerstnerWaveComponent4>());

            _shaderGerstner = ComputeShaderHelpers.LoadShader("Gerstner");
            _krnlGerstner = _shaderGerstner.FindKernel("Gerstner");
        }

        /// <summary>
        /// Min wavelength for a cascade in the wave buffer. Does not depend on viewpoint.
        /// </summary>
        public float MinWavelength(int cascadeIdx)
        {
            var diameter = 0.5f * (1 << cascadeIdx);
            var texelSize = diameter / _resolution;
            // Nyquist rate
            return texelSize * 2f;
        }

        public void CrestUpdate(CommandBuffer buf)
        {
#if UNITY_EDITOR
            UpdateEditorOnly();
#endif

            if (_waveBuffers == null || _resolution != _waveBuffers.width || _bufCascadeParams == null || _bufWaveData == null)
            {
                InitData();
            }

            var updateDataEachFrame = !_spectrumFixedAtRuntime;
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) updateDataEachFrame = true;
#endif

            // Calc wind speed in m/s
            var windSpeed = _overrideGlobalWindSpeed ? _windSpeed : OceanRenderer.Instance._globalWindSpeed;
            windSpeed /= 3.6f;

            if (_firstUpdate || updateDataEachFrame || windSpeed != _windSpeedWhenGenerated)
            {
                UpdateWaveData(windSpeed);

                InitBatches();

                _firstUpdate = false;
                _windSpeedWhenGenerated = windSpeed;
            }

            _matGenerateWaves.SetFloat(sp_RespectShallowWaterAttenuation, _respectShallowWaterAttenuation);
            _matGenerateWaves.SetFloat(sp_FeatherWaveStart, _featherWaveStart);
            _matGenerateWaves.SetFloat(sp_MaximumAttenuationDepth, OceanRenderer.Instance._lodDataAnimWaves.Settings.MaximumAttenuationDepth);
            _matGenerateWaves.SetVector(sp_AxisX, PrimaryWaveDirection);
            // Seems like shader errors cause this to unbind if I don't set it every frame. Could be an editor only issue.
            _matGenerateWaves.SetTexture(sp_WaveBuffer, _waveBuffers);

            ReportMaxDisplacement();

            // If some cascades have waves in them, generate
            if (_firstCascade != -1 && _lastCascade != -1)
            {
                UpdateGenerateWaves(buf);
            }

            // Seems to come unbound when editing shaders at runtime, so rebinding here.
            _matGenerateWaves.SetTexture(sp_WaveBuffer, _waveBuffers);
        }

#if UNITY_EDITOR
        void UpdateEditorOnly()
        {
            if (_spectrum != null)
            {
                _activeSpectrum = _spectrum;
            }

            if (_activeSpectrum == null)
            {
                _activeSpectrum = ScriptableObject.CreateInstance<OceanWaveSpectrum>();
                _activeSpectrum.name = "Default Waves (auto)";
            }

            // Unassign mesh
            if (_meshForDrawingWaves != null && !TryGetComponent<Spline.Spline>(out _))
            {
                _meshForDrawingWaves = null;
            }
        }
#endif

        void SliceUpWaves(float windSpeed)
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

            // Calc wind speed in m/s
            var windSpeed = _overrideGlobalWindSpeed ? _windSpeed : OceanRenderer.Instance._globalWindSpeed;
            windSpeed /= 3.6f;

            for (int i = 0; i < _wavelengths.Length; i++)
            {
                var amp = _weight * _activeSpectrum.GetAmplitude(_wavelengths[i], _componentsPerOctave, windSpeed, out _powers[i]);
                _amplitudes[i] = Random.value * amp;
                _amplitudes2[i] = Random.value * amp * 0.5f;
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

        private void ReportMaxDisplacement()
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
            OceanRenderer.Instance.ReportMaxDisplacementFromShape(ampSum * _activeSpectrum._chop, ampSum, ampSum);
        }

        void InitBatches()
        {
            var registered = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrAnimWaves));

            if (_batches != null)
            {
                foreach (var batch in _batches)
                {
                    registered.Remove(batch);
                }
            }

            if (TryGetComponent<Spline.Spline>(out var splineForWaves))
            {
                var radius = _overrideSplineSettings ? _radius : splineForWaves.Radius;
                var subdivs = _overrideSplineSettings ? _subdivisions : splineForWaves.Subdivisions;

                if (ShapeGerstnerSplineHandling.GenerateMeshFromSpline<SplinePointDataGerstner>(splineForWaves, transform, subdivs, radius, Vector2.one,
                    ref _meshForDrawingWaves, out _, out _))
                {
                    _meshForDrawingWaves.name = gameObject.name + "_mesh";
                }
            }

            if (_meshForDrawingWaves == null)
            {
                if (_matGenerateWavesGlobal == null)
                {
                    _matGenerateWavesGlobal = new Material(Shader.Find("Hidden/Crest/Inputs/Animated Waves/Gerstner Global"));
                    _matGenerateWavesGlobal.hideFlags = HideFlags.HideAndDontSave;
                }

                _matGenerateWaves = _matGenerateWavesGlobal;
            }
            else
            {
                if (_matGenerateWavesGeometry == null)
                {
                    _matGenerateWavesGeometry = new Material(Shader.Find("Crest/Inputs/Animated Waves/Gerstner Geometry"));
                    _matGenerateWavesGeometry.hideFlags = HideFlags.HideAndDontSave;
                }

                _matGenerateWaves = _matGenerateWavesGeometry;
            }

            // Submit draws to create the Gerstner waves
            _batches = new GerstnerBatch[CASCADE_COUNT];
            for (int i = _firstCascade; i <= _lastCascade; i++)
            {
                if (i == -1) break;
                _batches[i] = new GerstnerBatch(this, MinWavelength(i), i, _matGenerateWaves, _meshForDrawingWaves);
                registered.Add(0, _batches[i]);
            }
        }

        private void OnEnable()
        {
            Instances.Add(transform.GetSiblingIndex(), this);

            _firstUpdate = true;

            // Initialise with spectrum
            if (_spectrum != null)
            {
                _activeSpectrum = _spectrum;
            }

            if (_activeSpectrum == null)
            {
                _activeSpectrum = ScriptableObject.CreateInstance<OceanWaveSpectrum>();
                _activeSpectrum.name = "Default Waves (auto)";
            }

#if UNITY_EDITOR
            if (EditorApplication.isPlaying && !Validate(OceanRenderer.Instance, ValidatedHelper.DebugLog))
            {
                enabled = false;
                return;
            }
#endif

            LodDataMgrAnimWaves.RegisterUpdatable(this);
        }

        void OnDisable()
        {
            Instances.Remove(this);

            LodDataMgrAnimWaves.DeregisterUpdatable(this);

            if (_batches != null)
            {
                var registered = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrAnimWaves));
                foreach (var batch in _batches)
                {
                    registered.Remove(batch);
                }

                _batches = null;
            }

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
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying)
                {
                    DestroyImmediate(_waveBuffers);
                }
                else
#endif
                {
                    Destroy(_waveBuffers);
                }
                _waveBuffers = null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawMesh();
        }

        void DrawMesh()
        {
            if (_meshForDrawingWaves != null)
            {
                Gizmos.color = RegisterAnimWavesInput.s_gizmoColor;
                Gizmos.DrawWireMesh(_meshForDrawingWaves, 0, transform.position, transform.rotation, transform.lossyScale);
            }
        }

        void OnGUI()
        {
            if (_debugDrawSlicesInEditor)
            {
                OceanDebugGUI.DrawTextureArray(_waveBuffers, 8);
            }
        }

        public void OnSplinePointDrawGizmosSelected(SplinePoint point)
        {
            DrawMesh();
        }
#endif

        public bool AttachDataToSplinePoint(GameObject splinePoint)
        {
            if (splinePoint.TryGetComponent(out SplinePointDataGerstner _))
            {
                // Already existing, nothing to do
                return false;
            }

            splinePoint.AddComponent<SplinePointDataGerstner>();
            return true;
        }
    }

#if UNITY_EDITOR
    public partial class ShapeGerstner : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            if (TryGetComponent<Spline.Spline>(out var spline) && !spline.Validate(ocean, ValidatedHelper.Suppressed))
            {
                showMessage
                (
                    "A <i>Spline</i> component is attached but it has validation errors.",
                    "Check this component in the Inspector for issues.",
                    ValidatedHelper.MessageType.Error, this
                );
            }

            if (showMessage == ValidatedHelper.HelpBox)
            {
                showMessage
                (
                    "The <i>ShapeGerstner</i> component is now obsolete.",
                    "Prefer using <i>ShapeFFT</i> instead.",
                    ValidatedHelper.MessageType.Warning, this
                );
            }

            return isValid;
        }
    }

    // Here for the help boxes
    [CustomEditor(typeof(ShapeGerstner))]
    public class ShapeGerstnerEditor : ValidatedEditor { }
#endif
}
