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
    public partial class ShapeGerstner : MonoBehaviour, IFloatingOrigin
#if UNITY_EDITOR
        , IReceiveSplinePointOnDrawGizmosSelectedMessages
#endif
    {
        [Header("Wave Settings")]
        [Tooltip("The spectrum that defines the ocean surface shape. Assign asset of type Crest/Ocean Waves Spectrum.")]
        public OceanWaveSpectrum _spectrum;

        [Tooltip("If a spectrum will not change at runtime, set this true to calculate the wave data once on first update rather than each frame."), SerializeField]
        bool _spectrumIsStatic = true;

        [Tooltip("Wind direction (angle from x axis in degrees)"), Range(-180, 180)]
        public float _windDirectionAngle = 0f;
        public Vector2 WindDir => new Vector2(Mathf.Cos(Mathf.PI * _windDirectionAngle / 180f), Mathf.Sin(Mathf.PI * _windDirectionAngle / 180f));

        [Range(0f, 1f)]
        public float _weight = 1f;

        [Header("Generation Settings")]
        [Delayed, Tooltip("How many wave components to generate in each octave.")]
        public int _componentsPerOctave = 8;

        public int _randomSeed = 0;

        [Delayed]
        public int _resolution = 32;

        [SerializeField]
        bool _debugDrawSlicesInEditor = false;

        [Header("Spline Settings")]
        [SerializeField, Delayed]
        int _subdivisions = 1;

        [SerializeField]
        float _radius = 50f;

        [SerializeField, Delayed]
        int _smoothingIterations = 60;

        Mesh _meshForDrawingWaves;

        public class GerstnerBatch : ILodDataInput
        {
            Material _material;
            Mesh _mesh;

            RenderTexture _waveBuffer;
            int _waveBufferSliceIndex;
            public Matrix4x4 _matrix;

            public GerstnerBatch(float wavelength, RenderTexture waveBuffer, int waveBufferSliceIndex, Material material, Mesh mesh, Matrix4x4 matrix)
            {
                Wavelength = wavelength;
                _waveBuffer = waveBuffer;
                _waveBufferSliceIndex = waveBufferSliceIndex;
                _mesh = mesh;
                _material = material;
                _matrix = matrix;
            }

            // The ocean input system uses this to decide which lod this batch belongs in
            public float Wavelength { get; private set; }

            public bool Enabled { get => true; set { } }

            public float Weight { get; set; }

            public void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
            {
                if (weight > 0f)
                {
                    buf.SetGlobalInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                    buf.SetGlobalFloat(RegisterLodDataInputBase.sp_Weight, Weight * weight);
                    buf.SetGlobalTexture(sp_WaveBuffer, _waveBuffer);
                    buf.SetGlobalInt(sp_WaveBufferSliceIndex, _waveBufferSliceIndex);
                    buf.SetGlobalFloat(sp_AverageWavelength, Wavelength * 1.5f);

                    // Either use a full screen quad, or a provided mesh renderer to draw the waves
                    if (_mesh == null)
                    {
                        buf.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
                    }
                    else if (_material != null)
                    {
                        buf.DrawMesh(_mesh, _matrix, _material);
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
        float[] _powers;
        float[] _angleDegs;
        float[] _phases;

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
        GerstnerWaveComponent4[] _waveData = new GerstnerWaveComponent4[MAX_WAVE_COMPONENTS / 4];

        ComputeShader _shaderGerstner;
        int _krnlGerstner = -1;

        readonly int sp_FirstCascadeIndex = Shader.PropertyToID("_FirstCascadeIndex");
        readonly int sp_TextureRes = Shader.PropertyToID("_TextureRes");
        readonly int sp_CascadeParams = Shader.PropertyToID("_GerstnerCascadeParams");
        readonly int sp_GerstnerWaveData = Shader.PropertyToID("_GerstnerWaveData");
        static readonly int sp_WaveBuffer = Shader.PropertyToID("_WaveBuffer");
        static readonly int sp_WaveBufferSliceIndex = Shader.PropertyToID("_WaveBufferSliceIndex");
        static readonly int sp_AverageWavelength = Shader.PropertyToID("_AverageWavelength");
        readonly int sp_AxisX = Shader.PropertyToID("_AxisX");

        readonly float _twoPi = 2f * Mathf.PI;
        readonly float _recipTwoPi = 1f / (2f * Mathf.PI);

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
            return texelSize * OceanRenderer.Instance.MinTexelsPerWave;
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

            var updateDataEachFrame = !_spectrumIsStatic;
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) updateDataEachFrame = true;
#endif
            if (_firstUpdate || updateDataEachFrame)
            {
                UpdateWaveData();

                InitBatches();

                _firstUpdate = false;
            }

            // Set weights - this should always happen
            foreach (var batch in _batches)
            {
                if (batch != null)
                {
                    batch.Weight = _weight;
                    batch._matrix = transform.localToWorldMatrix;
                }
            }

            ReportMaxDisplacement();

            // If some cascades have waves in them, generate
            if (_firstCascade != -1 && _lastCascade != -1)
            {
                UpdateGenerateWaves(buf);
            }

            buf.SetGlobalVector(sp_AxisX, WindDir);
        }

#if UNITY_EDITOR
        void UpdateEditorOnly()
        {
            if (_spectrum == null)
            {
                _spectrum = ScriptableObject.CreateInstance<OceanWaveSpectrum>();
                _spectrum.name = "Default Waves (auto)";
            }

            // Unassign mesh
            if (_meshForDrawingWaves != null && GetComponent<Spline.Spline>() == null)
            {
                _meshForDrawingWaves = null;
            }
        }
#endif

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
                if (cascadeIdx == CASCADE_COUNT) break;

                {
                    // Pack into vector elements
                    int vi = outputIdx / 4;
                    int ei = outputIdx - vi * 4;

                    _waveData[vi]._twoPiOverWavelength[ei] = 2f * Mathf.PI / _wavelengths[componentIdx];
                    _waveData[vi]._amp[ei] = _amplitudes[componentIdx];

                    float chopScale = _spectrum._chopScales[componentIdx / _componentsPerOctave];
                    _waveData[vi]._chopAmp[ei] = -chopScale * _spectrum._chop * _amplitudes[componentIdx];

                    float angle = Mathf.Deg2Rad * _angleDegs[componentIdx];
                    float dx = Mathf.Cos(angle);
                    float dz = Mathf.Sin(angle);

                    // It used to be this, but I'm pushing all the stuff that doesn't depend on position into the phase.
                    //half4 angle = k * (C * _CrestTime + x) + _Phases[vi];
                    float gravityScale = _spectrum._gravityScales[(componentIdx) / _componentsPerOctave];
                    float gravity = OceanRenderer.Instance.Gravity * _spectrum._gravityScale;
                    float C = Mathf.Sqrt(_wavelengths[componentIdx] * gravity * gravityScale * _recipTwoPi);
                    float k = _twoPi / _wavelengths[componentIdx];

                    // Constrain wave vector (wavelength and wave direction) to ensure wave tiles across domain
                    {
                        float kx = k * dx;
                        float kz = k * dz;
                        var diameter = 0.5f * (1 << cascadeIdx);
                        float n = kx / (2f * Mathf.PI / diameter);
                        float m = kz / (2f * Mathf.PI / diameter);
                        kx = 2f * Mathf.PI * Mathf.Round(n) / diameter;
                        kz = 2f * Mathf.PI * Mathf.Round(m) / diameter;

                        k = Mathf.Sqrt(kx * kx + kz * kz);
                        dx = kx / k;
                        dz = kz / k;
                    }

                    _waveData[vi]._waveDirX[ei] = dx;
                    _waveData[vi]._waveDirZ[ei] = dz;

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
                var amp = _spectrum.GetAmplitude(wl, 1f, out _);
                var chop = _spectrum._chopScales[octaveIndex];
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
            if (_powers == null || _powers.Length != _wavelengths.Length)
            {
                _powers = new float[_wavelengths.Length];
            }

            for (int i = 0; i < _wavelengths.Length; i++)
            {
                _amplitudes[i] = Random.value * _weight * _spectrum.GetAmplitude(_wavelengths[i], _componentsPerOctave, out _powers[i]);
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

            var splineForWaves = GetComponent<Spline.Spline>();
            if (splineForWaves != null)
            {
                if (GenerateMeshFromSpline(splineForWaves, transform, _subdivisions, _radius, _smoothingIterations, ref _meshForDrawingWaves))
                {
                    _meshForDrawingWaves.name = gameObject.name + "_mesh";
                }
            }

            Material mat;
            if (_meshForDrawingWaves == null)
            {
                mat = new Material(Shader.Find("Hidden/Crest/Inputs/Animated Waves/Gerstner Global"));
            }
            else
            {
                mat = new Material(Shader.Find("Crest/Inputs/Animated Waves/Gerstner Geometry"));
            }

            // Submit draws to create the Gerstner waves
            _batches = new GerstnerBatch[CASCADE_COUNT];
            for (int i = _firstCascade; i <= _lastCascade; i++)
            {
                if (i == -1) break;
                _batches[i] = new GerstnerBatch(MinWavelength(i), _waveBuffers, i, mat, _meshForDrawingWaves, transform.localToWorldMatrix);
                registered.Add(0, _batches[i]);
            }
        }

        static Vector3 TangentAfter(SplinePoint[] splinePoints, int idx)
        {
            var tangent = Vector3.zero;
            var wt = 0f;
            //var idx = i / 3;
            if (idx - 1 >= 0)
            {
                tangent += splinePoints[idx].transform.position - splinePoints[idx - 1].transform.position;
                wt += 1f;
            }
            if (idx + 1 < splinePoints.Length)
            {
                tangent += splinePoints[idx + 1].transform.position - splinePoints[idx].transform.position;
                wt += 1f;
            }
            return tangent / wt;
        }

        static Vector3 TangentBefore(SplinePoint[] splinePoints, int idx)
        {
            var tangent = Vector3.zero;
            var wt = 0f;
            if (idx - 1 >= 0)
            {
                tangent += splinePoints[idx].transform.position - splinePoints[idx - 1].transform.position;
                wt += 1f;
            }
            if (idx + 1 < splinePoints.Length)
            {
                tangent += splinePoints[idx + 1].transform.position - splinePoints[idx].transform.position;
                wt += 1f;
            }
            return tangent / wt;
        }

        static bool GenerateMeshFromSpline(Spline.Spline spline, Transform transform, int subdivisions, float radius, int smoothingIterations, ref Mesh mesh)
        {
            var splinePoints = spline.SplinePoints;
            if (splinePoints.Length < 2) return false;

            var points = new Vector3[(splinePoints.Length - 1) * 3 + 1];
            for (int i = 0; i < points.Length; i++)
            {
                float tm = 0.39f;

                if (i % 3 == 0)
                {
                    points[i] = splinePoints[i / 3].transform.position;
                }
                else if (i % 3 == 1)
                {
                    var idx = i / 3;
                    var tangent = TangentAfter(splinePoints, idx);
                    tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                    points[i] = splinePoints[idx].transform.position + tm * tangent;

                    if (i == 1)
                    {
                        tangent = TangentBefore(splinePoints, idx + 1);
                        // Mirror first tangent
                        var toNext = (splinePoints[idx + 1].transform.position - splinePoints[idx].transform.position).normalized;
                        var nearestPoint = Vector3.Dot(tangent, toNext) * toNext;
                        tangent += 2f * (nearestPoint - tangent);
                        tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                        points[i] = splinePoints[idx].transform.position + tm * tangent;
                    }
                }
                else
                {
                    var idx = i / 3 + 1;
                    var tangent = TangentBefore(splinePoints, idx);
                    tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                    points[i] = splinePoints[idx].transform.position - tm * tangent;

                    if (i == points.Length - 2)
                    {
                        tangent = TangentAfter(splinePoints, idx - 1);
                        // Mirror first tangent
                        var toNext = (splinePoints[idx - 1].transform.position - splinePoints[idx].transform.position).normalized;
                        var nearestPoint = Vector3.Dot(tangent, toNext) * toNext;
                        tangent += 2f * (nearestPoint - tangent);
                        tangent = tangent.normalized * (splinePoints[i / 3 + 1].transform.position - splinePoints[i / 3].transform.position).magnitude;
                        points[i] = splinePoints[idx].transform.position - tm * tangent;
                    }
                }
            }

            if (splinePoints.Length > 1)
            {
                float lengthEst = 0f;
                for (int i = 1; i < splinePoints.Length; i++)
                {
                    lengthEst += (splinePoints[i].transform.position - splinePoints[i - 1].transform.position).magnitude;
                }
                lengthEst = Mathf.Max(lengthEst, 1f);

                float spacing = 16f / Mathf.Pow(2f, subdivisions + 2);
                int pointCount = Mathf.CeilToInt(lengthEst / spacing);
                pointCount = Mathf.Max(pointCount, 1);

                var resultPts0 = new Vector3[pointCount];

                resultPts0[0] = points[0];
                for (int i = 1; i < pointCount; i++)
                {
                    float t = i / (float)(pointCount - 1);

                    var tpts = t * (splinePoints.Length - 1);
                    var spidx = Mathf.FloorToInt(tpts);
                    var alpha = tpts - spidx;
                    if (spidx == splinePoints.Length - 1)
                    {
                        spidx -= 1;
                        alpha = 1f;
                    }
                    var pidx = spidx * 3;

                    resultPts0[i] = (1 - alpha) * (1 - alpha) * (1 - alpha) * points[pidx] + 3 * alpha * (1 - alpha) * (1 - alpha) * points[pidx + 1] + 3 * alpha * alpha * (1 - alpha) * points[pidx + 2] + alpha * alpha * alpha * points[pidx + 3];
                }

                var resultPts1 = new Vector3[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    var tangent = resultPts0[Mathf.Min(pointCount - 1, i + 1)] - resultPts0[Mathf.Max(0, i - 1)];
                    var normal = tangent;
                    normal.x = tangent.z;
                    normal.z = -tangent.x;
                    normal = normal.normalized;
                    resultPts1[i] = resultPts0[i] + normal * radius;
                }

                var resultPtsTmp = new Vector3[pointCount];
                for (int j = 0; j < smoothingIterations; j++)
                {
                    resultPtsTmp[0] = resultPts1[0];
                    resultPtsTmp[pointCount - 1] = resultPts1[pointCount - 1];
                    for (int i = 1; i < pointCount - 1; i++)
                    {
                        resultPtsTmp[i] = (resultPts1[i] + resultPts1[i + 1] + resultPts1[i - 1]) / 3f;
                        resultPtsTmp[i] = resultPts0[i] + (resultPtsTmp[i] - resultPts0[i]).normalized * radius;
                    }
                    var tmp = resultPts1;
                    resultPts1 = resultPtsTmp;
                    resultPtsTmp = tmp;
                }

                return UpdateMesh(transform, resultPts0, resultPts1, ref mesh);
            }

            return false;
        }

        static bool UpdateMesh(Transform transform, Vector3[] resultPts0, Vector3[] resultPts1, ref Mesh mesh)
        {
            if (mesh == null)
            {
                mesh = new Mesh();
            }

            var splineLength = 0f;
            for (int i = 1; i < resultPts0.Length; i++)
            {
                splineLength += (resultPts0[i] - resultPts0[i - 1]).magnitude;
            }

            //           \
            //   \   ___--4 uvs1 _-
            //    4--      \
            //     \        \
            //  sp1 3--------3
            //      |        |
            //      2--------2
            //      |        |
            //      1--------1
            //      |        |
            //  sp0 0--------0 uvs1 __
            //      ^        ^
            //     RP0s     RP1s
            //
            var triCount = (resultPts0.Length - 1) * 2;
            var verts = new Vector3[triCount + 2];
            var uvs = new Vector2[triCount + 2];
            var uvs2 = new Vector2[triCount + 2];
            var indices = new int[triCount * 6];
            var distSoFar = 0f;
            for (var i0 = 0; i0 < resultPts0.Length - 1; i0 += 1)
            {
                // Vert indices:
                //
                //     2i1------2i1+1
                //      |\       |
                //      |  \     |
                //      |    \   |
                //      |      \ |
                //     2i0------2i0+1
                //      |        |
                //    sp0--------*
                //
                var i1 = i0 + 1;

                verts[2 * i0] = transform.InverseTransformPoint(resultPts0[i0]);
                verts[2 * i0 + 1] = transform.InverseTransformPoint(resultPts1[i0]);
                verts[2 * i1] = transform.InverseTransformPoint(resultPts0[i1]);
                verts[2 * i1 + 1] = transform.InverseTransformPoint(resultPts1[i1]);

                var axis0 = -new Vector2(resultPts1[i0].x - resultPts0[i0].x, resultPts1[i0].z - resultPts0[i0].z).normalized;
                var axis1 = -new Vector2(resultPts1[i1].x - resultPts0[i1].x, resultPts1[i1].z - resultPts0[i1].z).normalized;
                uvs[2 * i0] = axis0;
                uvs[2 * i0 + 1] = axis0;
                uvs[2 * i1] = axis1;
                uvs[2 * i1 + 1] = axis1;

                // uvs2.x - Dist to closest spline end
                // uvs2.y - 1-0 inverted normalized dist from shoreline
                var nextDistSoFar = distSoFar + (resultPts0[i0 + 1] - resultPts0[i0]).magnitude;
                uvs2[2 * i0].x = uvs2[2 * i0 + 1].x = Mathf.Min(distSoFar, splineLength - distSoFar);
                uvs2[2 * i1].x = uvs2[2 * i1 + 1].x = Mathf.Min(nextDistSoFar, splineLength - nextDistSoFar);
                uvs2[2 * i0].y = uvs[2 * i1].y = 1f;
                uvs2[2 * i0 + 1].y = uvs[2 * i1 + 1].y = 0f;

                indices[i0 * 6] = 2 * i0;
                indices[i0 * 6 + 1] = 2 * i1;
                indices[i0 * 6 + 2] = 2 * i0 + 1;

                indices[i0 * 6 + 3] = 2 * i1;
                indices[i0 * 6 + 4] = 2 * i1 + 1;
                indices[i0 * 6 + 5] = 2 * i0 + 1;

                distSoFar = nextDistSoFar;
            }

            mesh.SetIndices(new int[] { }, MeshTopology.Triangles, 0);
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.uv2 = uvs2;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();

            return true;
        }

        private void OnEnable()
        {
            _firstUpdate = true;

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

            LodDataMgrAnimWaves.RegisterUpdatable(this);
        }

        void OnDisable()
        {
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
    }

#if UNITY_EDITOR
    public partial class ShapeGerstner : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            if (_componentsPerOctave == 0)
            {
                showMessage
                (
                    "Components Per Octave set to 0 meaning this Gerstner component won't generate any waves.",
                    ValidatedHelper.MessageType.Warning, this
                );

                isValid = false;
            }

            if (GetComponent<Spline.Spline>() == null)
            {
                showMessage
                (
                    "These waves will be applied everywhere in the world. To limit the area, attach a Spline component.",
                    ValidatedHelper.MessageType.Info, this
                );
            }
            else
            {
                if (_meshForDrawingWaves == null)
                {
                    showMessage
                    (
                        "There is an issue with the attached Spline component and no waves are rendered. Please check this component in the Inspector for issues.",
                        ValidatedHelper.MessageType.Error, this
                    );
                }
            }

            return isValid;
        }
    }

    [CustomEditor(typeof(ShapeGerstner))]
    public class ShapeGerstnerEditor : ValidatedEditor
    {
    }
#endif
}
