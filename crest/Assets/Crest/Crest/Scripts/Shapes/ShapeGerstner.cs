// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;
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
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "wave-conditions.html" + Internal.Constants.HELP_URL_RP + "#shapegerstner-preview")]
    public partial class ShapeGerstner : MonoBehaviour /*TODO, IFloatingOrigin*/
        , ISplinePointCustomDataSetup
#if UNITY_EDITOR
        , IReceiveSplinePointOnDrawGizmosSelectedMessages
#endif
    {
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
        [SerializeField, Predicated("_overrideSplineSettings"), Delayed]
        int _smoothingIterations = 0;

        [SerializeField]
        float _featherWaveStart = 0.1f;

        Mesh _meshForDrawingWaves;

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

        GerstnerBatch[] _batches = null;

        Material _matGenerateWaves;

        static readonly int sp_WaveBuffer = Shader.PropertyToID("_WaveBuffer");
        static readonly int sp_WaveBufferSliceIndex = Shader.PropertyToID("_WaveBufferSliceIndex");
        static readonly int sp_AverageWavelength = Shader.PropertyToID("_AverageWavelength");
        static readonly int sp_RespectShallowWaterAttenuation = Shader.PropertyToID("_RespectShallowWaterAttenuation");
        static readonly int sp_FeatherWaveStart = Shader.PropertyToID("_FeatherWaveStart");
        readonly int sp_AxisX = Shader.PropertyToID("_AxisX");

        internal static readonly CrestSortedList<int, ShapeGerstner> Instances = new CrestSortedList<int, ShapeGerstner>(new SiblingIndexComparer());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            Instances.Clear();
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

            if (_matGenerateWaves == null)
            {
                if (_meshForDrawingWaves == null)
                {
                    _matGenerateWaves = new Material(Shader.Find("Hidden/Crest/Inputs/Animated Waves/Gerstner Global"));
                }
                else
                {
                    _matGenerateWaves = new Material(Shader.Find("Crest/Inputs/Animated Waves/Gerstner Geometry"));
                }
            }

            var updateDataEachFrame = !_spectrumFixedAtRuntime;
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) updateDataEachFrame = true;
#endif

            _matGenerateWaves.SetFloat(sp_RespectShallowWaterAttenuation, _respectShallowWaterAttenuation);
            _matGenerateWaves.SetFloat(sp_FeatherWaveStart, _featherWaveStart);
            _matGenerateWaves.SetVector(sp_AxisX, PrimaryWaveDirection);

            // Calc wind speed in m/s
            var windSpeed = _overrideGlobalWindSpeed ? _windSpeed : OceanRenderer.Instance._globalWindSpeed;
            windSpeed /= 3.6f;

            //Debug.Log("GEN " + _spectrum.GetHashCode());

            var generator = WaveGenerator.GetGenerator(_resolution, _spectrum);
            var generatedData = generator.Generate(OceanRenderer.Instance.CurrentTime, windSpeed, updateDataEachFrame, _componentsPerOctave, buf, out var firstSlice, out var lastSlice);
            _matGenerateWaves.SetTexture(sp_WaveBuffer, generatedData);

            InitBatches(firstSlice, lastSlice);

            ReportMaxDisplacement();
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

        private void ReportMaxDisplacement()
        {
            // TODO
            //if (_activeSpectrum._chopScales.Length != OceanWaveSpectrum.NUM_OCTAVES)
            //{
            //    Debug.LogError($"OceanWaveSpectrum {_activeSpectrum.name} is out of date, please open this asset and resave in editor.", _activeSpectrum);
            //}

            //float ampSum = 0f;
            //for (int i = 0; i < _wavelengths.Length; i++)
            //{
            //    ampSum += _amplitudes[i] * _activeSpectrum._chopScales[i / _componentsPerOctave];
            //}
            //OceanRenderer.Instance.ReportMaxDisplacementFromShape(ampSum * _activeSpectrum._chop, ampSum, ampSum);
        }

        void InitBatches(int firstSlice, int lastSlice)
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
                var smooth = _overrideSplineSettings ? _smoothingIterations : splineForWaves.SmoothingIterations;

                if (ShapeGerstnerSplineHandling.GenerateMeshFromSpline<SplinePointDataGerstner>(splineForWaves, transform, subdivs, radius, smooth, Vector2.one, ref _meshForDrawingWaves))
                {
                    _meshForDrawingWaves.name = gameObject.name + "_mesh";
                }
            }

            // Submit draws to create the Gerstner waves
            _batches = new GerstnerBatch[CASCADE_COUNT];
            for (int i = firstSlice; i <= lastSlice; i++)
            {
                if (i == -1) break;
                _batches[i] = new GerstnerBatch(this, MinWavelength(i), i, _matGenerateWaves, _meshForDrawingWaves);
                registered.Add(0, _batches[i]);
            }
        }

        private void OnEnable()
        {
            Instances.Add(transform.GetSiblingIndex(), this);

            //_firstUpdate = true;

            // Initialise with spectrum
            if (_spectrum != null)
            {
                _activeSpectrum = _spectrum;
            }
#if UNITY_EDITOR
            if (_activeSpectrum == null)
            {
                _activeSpectrum = ScriptableObject.CreateInstance<OceanWaveSpectrum>();
                _activeSpectrum.name = "Default Waves (auto)";
            }

            if (EditorApplication.isPlaying && !Validate(OceanRenderer.Instance, ValidatedHelper.DebugLog))
            {
                enabled = false;
                return;
            }

            _activeSpectrum.Upgrade();
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
            // TODO
            //if (_debugDrawSlicesInEditor)
            //{
            //    OceanDebugGUI.DrawTextureArray(_waveBuffers, 8);
            //}
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

            return isValid;
        }
    }

    // Here for the help boxes
    [CustomEditor(typeof(ShapeGerstner))]
    public class ShapeGerstnerEditor : ValidatedEditor { }
#endif
}
