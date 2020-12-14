// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    /// <summary>
    /// Support script for Gerstner wave ocean shapes.
    /// Generates a number of batches of Gerstner waves.
    /// </summary>
    public partial class ShapeGerstner : MonoBehaviour, IFloatingOrigin
    {
        public enum GerstnerMode
        {
            Global,
            Geometry,
        }

        [Tooltip("If set to 'Global', waves will render everywhere. If set to 'Geometry', the geometry on this GameObject will be rendered from a top down perspective to generate the waves. This allows having local wave conditions by placing Quad geometry where desired. The geometry must have one of the Gerstner shaders on it such as 'Crest/Inputs/Animated Waves/Gerstner Batch Geometry'.")]
        public GerstnerMode _mode = GerstnerMode.Global;

        [Tooltip("The spectrum that defines the ocean surface shape. Create asset of type Crest/Ocean Waves Spectrum.")]
        public OceanWaveSpectrum _spectrum;

        [Tooltip("Wind direction (angle from x axis in degrees)"), Range(-180, 180)]
        public float _windDirectionAngle = 0f;
        public Vector2 WindDir => new Vector2(Mathf.Cos(Mathf.PI * _windDirectionAngle / 180f), Mathf.Sin(Mathf.PI * _windDirectionAngle / 180f));

        public class GerstnerBatch : ILodDataInput
        {
            public GerstnerBatch(ShapeGerstner gerstner, int batchIndex, MeshRenderer rend)
            {
                _gerstner = gerstner;
                _batchIndex = batchIndex;

                _materials = new PropertyWrapperMaterial[]
                {
                    new PropertyWrapperMaterial(new Material(rend.sharedMaterial ?? rend.material)),
                    new PropertyWrapperMaterial(new Material(rend.sharedMaterial ?? rend.material))
                };

                _rend = rend;

                // Enabled stays true, because we don't sort the waves into buckets until Draw time, so we don't know if something should
                // be drawn in advance.
                Enabled = true;
            }

            public PropertyWrapperMaterial GetMaterial(int isTransition) => _materials[isTransition];

            // Two materials because as batch may be rendered twice if it has large wavelengths that are being transitioned back
            // and forth across the last 2 LODs.
            PropertyWrapperMaterial[] _materials;

            MeshRenderer _rend;

            ShapeGerstner _gerstner;
            int _batchIndex = -1;

            // The ocean input system uses this to decide which lod this batch belongs in
            public float Wavelength => OceanRenderer.Instance._lodTransform.MaxWavelength(_batchIndex) / 2f;

            public bool Enabled { get; set; }

            public bool HasWaves { get; set; }

            public void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
            {
                HasWaves = false;
                _gerstner.UpdateBatch(this, _batchIndex);

                if (HasWaves && weight > 0f)
                {
                    PropertyWrapperMaterial mat = GetMaterial(isTransition);
                    mat.SetFloat(RegisterLodDataInputBase.sp_Weight, weight);
                    buf.DrawRenderer(_rend, mat.material);
                }
            }
        }

        GerstnerBatch[] _batches = null;

        [Delayed, Tooltip("How many wave components to generate in each octave.")]
        public int _componentsPerOctave = 8;

        [Range(0f, 1f)]
        public float _weight = 1f;

        public int _randomSeed = 0;

        // Data for all components
        [Header("Wave data (usually populated at runtime)")]
        public bool _evaluateSpectrumAtRuntime = true;
        [PredicatedField("_evaluateSpectrumAtRuntime", true)]
        public float[] _wavelengths;
        [PredicatedField("_evaluateSpectrumAtRuntime", true)]
        public float[] _amplitudes;
        [PredicatedField("_evaluateSpectrumAtRuntime", true)]
        public float[] _angleDegs;
        [PredicatedField("_evaluateSpectrumAtRuntime", true)]
        public float[] _phases;

        public int _resolution = 32;
        RenderTexture _waveBuffers;

        struct GerstnerCascadeParams
        {
            public int _startIndex;
        }
        ComputeBuffer _bufCascadeParams;
        static int sp_cascadeParams = Shader.PropertyToID("_GerstnerCascadeParams");
        GerstnerCascadeParams[] _cascadeParams = new GerstnerCascadeParams[LodDataMgr.MAX_LOD_COUNT + 1];

        int _firstCascade = -1;
        int _lastCascade = -1;

        struct GerstnerWaveComponent4
        {
            public Vector4 _twoPiOverWavelengths;
            public Vector4 _amps;
            public Vector4 _waveDirX;
            public Vector4 _waveDirZ;
            public Vector4 _phases;
            public Vector4 _chopAmps;
        }
        ComputeBuffer _bufWaveData;
        const int MAX_WAVE_COMPONENTS = 1024;
        static int sp_waveData = Shader.PropertyToID("_GerstnerWaveData");
        GerstnerWaveComponent4[] _waveData = new GerstnerWaveComponent4[MAX_WAVE_COMPONENTS / 4];

        ComputeShader _shaderGerstner;
        int _krnlGerstner = -1;

        CommandBuffer _buf;

        readonly int sp_NumInBatch = Shader.PropertyToID("_NumInBatch");
        readonly int sp_AttenuationInShallows = Shader.PropertyToID("_AttenuationInShallows");
        readonly int sp_NumWaveVecs = Shader.PropertyToID("_NumWaveVecs");

        // IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatch.shader, both must be updated together!
        const int BATCH_SIZE = 32;

        GameObject _renderProxy;

        // scratch data used by batching code

        private void OnEnable()
        {
#if UNITY_EDITOR
            // Initialise with spectrum
            if (_spectrum == null)
            {
                _spectrum = ScriptableObject.CreateInstance<OceanWaveSpectrum>();
                _spectrum.name = "Default Waves (auto)";
            }

            if (EditorApplication.isPlaying && !Validate(OceanRenderer.Instance, ValidatedHelper.DebugLog))
            {
                enabled = false;
                return;
            }

            _spectrum.Upgrade();
#endif

            InitData();

            InitBatches();
        }

        void InitData()
        {
            {
                var desc = new RenderTextureDescriptor(_resolution, _resolution, GraphicsFormat.R16G16B16A16_SFloat, 0);
                _waveBuffers = new RenderTexture(desc);
                _waveBuffers.wrapMode = TextureWrapMode.Clamp;
                _waveBuffers.antiAliasing = 1;
                _waveBuffers.filterMode = FilterMode.Bilinear;
                _waveBuffers.anisoLevel = 0;
                _waveBuffers.useMipMap = false;
                _waveBuffers.name = "GerstnerCascades";
                _waveBuffers.dimension = TextureDimension.Tex2DArray;
                _waveBuffers.volumeDepth = LodDataMgr.MAX_LOD_COUNT;
                _waveBuffers.enableRandomWrite = true;
                _waveBuffers.Create();
            }

            _bufCascadeParams = new ComputeBuffer(LodDataMgr.MAX_LOD_COUNT + 1, UnsafeUtility.SizeOf<GerstnerCascadeParams>());
            _bufWaveData = new ComputeBuffer(MAX_WAVE_COMPONENTS / 4, UnsafeUtility.SizeOf<GerstnerWaveComponent4>());

            _shaderGerstner = ComputeShaderHelpers.LoadShader("Gerstner");
            _krnlGerstner = _shaderGerstner.FindKernel("Gerstner");

            _buf = new CommandBuffer();
            _buf.name = "ShapeGerstner";
        }

        public float MinWavelength(int cascadeIdx)
        {
            var diameter = (float)(1 << cascadeIdx);
            var texelSize = diameter / _resolution;
            return texelSize * OceanRenderer.Instance.MinTexelsPerWave;
        }
        float twopi = 2f * Mathf.PI;
        float one_over_2pi = 1f / (2f * Mathf.PI);

        void Update()
        {
            // We are using the render proxy to hold state since we need to anyway.
            if (_renderProxy != null ? _mode == GerstnerMode.Geometry : _mode == GerstnerMode.Global)
            {
                InitBatches();
            }

            // Should be done only once when sampling a spectrum
            SliceUpWaves();

            if (_firstCascade != -1 && _lastCascade != -1)
            {
                //Debug.Log($"{_firstCascade} to {_lastCascade}");

                _buf.Clear();
                _buf.SetComputeFloatParam(_shaderGerstner, "_TextureRes", _waveBuffers.width);
                _buf.SetComputeBufferParam(_shaderGerstner, _krnlGerstner, sp_cascadeParams, _bufCascadeParams);
                _buf.SetComputeBufferParam(_shaderGerstner, _krnlGerstner, "_GerstnerWaveData", _bufWaveData);
                _buf.SetComputeTextureParam(_shaderGerstner, _krnlGerstner, "_WaveBuffer", _waveBuffers);
                _buf.DispatchCompute(_shaderGerstner, _krnlGerstner, _waveBuffers.width / 8, _waveBuffers.height / 8, _lastCascade - _firstCascade + 1);
                Graphics.ExecuteCommandBuffer(_buf);
            }
        }

        void SliceUpWaves()
        {
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
                // Skip small waves
                while (componentIdx < _wavelengths.Length && _amplitudes[componentIdx] < 0.01f)
                {
                    componentIdx++;
                }
                if (componentIdx >= _wavelengths.Length) break;

                // Check if we need to move to the next cascade
                while (cascadeIdx < LodDataMgr.MAX_LOD_COUNT && _wavelengths[componentIdx] >= 2f * minWl)
                {
                    // Wrap up this cascade and begin next

                    // Fill remaining elements of current vector4 with 0s
                    int vi = outputIdx / 4;
                    int ei = outputIdx - vi * 4;

                    while (ei != 0)
                    {
                        _waveData[vi]._twoPiOverWavelengths[ei] = 1f;
                        _waveData[vi]._amps[ei] = 0f;
                        _waveData[vi]._waveDirX[ei] = 0f;
                        _waveData[vi]._waveDirZ[ei] = 0f;
                        _waveData[vi]._phases[ei] = 0f;
                        _waveData[vi]._chopAmps[ei] = 0f;
                        ei = (ei + 1) % 4;
                        outputIdx++;
                    }

                    if (outputIdx > 0 && _firstCascade == -1) _firstCascade = cascadeIdx;

                    cascadeIdx++;
                    _cascadeParams[cascadeIdx]._startIndex = outputIdx / 4;
                    minWl *= 2f;

                    //Debug.Log($"{cascadeIdx}: start {_cascadeParams[cascadeIdx]._startIndex} minWL {minWl}");
                }
                if (cascadeIdx == LodDataMgr.MAX_LOD_COUNT) break;

                {
                    // Pack into vector elements
                    int vi = outputIdx / 4;
                    int ei = outputIdx - vi * 4;

                    _waveData[vi]._twoPiOverWavelengths[ei] = 2f * Mathf.PI / _wavelengths[componentIdx];
                    _waveData[vi]._amps[ei] = _amplitudes[componentIdx];

                    float chopScale = _spectrum._chopScales[(componentIdx) / _componentsPerOctave];
                    _waveData[vi]._chopAmps[ei] = -chopScale * _spectrum._chop * _amplitudes[componentIdx];

                    float angle = Mathf.Deg2Rad * (_windDirectionAngle + _angleDegs[componentIdx]);
                    _waveData[vi]._waveDirX[ei] = Mathf.Cos(angle);
                    _waveData[vi]._waveDirZ[ei] = Mathf.Sin(angle);

                    // It used to be this, but I'm pushing all the stuff that doesn't depend on position into the phase.
                    //half4 angle = k * (C * _CrestTime + x) + _Phases[vi];
                    float gravityScale = _spectrum._gravityScales[(componentIdx) / _componentsPerOctave];
                    float gravity = OceanRenderer.Instance.Gravity * _spectrum._gravityScale;
                    float C = Mathf.Sqrt(_wavelengths[componentIdx] * gravity * gravityScale * one_over_2pi);
                    float k = twopi / _wavelengths[componentIdx];
                    // Repeat every 2pi to keep angle bounded - helps precision on 16bit platforms
                    _waveData[vi]._phases[ei] = Mathf.Repeat(_phases[componentIdx] + k * C * OceanRenderer.Instance.CurrentTime, Mathf.PI * 2f);

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
                    _waveData[vi]._twoPiOverWavelengths[ei] = 1f;
                    _waveData[vi]._amps[ei] = 0f;
                    _waveData[vi]._waveDirX[ei] = 0f;
                    _waveData[vi]._waveDirZ[ei] = 0f;
                    _waveData[vi]._phases[ei] = 0f;
                    _waveData[vi]._chopAmps[ei] = 0f;
                    ei = (ei + 1) % 4;
                    outputIdx++;
                }
            }

            while (cascadeIdx < LodDataMgr.MAX_LOD_COUNT)
            {
                cascadeIdx++;
                minWl *= 2f;
                _cascadeParams[cascadeIdx]._startIndex = outputIdx / 4;
                //Debug.Log($"{cascadeIdx}: start {_cascadeParams[cascadeIdx]._startIndex} minWL {minWl}");
            }

            _bufCascadeParams.SetData(_cascadeParams);
            _bufWaveData.SetData(_waveData);
        }

        void InitPhases()
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            var totalComps = _componentsPerOctave * OceanWaveSpectrum.NUM_OCTAVES;
            _phases = new float[totalComps];
            for (var octave = 0; octave < OceanWaveSpectrum.NUM_OCTAVES; octave++)
            {
                for (var i = 0; i < _componentsPerOctave; i++)
                {
                    var index = octave * _componentsPerOctave + i;
                    var rnd = (i + Random.value) / _componentsPerOctave;
                    _phases[index] = 2f * Mathf.PI * rnd;
                }
            }

            Random.state = randomStateBkp;
        }

        public void SetOrigin(Vector3 newOrigin)
        {
            if (_phases == null) return;

            var windAngle = _windDirectionAngle;
            for (int i = 0; i < _phases.Length; i++)
            {
                var direction = new Vector3(Mathf.Cos((windAngle + _angleDegs[i]) * Mathf.Deg2Rad), 0f, Mathf.Sin((windAngle + _angleDegs[i]) * Mathf.Deg2Rad));
                var phaseOffsetMeters = Vector3.Dot(newOrigin, direction);

                // wave number
                var k = 2f * Mathf.PI / _wavelengths[i];

                _phases[i] = Mathf.Repeat(_phases[i] + phaseOffsetMeters * k, Mathf.PI * 2f);
            }
        }

        int _lastFrameForUpdateData = -1;

        void UpdateData()
        {
            if (OceanRenderer.Instance == null) return;

            // We only want this to be executed once per frame.
            if (_lastFrameForUpdateData == OceanRenderer.FrameCount) return;
            _lastFrameForUpdateData = OceanRenderer.FrameCount;

            if (_evaluateSpectrumAtRuntime)
            {
                UpdateWaveData();
            }

            ReportMaxDisplacement();
        }

        public void UpdateWaveData()
        {
            // Set random seed to get repeatable results
            Random.State randomStateBkp = Random.state;
            Random.InitState(_randomSeed);

            _spectrum.GenerateWaveData(_componentsPerOctave, ref _wavelengths, ref _angleDegs);

            UpdateAmplitudes();

            // Won't run every time so put last in the random sequence
            if (_phases == null || _phases.Length != _wavelengths.Length)
            {
                InitPhases();
            }

            Random.state = randomStateBkp;
        }

        void UpdateAmplitudes()
        {
            if (_amplitudes == null || _amplitudes.Length != _wavelengths.Length)
            {
                _amplitudes = new float[_wavelengths.Length];
            }

            for (int i = 0; i < _wavelengths.Length; i++)
            {
                _amplitudes[i] = _weight * _spectrum.GetAmplitude(_wavelengths[i], _componentsPerOctave);
            }
        }

        private void ReportMaxDisplacement()
        {
            if (_spectrum._chopScales.Length != OceanWaveSpectrum.NUM_OCTAVES)
            {
                Debug.LogError($"OceanWaveSpectrum {_spectrum.name} is out of date, please open this asset and resave in editor.", _spectrum);
            }

            float ampSum = 0f;
            for (int i = 0; i < _wavelengths.Length; i++)
            {
                ampSum += _amplitudes[i] * _spectrum._chopScales[i / _componentsPerOctave];
            }
            OceanRenderer.Instance.ReportMaxDisplacementFromShape(ampSum * _spectrum._chop, ampSum, ampSum);
        }

        void InitBatches()
        {
            // Get the wave
            MeshRenderer rend = GetComponent<MeshRenderer>();
            if (_mode == GerstnerMode.Geometry)
            {
                rend.enabled = false;
#if UNITY_EDITOR
                // Cleanup render proxy used for global mode after switching.
                if (_renderProxy != null)
                {
                    DestroyImmediate(_renderProxy);
                }
#endif
            }
            else if (_mode == GerstnerMode.Global)
            {
                // Create render proxy only if we don't already have one.
                if (_renderProxy == null)
                {
                    // Create a proxy MeshRenderer to feed the rendering
                    _renderProxy = GameObject.CreatePrimitive(PrimitiveType.Quad);
#if UNITY_EDITOR
                    DestroyImmediate(_renderProxy.GetComponent<Collider>());
#else
                    Destroy(_renderProxy.GetComponent<Collider>());
#endif
                    _renderProxy.hideFlags = HideFlags.HideAndDontSave;
                    _renderProxy.transform.parent = transform;
                    rend = _renderProxy.GetComponent<MeshRenderer>();
                    rend.enabled = false;
                    var waveShader = Shader.Find("Hidden/Crest/Inputs/Animated Waves/Gerstner Batch Global");
                    Debug.Assert(waveShader, "Could not load Gerstner wave shader, make sure it is packaged in the build.");
                    if (waveShader == null)
                    {
                        enabled = false;
                        return;
                    }

                    rend.material = new Material(waveShader);
                }
                else
                {
                    rend = _renderProxy.GetComponent<MeshRenderer>();
                }
            }

            var registered = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrAnimWaves));

