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
    /// Gerstner ocean waves.
    /// </summary>
    public partial class ShapeGerstner : MonoBehaviour, IFloatingOrigin
    {
        [Tooltip("The spectrum that defines the ocean surface shape. Assign asset of type Crest/Ocean Waves Spectrum.")]
        public OceanWaveSpectrum _spectrum;

        [Tooltip("Wind direction (angle from x axis in degrees)"), Range(-180, 180)]
        public float _windDirectionAngle = 0f;
        public Vector2 WindDir => new Vector2(Mathf.Cos(Mathf.PI * _windDirectionAngle / 180f), Mathf.Sin(Mathf.PI * _windDirectionAngle / 180f));

        public class GerstnerBatch : ILodDataInput
        {
            RenderTexture _waveBuffer;
            Material _material;

            public GerstnerBatch(float wavelength, RenderTexture waveBuffer, int waveBufferSliceIndex)
            {
                Wavelength = wavelength;

                _waveBuffer = waveBuffer;

                var combineShader = Shader.Find("Hidden/Crest/Inputs/Animated Waves/Gerstner Global");
                _material = new Material(combineShader);
                _material.SetTexture("_WaveBuffer", waveBuffer);
                _material.SetInt("_WaveBufferSliceIndex", waveBufferSliceIndex);
            }

            //public PropertyWrapperMaterial GetMaterial(int isTransition) => _materials[isTransition];

            // Two materials because as batch may be rendered twice if it has large wavelengths that are being transitioned back
            // and forth across the last 2 LODs.
            //PropertyWrapperMaterial[] _materials;

            // The ocean input system uses this to decide which lod this batch belongs in
            public float Wavelength { get; private set; }

            public bool Enabled { get => true; set { } }

            public void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
            {
                if (weight > 0f)
                {
                    buf.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
                    //Debug.Log($"{Time.frameCount}: Drawing {lodIdx}");

                    //PropertyWrapperMaterial mat = GetMaterial(isTransition);
                    //mat.SetFloat(RegisterLodDataInputBase.sp_Weight, weight);

                    // TODO draw full screen quad?
                    //buf.DrawRenderer(_rend, mat.material);
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
        //[Header("Wave data (usually populated at runtime)")]
        //public bool _evaluateSpectrumAtRuntime = true;
        //[PredicatedField("_evaluateSpectrumAtRuntime", true)]
        public float[] _wavelengths;
        //[PredicatedField("_evaluateSpectrumAtRuntime", true)]
        public float[] _amplitudes;
        //[PredicatedField("_evaluateSpectrumAtRuntime", true)]
        public float[] _angleDegs;
        //[PredicatedField("_evaluateSpectrumAtRuntime", true)]
        public float[] _phases;

        public int _resolution = 32;
        public RenderTexture _waveBuffers;

        struct GerstnerCascadeParams
        {
            public int _startIndex;
        }
        ComputeBuffer _bufCascadeParams;
        GerstnerCascadeParams[] _cascadeParams = new GerstnerCascadeParams[LodDataMgr.MAX_LOD_COUNT + 1];

        int _firstCascade = -1;
        int _lastCascade = -1;

        struct GerstnerWaveComponent4
        {
            public Vector4 _twoPiOverWavelength;
            public Vector4 _amp;
            public Vector4 _waveDirX;
            public Vector4 _waveDirZ;
            public Vector4 _omega;
            public Vector4 _phase;
            public Vector4 _chopAmp;
        }
        ComputeBuffer _bufWaveData;
        const int MAX_WAVE_COMPONENTS = 1024;
        GerstnerWaveComponent4[] _waveData = new GerstnerWaveComponent4[MAX_WAVE_COMPONENTS / 4];

        ComputeShader _shaderGerstner;
        int _krnlGerstner = -1;

        CommandBuffer _buf;

        readonly int sp_TextureRes = Shader.PropertyToID("_TextureRes");
        readonly int sp_AttenuationInShallows = Shader.PropertyToID("_AttenuationInShallows");
        readonly int sp_CascadeParams = Shader.PropertyToID("_GerstnerCascadeParams");
        readonly int sp_GerstnerWaveData = Shader.PropertyToID("_GerstnerWaveData");
        readonly int sp_WaveBuffer = Shader.PropertyToID("_WaveBuffer");

        GameObject _renderProxy;

        readonly float _twopi = 2f * Mathf.PI;
        readonly float _one_over_2pi = 1f / (2f * Mathf.PI);

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

        bool _firstUpdate = true;

        void Update()
        {
            if (_firstUpdate)
            {
                InitData();
            }

            bool doEveryFrame = true;
            if (_firstUpdate || doEveryFrame)
            {
                UpdateWaveData();

                InitBatches();
            }

            _firstUpdate = false;

            ReportMaxDisplacement();

            if (_firstCascade != -1 && _lastCascade != -1)
            {
                _buf.Clear();

                _buf.SetComputeFloatParam(_shaderGerstner, sp_TextureRes, _waveBuffers.width);
                _buf.SetComputeFloatParam(_shaderGerstner, OceanRenderer.sp_crestTime, OceanRenderer.Instance.CurrentTime);
                _buf.SetComputeBufferParam(_shaderGerstner, _krnlGerstner, sp_CascadeParams, _bufCascadeParams);
                _buf.SetComputeBufferParam(_shaderGerstner, _krnlGerstner, sp_GerstnerWaveData, _bufWaveData);
                _buf.SetComputeTextureParam(_shaderGerstner, _krnlGerstner, sp_WaveBuffer, _waveBuffers);

                _buf.DispatchCompute(_shaderGerstner, _krnlGerstner, _waveBuffers.width / 8, _waveBuffers.height / 8, _lastCascade - _firstCascade + 1);

                Graphics.ExecuteCommandBuffer(_buf);
            }
        }

        void SliceUpWaves()
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
                        _waveData[vi]._twoPiOverWavelength[ei] = 1f;
                        _waveData[vi]._amp[ei] = 0f;
                        _waveData[vi]._waveDirX[ei] = 0f;
                        _waveData[vi]._waveDirZ[ei] = 0f;
                        _waveData[vi]._omega[ei] = 0f;
                        _waveData[vi]._phase[ei] = 0f;
                        _waveData[vi]._chopAmp[ei] = 0f;
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

                    _waveData[vi]._twoPiOverWavelength[ei] = 2f * Mathf.PI / _wavelengths[componentIdx];
                    _waveData[vi]._amp[ei] = _amplitudes[componentIdx];

                    float chopScale = _spectrum._chopScales[(componentIdx) / _componentsPerOctave];
                    _waveData[vi]._chopAmp[ei] = -chopScale * _spectrum._chop * _amplitudes[componentIdx];

                    float angle = Mathf.Deg2Rad * (_windDirectionAngle + _angleDegs[componentIdx]);
                    _waveData[vi]._waveDirX[ei] = Mathf.Cos(angle);
                    _waveData[vi]._waveDirZ[ei] = Mathf.Sin(angle);

                    // It used to be this, but I'm pushing all the stuff that doesn't depend on position into the phase.
                    //half4 angle = k * (C * _CrestTime + x) + _Phases[vi];
                    float gravityScale = _spectrum._gravityScales[(componentIdx) / _componentsPerOctave];
                    float gravity = OceanRenderer.Instance.Gravity * _spectrum._gravityScale;
                    float C = Mathf.Sqrt(_wavelengths[componentIdx] * gravity * gravityScale * _one_over_2pi);
                    float k = _twopi / _wavelengths[componentIdx];
                    // Repeat every 2pi to keep angle bounded - helps precision on 16bit platforms
                    _waveData[vi]._omega[ei] = k * C;
                    _waveData[vi]._phase[ei] = Mathf.Repeat(_phases[componentIdx], Mathf.PI * 2f);

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
                    _waveData[vi]._chopAmp[ei] = 0f;
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

            SliceUpWaves();
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
            var registered = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrAnimWaves));

//#if UNITY_EDITOR
            // Unregister after switching modes in the editor.
            if (_batches != null)
            {
                foreach (var batch in _batches)
                {
                    registered.Remove(batch);
                }
            }
//#endif

            // Submit draws to create the Gerstner waves
            _batches = new GerstnerBatch[LodDataMgr.MAX_LOD_COUNT];
            for (int i = _firstCascade; i <= _lastCascade; i++)
            {
                if (i == -1) break;
                _batches[i] = new GerstnerBatch(MinWavelength(i), _waveBuffers, i);
                registered.Add(0, _batches[i]);
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

        void OnGUI()
        {
            OceanDebugGUI.DrawGerstner(this);
        }
    }

#if UNITY_EDITOR
    public partial class ShapeGerstner : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

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
