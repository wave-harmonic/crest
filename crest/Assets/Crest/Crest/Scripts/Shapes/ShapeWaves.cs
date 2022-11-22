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
    /// FFT ocean wave shape
    /// </summary>
    [ExecuteDuringEditMode(ExecuteDuringEditModeAttribute.Include.None)]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "wave-conditions.html" + Internal.Constants.HELP_URL_RP)]
    public abstract partial class ShapeWaves : CustomMonoBehaviour, LodDataMgrAnimWaves.IShapeUpdatable, ISplinePointCustomDataSetup
    {
        [Tooltip("The spectrum that defines the ocean surface shape. Assign asset of type Crest/Ocean Waves Spectrum."), Embedded]
        public OceanWaveSpectrum _spectrum;
        protected OceanWaveSpectrum _activeSpectrum = null;

        [Tooltip("When true, the wave spectrum is evaluated once on startup in editor play mode and standalone builds, rather than every frame. This is less flexible but reduces the performance cost significantly."), SerializeField]
        bool _spectrumFixedAtRuntime = true;

        [Tooltip("Primary wave direction heading (deg). This is the angle from x axis in degrees that the waves are oriented towards. If a spline is being used to place the waves, this angle is relative ot the spline."), Range(-180, 180)]
        public float _waveDirectionHeadingAngle = 0f;
        public Vector2 PrimaryWaveDirection => new Vector2(Mathf.Cos(Mathf.PI * _waveDirectionHeadingAngle / 180f), Mathf.Sin(Mathf.PI * _waveDirectionHeadingAngle / 180f));

        [Tooltip("When true, uses the wind speed on this component rather than the wind speed from the Ocean Renderer component.")]
        public bool _overrideGlobalWindSpeed = false;

        [Tooltip("Wind speed in km/h. Controls wave conditions."), Range(0, 150f, power: 2f), Predicated("_overrideGlobalWindSpeed")]
        public float _windSpeed = 20f;
        public float WindSpeed => (_overrideGlobalWindSpeed ? _windSpeed : OceanRenderer.Instance._globalWindSpeed) / 3.6f;

        [Tooltip("How much these waves respect the shallow water attenuation setting in the Animated Waves Settings. Set to 0 to ignore shallow water."), SerializeField, Range(0f, 1f)]
        public float _respectShallowWaterAttenuation = 1f;


        [Header("Input Settings")]

        [Tooltip("Multiplier for these waves to scale up/down."), Range(0f, 1f)]
        public float _weight = 1f;

        [Predicated(typeof(ShapeWaves), "IsLocalWaves"), DecoratedField]
        [Tooltip("How the waves are blended into the wave buffer. Use <i>Blend</i> to override waves.")]
        public ShapeBlendMode _blendMode = ShapeBlendMode.Additive;
        public ShapeBlendMode BlendMode => _meshForDrawingWaves ? _blendMode : ShapeBlendMode.Additive;
        public enum ShapeBlendMode
        {
            Additive,
            Blend,
        }

        [Predicated(typeof(ShapeWaves), "IsLocalWaves"), DecoratedField]
        [Tooltip("Order this input will render. Queue is <i>Queue + SiblingIndex</i>")]
        public int _queue = 0;


        [Header("Generation Settings")]

        [Tooltip("Resolution to use for wave generation buffers. Low resolutions are more efficient but can result in noticeable patterns in the shape."), Delayed]
        public int _resolution = 128;

        [Tooltip("In Editor, shows the wave generation buffers on screen."), SerializeField]
#pragma warning disable 414
        protected bool _debugDrawSlicesInEditor = false;
#pragma warning restore 414


        [Header("Spline Settings")]

        [SerializeField]
        bool _overrideSplineSettings = false;
        [SerializeField, Predicated("_overrideSplineSettings"), DecoratedField]
        float _radius = 50f;
        [SerializeField, Predicated("_overrideSplineSettings"), Delayed]
        int _subdivisions = 1;

        [SerializeField]
        float _featherWaveStart = 0.1f;

        protected Mesh _meshForDrawingWaves;

        static OceanWaveSpectrum s_DefaultSpectrum;
        protected static OceanWaveSpectrum DefaultSpectrum
        {
            get
            {
                if (s_DefaultSpectrum == null)
                {
                    s_DefaultSpectrum = ScriptableObject.CreateInstance<OceanWaveSpectrum>();
                    s_DefaultSpectrum.name = "Default Waves (auto)";
                }

                return s_DefaultSpectrum;
            }
        }

        protected abstract int MinimumResolution { get; }
        protected abstract int MaximumResolution { get; }

        public class WaveBatch : ILodDataInput
        {
            ShapeWaves _shapeWaves;

            Material _material;
            Mesh _mesh;

            int _waveBufferSliceIndex;

            public static Component _previousShapeComponent;
            public static int _previousLodIndex = -1;

            public WaveBatch(ShapeWaves shapeWaves, float wavelength, int waveBufferSliceIndex, Material material, Mesh mesh)
            {
                _shapeWaves = shapeWaves;
                Wavelength = wavelength;
                _waveBufferSliceIndex = waveBufferSliceIndex;
                _mesh = mesh;
                _material = material;
            }

            // The ocean input system uses this to decide which lod this batch belongs in
            public float Wavelength { get; private set; }

            public bool Enabled { get => true; set { } }

            public void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
            {
                var finalWeight = weight * _shapeWaves._weight;
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
                        // If the component has changed or the LOD slice then this is a new batch of cascades. We use this
                        // to add alpha blending without changing much of the architecture. It requires an extra pass which
                        // is not very lean performance wise. Use blend states when breaking change can be introduced.
                        if (_shapeWaves._blendMode == ShapeBlendMode.Blend && (_previousShapeComponent != _shapeWaves || _previousLodIndex != lodIdx))
                        {
                            _previousShapeComponent = _shapeWaves;
                            _previousLodIndex = lodIdx;
                            buf.DrawMesh(_mesh, _shapeWaves.transform.localToWorldMatrix, _material, submeshIndex: 0, shaderPass: 1);
                        }

                        buf.DrawMesh(_mesh, _shapeWaves.transform.localToWorldMatrix, _material, submeshIndex: 0, shaderPass: 0);
                    }
                }
            }
        }

        public const int CASCADE_COUNT = 16;

        WaveBatch[] _batches = null;

        // First cascade of wave buffer that has waves and will be rendered.
        protected int _firstCascade = -1;
        // Last cascade of wave buffer that has waves and will be rendered.
        protected int _lastCascade = -1;

        // Used to populate data on first frame.
        protected bool _firstUpdate = true;

        // Active material.
        protected Material _matGenerateWaves;
        // Cache material options.
        Material _matGenerateWavesGlobal;
        Material _matGenerateWavesGeometry;

        MeshRenderer _renderer;
        Spline.Spline _spline;

        protected static readonly int sp_WaveBuffer = Shader.PropertyToID("_WaveBuffer");
        protected static readonly int sp_WaveBufferSliceIndex = Shader.PropertyToID("_WaveBufferSliceIndex");
        protected static readonly int sp_AverageWavelength = Shader.PropertyToID("_AverageWavelength");
        protected static readonly int sp_RespectShallowWaterAttenuation = Shader.PropertyToID("_RespectShallowWaterAttenuation");
        protected static readonly int sp_MaximumAttenuationDepth = Shader.PropertyToID("_MaximumAttenuationDepth");
        protected static readonly int sp_FeatherWaveStart = Shader.PropertyToID("_FeatherWaveStart");
        protected static readonly int sp_AxisX = Shader.PropertyToID("_AxisX");

        static int s_InstanceCount = 0;

        protected bool UpdateDataEachFrame
        {
            get
            {
                var updateDataEachFrame = !_spectrumFixedAtRuntime;
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying) updateDataEachFrame = true;
#endif
                return updateDataEachFrame;
            }
        }

        /// <summary>
        /// Min wavelength for a cascade in the wave buffer. Does not depend on viewpoint.
        /// </summary>
        public abstract float MinWavelength(int cascadeIdx);
        protected abstract void ReportMaxDisplacement();
        protected abstract void DestroySharedResources();

        public virtual void CrestUpdate(CommandBuffer buffer)
        {
#if UNITY_EDITOR
            UpdateEditorOnly();
#endif

            // Ensure batches assigned to correct slots.
            if (_firstUpdate || UpdateDataEachFrame)
            {
                InitBatches();
                _firstUpdate = false;
            }

            _matGenerateWaves.SetFloat(sp_RespectShallowWaterAttenuation, _respectShallowWaterAttenuation);
            _matGenerateWaves.SetFloat(sp_MaximumAttenuationDepth, OceanRenderer.Instance._lodDataAnimWaves.Settings.MaximumAttenuationDepth);
            _matGenerateWaves.SetFloat(sp_FeatherWaveStart, _featherWaveStart);

            ReportMaxDisplacement();
        }

        void CreateOrUpdateSplineMesh()
        {
            if (TryGetComponent(out _spline))
            {
                var radius = _overrideSplineSettings ? _radius : _spline.Radius;
                var subdivs = _overrideSplineSettings ? _subdivisions : _spline.Subdivisions;
                if (ShapeGerstnerSplineHandling.GenerateMeshFromSpline<SplinePointDataWaves>(_spline, transform,
                    subdivs, radius, Vector2.one, ref _meshForDrawingWaves, out _, out _))
                {
                    _meshForDrawingWaves.name = gameObject.name + "_mesh";
                }
            }
        }

        void InitBatches()
        {
            if (_batches != null)
            {
                foreach (var batch in _batches)
                {
                    RegisterLodDataInput<LodDataMgrAnimWaves>.DeregisterInput(batch);
                }
            }

            if (_firstUpdate)
            {
                CreateOrUpdateSplineMesh();

                if (!_spline && TryGetComponent(out _renderer) && TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh)
                {
                    _meshForDrawingWaves = meshFilter.sharedMesh;
                }
                else
                {
                    _renderer = null;
                }
            }

            // Queue determines draw order of this input. Global waves should be rendered first. They are additive
            // so not order dependent.
            var queue = int.MinValue;
            var subQueue = transform.GetSiblingIndex();

            if (_spline)
            {
                if (_matGenerateWavesGeometry == null)
                {
                    _matGenerateWavesGeometry = new Material(Shader.Find("Crest/Inputs/Animated Waves/Gerstner Geometry"));
                }

                _matGenerateWaves = _matGenerateWavesGeometry;
                queue = _queue;
            }
            else if (_renderer)
            {
                _matGenerateWaves = _renderer.sharedMaterial;
                queue = _queue;
            }
            else
            {
                if (_matGenerateWavesGlobal == null)
                {
                    _matGenerateWavesGlobal = new Material(Shader.Find("Hidden/Crest/Inputs/Animated Waves/Gerstner Global"));
                }

                _matGenerateWaves = _matGenerateWavesGlobal;
            }

            // Submit draws to create the FFT waves
            _batches = new WaveBatch[CASCADE_COUNT];
            for (int i = _firstCascade; i <= _lastCascade; i++)
            {
                if (i == -1) break;
                _batches[i] = new WaveBatch(this, MinWavelength(i), i, _matGenerateWaves, _meshForDrawingWaves);
                RegisterLodDataInput<LodDataMgrAnimWaves>.RegisterInput(_batches[i], queue, subQueue);
            }
        }

        void Awake()
        {
            s_InstanceCount++;
        }

        void OnDestroy()
        {
            // Since FFTCompute resources are shared we will clear after last ShapeFFT is destroyed.
            if (--s_InstanceCount <= 0)
            {
                DestroySharedResources();

                if (s_DefaultSpectrum != null)
                {
                    Helpers.Destroy(s_DefaultSpectrum);
                }
            }
        }

        protected virtual void OnEnable()
        {
            _firstUpdate = true;

            // Initialise with spectrum
            if (_spectrum != null)
            {
                _activeSpectrum = _spectrum;
            }

            if (_activeSpectrum == null)
            {
                _activeSpectrum = DefaultSpectrum;
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

        protected virtual void OnDisable()
        {
            LodDataMgrAnimWaves.DeregisterUpdatable(this);

            if (_batches != null)
            {
                foreach (var batch in _batches)
                {
                    RegisterLodDataInput<LodDataMgrAnimWaves>.DeregisterInput(batch);
                }

                _batches = null;
            }
        }

        public bool AttachDataToSplinePoint(GameObject splinePoint)
        {
            if (splinePoint.TryGetComponent(out SplinePointDataWaves _))
            {
                // Already existing, nothing to do
                return false;
            }

            splinePoint.AddComponent<SplinePointDataWaves>();
            return true;
        }
    }

#if UNITY_EDITOR
    // Editor
    public partial class ShapeWaves : IReceiveSplinePointOnDrawGizmosSelectedMessages
    {
        void UpdateEditorOnly()
        {
            if (_spectrum != null)
            {
                _activeSpectrum = _spectrum;
            }

            if (_activeSpectrum == null)
            {
                _activeSpectrum = DefaultSpectrum;
            }

            // Unassign mesh
            if (_meshForDrawingWaves != null && !TryGetComponent<Spline.Spline>(out _) && !TryGetComponent<MeshRenderer>(out _))
            {
                _meshForDrawingWaves = null;
            }
        }

        // Called by Predicated attribute. Signature must not be changed.
        bool IsLocalWaves(Component component)
        {
            return TryGetComponent<MeshRenderer>(out _) || TryGetComponent<Spline.Spline>(out _);
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            _resolution = Mathf.ClosestPowerOfTwo(_resolution);
            _resolution = Mathf.Clamp(_resolution, MinimumResolution, MaximumResolution);
        }

        private void OnDrawGizmosSelected()
        {
            // Restrict this call as it is costly.
            if (Selection.activeGameObject == gameObject)
            {
                CreateOrUpdateSplineMesh();
            }

            DrawMesh();
        }

        public void OnSplinePointDrawGizmosSelected(SplinePoint point)
        {
            CreateOrUpdateSplineMesh();
            OnDrawGizmosSelected();
        }

        void DrawMesh()
        {
            if (_meshForDrawingWaves != null)
            {
                Gizmos.color = RegisterAnimWavesInput.s_gizmoColor;
                Gizmos.DrawWireMesh(_meshForDrawingWaves, 0, transform.position, transform.rotation, transform.lossyScale);
            }
        }
    }

    // Validation.
    public partial class ShapeWaves : IValidated
    {
        public virtual bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
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

            if (BlendMode == ShapeBlendMode.Blend && _matGenerateWaves && _matGenerateWaves.shader.passCount < 2)
            {
                showMessage
                (
                    "Using Blend requires two shader passes - the second pass reduces the waves to implement alpha blending.",
                    "Add a second pass to the shader. See AnimWavesGerstnerGeometry.shader",
                    ValidatedHelper.MessageType.Error, this
                );
            }

            return isValid;
        }
    }
#endif
}