#if UNITY_EDITOR
            // Unregister after switching modes in the editor.
            if (_batches != null)
            {
                foreach (var batch in _batches)
                {
                    registered.Remove(batch);
                }
            }
#endif

            _batches = new GerstnerBatch[LodDataMgr.MAX_LOD_COUNT];
            for (int i = 0; i < _batches.Length; i++)
            {
                _batches[i] = new GerstnerBatch(this, i, rend);
            }

            // Submit draws to create the Gerstner waves. LODs from 0 to N-2 render the Gerstner waves from their lod. Additionally, any waves
            // in the biggest lod, or too big for the biggest lod, are rendered into both of the last two LODs N-1 and N-2, as this allows us to
            // move these waves between LODs without pops when the camera changes heights and the LODs need to change scale.

            foreach (var batch in _batches)
            {
                registered.Add(0, batch);
            }
        }

        /// <summary>
        /// Computes Gerstner params for a set of waves, for the given lod idx. Writes shader data to the given property.
        /// Returns number of wave components rendered in this batch.
        /// </summary>
        void UpdateBatch(int lodIdx, int firstComponent, int lastComponentNonInc, GerstnerBatch batch)
        {
            batch.HasWaves = false;

            int numComponents = lastComponentNonInc - firstComponent;
            int numInBatch = 0;
            int dropped = 0;

            float twopi = 2f * Mathf.PI;
            float one_over_2pi = 1f / twopi;

            // register any nonzero components
            for (int i = 0; i < numComponents; i++)
            {
                float wl = _wavelengths[firstComponent + i];

                // compute amp - contains logic for shifting wave components between last two LODs...
                float amp = _amplitudes[firstComponent + i];

                if (amp >= 0.001f)
                {
                    if (numInBatch < BATCH_SIZE)
                    {
                        numInBatch++;
                    }
                    else
                    {
                        dropped++;
                    }
                }
            }

            if (dropped > 0)
            {
                Debug.LogWarning(string.Format("Gerstner LOD{0}: Batch limit reached, dropped {1} wavelengths. To support bigger batch sizes, see the comment around the BATCH_SIZE declaration.", lodIdx, dropped), this);
                numComponents = BATCH_SIZE;
            }

            if (numInBatch == 0)
            {
                // no waves to draw - abort
                return;
            }

            // if we did not fill the batch, put a terminator signal after the last position
            if (numInBatch < BATCH_SIZE)
            {
                int vi_last = numInBatch / 4;
                int ei_last = numInBatch - vi_last * 4;

                for (int vi = vi_last; vi < BATCH_SIZE / 4; vi++)
                {
                    for (int ei = ei_last; ei < 4; ei++)
                    {
                    }

                    ei_last = 0;
                }
            }

            // apply the data to the shape property
            for (int i = 0; i < 2; i++)
            {
                var mat = batch.GetMaterial(i);
                mat.SetFloat(sp_NumInBatch, numInBatch);
                mat.SetFloat(sp_AttenuationInShallows, OceanRenderer.Instance._lodDataAnimWaves.Settings.AttenuationInShallows);

                int numVecs = (numInBatch + 3) / 4;
                mat.SetInt(sp_NumWaveVecs, numVecs);
                mat.SetInt(LodDataMgr.sp_LD_SliceIndex, lodIdx - i);

                LodDataMgrAnimWaves.Bind(mat);
                LodDataMgrSeaFloorDepth.Bind(mat);
            }

            batch.HasWaves = true;
        }

        void UpdateBatch(GerstnerBatch batch, int batchIdx)
        {
#if UNITY_EDITOR
            if (_spectrum == null) return;
#endif

            // Default to disabling all batches
            batch.HasWaves = false;

            if (OceanRenderer.Instance == null)
            {
                return;
            }

            UpdateData();

            if (_wavelengths.Length == 0)
            {
                return;
            }

            int lodIdx = Mathf.Min(batchIdx, OceanRenderer.Instance.CurrentLodCount - 1);

            int componentIdx = 0;

            // seek forward to first wavelength that is big enough to render into current LODs
            float minWl = OceanRenderer.Instance._lodTransform.MaxWavelength(batchIdx) / 2f;
            while (componentIdx < _wavelengths.Length && _wavelengths[componentIdx] < minWl)
            {
                componentIdx++;
            }

            // Assemble wavelengths into current batch
            int startCompIdx = componentIdx;
            while (componentIdx < _wavelengths.Length && _wavelengths[componentIdx] < 2f * minWl)
            {
                componentIdx++;
            }

            // One or more wavelengths - update the batch
            if (componentIdx > startCompIdx)
            {
                //Debug.Log($"Batch {batch}, lodIdx {lodIdx}, range: {minWl} -> {2f * minWl}, indices: {startCompIdx} -> {componentIdx}");
                UpdateBatch(lodIdx, startCompIdx, componentIdx, batch);
            }
        }

        void OnDisable()
        {
            if (_batches != null)
            {
                var registered = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrAnimWaves));
                foreach (var batch in _batches)
                {
                    registered.Remove(batch);
                }

                _batches = null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf)
            {
                Gizmos.color = RegisterAnimWavesInput.s_gizmoColor;
                Gizmos.DrawWireMesh(mf.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
            }
        }
#endif

        float ComputeWaveSpeed(float wavelength/*, float depth*/)
        {
            // wave speed of deep sea ocean waves: https://en.wikipedia.org/wiki/Wind_wave
            // https://en.wikipedia.org/wiki/Dispersion_(water_waves)#Wave_propagation_and_dispersion
            float g = 9.81f;
            float k = 2f * Mathf.PI / wavelength;
            //float h = max(depth, 0.01);
            //float cp = sqrt(abs(tanh_clamped(h * k)) * g / k);
            float cp = Mathf.Sqrt(g / k);
            return cp;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ShapeGerstner)), CanEditMultipleObjects]
    public class ShapeGerstnerEditor : ValidatedEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var gerstner = target as ShapeGerstner;

            GUI.enabled = !EditorApplication.isPlaying || !gerstner._evaluateSpectrumAtRuntime;
            if (GUILayout.Button("Generate wave data from spectrum"))
            {
                if (gerstner._spectrum == null)
                {
                    Debug.LogError("A wave spectrum must be assigned in order to generate wave data.", gerstner);
                }
                else
                {
                    gerstner.UpdateWaveData();
                }
            }
            GUI.enabled = true;
        }
    }

    public partial class ShapeGerstner : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            // Renderer
            if (_mode == GerstnerMode.Geometry)
            {
                isValid = ValidatedHelper.ValidateRenderer(gameObject, "Crest/Inputs/Animated Waves/Gerstner", showMessage);
            }
            else if (_mode == GerstnerMode.Global && GetComponent<MeshRenderer>() != null)
            {
                showMessage
                (
                    "The MeshRenderer component will be ignored because the Mode is set to Global.",
                    ValidatedHelper.MessageType.Warning, this
                );
            }

            if (_mode == GerstnerMode.Global && GetComponent<MeshRenderer>() != null)
            {
                showMessage
                (
                    "The MeshRenderer component will be ignored because the Mode is set to Global.",
                    ValidatedHelper.MessageType.Warning, this
                );

                isValid = false;
            }

            if (_spectrum == null)
            {
                showMessage
                (
                    "There is no spectrum assigned meaning this Gerstner component won't generate any waves.",
                    ValidatedHelper.MessageType.Warning, this
                );

                isValid = false;
            }

            if (_componentsPerOctave == 0)
            {
                showMessage
                (
                    "Components Per Octave set to 0 meaning this Gerstner component won't generate any waves.",
                    ValidatedHelper.MessageType.Warning, this
                );

                isValid = false;
            }

            return isValid;
        }
    }
#endif
}
