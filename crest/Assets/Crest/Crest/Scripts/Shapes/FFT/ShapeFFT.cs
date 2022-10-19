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
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Shape FFT")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "wave-conditions.html" + Internal.Constants.HELP_URL_RP)]
    public partial class ShapeFFT : CustomMonoBehaviour, LodDataMgrAnimWaves.IShapeUpdatable
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

        [Header("Wave Conditions")]
        [Tooltip("Impacts how aligned waves are with wind.")]
        [Range(0, 1)]
        public float _windTurbulence = 0.145f;

        [Tooltip("The spectrum that defines the ocean surface shape. Assign asset of type Crest/Ocean Waves Spectrum."), Embedded]
        public OceanWaveSpectrum _spectrum;
        OceanWaveSpectrum _activeSpectrum = null;

        [Tooltip("When true, the wave spectrum is evaluated once on startup in editor play mode and standalone builds, rather than every frame. This is less flexible but reduces the performance cost significantly."), SerializeField]
        bool _spectrumFixedAtRuntime = true;

        [Tooltip("Primary wave direction heading (deg). This is the angle from x axis in degrees that the waves are oriented towards. If a spline is being used to place the waves, this angle is relative ot the spline."), Range(-180, 180)]
        public float _waveDirectionHeadingAngle = 0f;
        public float WindDirRadForFFT => _meshForDrawingWaves != null ? 0f : _waveDirectionHeadingAngle * Mathf.Deg2Rad;
        public Vector2 PrimaryWaveDirection => new Vector2(Mathf.Cos(Mathf.PI * _waveDirectionHeadingAngle / 180f), Mathf.Sin(Mathf.PI * _waveDirectionHeadingAngle / 180f));

        [Tooltip("When true, uses the wind speed on this component rather than the wind speed from the Ocean Renderer component.")]
        public bool _overrideGlobalWindSpeed = false;
        [Tooltip("Wind speed in km/h. Controls wave conditions."), Range(0, 150f, 2f), Predicated("_overrideGlobalWindSpeed")]
        public float _windSpeed = 20f;
        public float WindSpeedForFFT => (_overrideGlobalWindSpeed ? _windSpeed : OceanRenderer.Instance._globalWindSpeed) / 3.6f;

        [Tooltip("Multiplier for these waves to scale up/down."), Range(0f, 1f)]
        public float _weight = 1f;

        public enum BlendMode
        {
            Additive,
            Blend,
        }

        [Predicated(typeof(ShapeFFT), "IsLocalWaves"), DecoratedField]
        [Tooltip("How the waves are blended into the wave buffer. Use <i>Blend</i> to override waves.")]
        public BlendMode _blendMode = BlendMode.Additive;

        [Predicated(typeof(ShapeFFT), "IsLocalWaves"), DecoratedField]
        [Tooltip("Order this input will render. Queue is <i>Queue + SiblingIndex</i>")]
        public int _queue = 0;

        [Tooltip("How much these waves respect the shallow water attenuation setting in the Animated Waves Settings. Set to 0 to ignore shallow water."), SerializeField, Range(0f, 1f)]
        public float _respectShallowWaterAttenuation = 1f;

        [HideInInspector]
        [Delayed, Tooltip("How many wave components to generate in each octave.")]
        public int _componentsPerOctave = 8;

        [HideInInspector]
        [Tooltip("Change to get a different set of waves.")]
        public int _randomSeed = 0;

        [Header("Generation Settings")]
        [Tooltip("Resolution to use for wave generation buffers. Low resolutions are more efficient but can result in noticeable patterns in the shape."), Delayed]
        public int _resolution = 128;

        [Tooltip("In Editor, shows the wave generation buffers on screen."), SerializeField]
#pragma warning disable 414
        bool _debugDrawSlicesInEditor = false;
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

        [Header("Culling")]
        [Tooltip("Maximum amount surface will be displaced vertically from sea level. Increase this if gaps appear at bottom of screen."), SerializeField]
        float _maxVerticalDisplacement = 10f;
        [Tooltip("Maximum amount a point on the surface will be displaced horizontally by waves from its rest position. Increase this if gaps appear at sides of screen."), SerializeField]
        float _maxHorizontalDisplacement = 15f;

        [Header("Collision Data Baking")]
        [Tooltip("Enable running this FFT with baked data. This makes the FFT periodic (repeating in time).")]
        public bool _enableBakedCollision = false;
        [Tooltip("Frames per second of baked data. Larger values may help the collision track the surface closely at the cost of more frames and increase baked data size."), DecoratedField, Predicated("_enableBakedCollision")]
        public int _timeResolution = 4;
        [Tooltip("Smallest wavelength required in collision. To preview the effect of this, disable power sliders in spectrum for smaller values than this number. Smaller values require more resolution and increase baked data size."), DecoratedField, Predicated("_enableBakedCollision")]
        public float _smallestWavelengthRequired = 2f;
        [Tooltip("FFT waves will loop with a period of this many seconds. Smaller values decrease data size but can make waves visibly repetitive."), Predicated("_enableBakedCollision"), Range(4f, 128f)]
        public float _timeLoopLength = 32f;

        internal float LoopPeriod => _enableBakedCollision ? _timeLoopLength : -1f;

        Mesh _meshForDrawingWaves;

        float _windTurbulenceOld;
        float _windSpeedOld;
        float _windDirRadOld;
        OceanWaveSpectrum _spectrumOld;

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

        public class FFTBatch : ILodDataInput
        {
            ShapeFFT _shapeFFT;

            Material _material;
            Mesh _mesh;

            int _waveBufferSliceIndex;

            public static Component _previousShapeComponent;
            public static int _previousLodIndex = -1;

            public FFTBatch(ShapeFFT shapeFFT, float wavelength, int waveBufferSliceIndex, Material material, Mesh mesh)
            {
                _shapeFFT = shapeFFT;
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
                var finalWeight = weight * _shapeFFT._weight;
                if (finalWeight > 0f)
                {
                    buf.SetGlobalInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                    buf.SetGlobalFloat(RegisterLodDataInputBase.sp_Weight, finalWeight);
                    buf.SetGlobalInt(sp_WaveBufferSliceIndex, _waveBufferSliceIndex);
                    buf.SetGlobalFloat(sp_AverageWavelength, Wavelength * 1.5f);

                    // If the component has changed or the LOD slice then this is a new batch of cascades. We use this
                    // to add alpha blending without changing much of the architecture. It requires an extra pass which
                    // is not very lean performance wise. Use blend states when breaking change can be introduced.
                    var isBlending = false;
                    if (_shapeFFT._blendMode == BlendMode.Blend && (_previousShapeComponent != _shapeFFT || _previousLodIndex != lodIdx))
                    {
                        isBlending = true;
                        _previousShapeComponent = _shapeFFT;
                        _previousLodIndex = lodIdx;
                    }

                    // Either use a full screen quad, or a provided mesh renderer to draw the waves
                    if (_mesh == null)
                    {
                        if (isBlending)
                        {
                            buf.DrawProcedural(Matrix4x4.identity, _shapeFFT._blendMaterial, 0, MeshTopology.Triangles, 3);
                        }
                        buf.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
                    }
                    else if (_material != null)
                    {
                        if (isBlending)
                        {
                            buf.DrawMesh(_mesh, _shapeFFT.transform.localToWorldMatrix, _shapeFFT._blendMaterial);
                        }
                        buf.DrawMesh(_mesh, _shapeFFT.transform.localToWorldMatrix, _material);
                    }
                }
            }
        }

        public const int CASCADE_COUNT = 16;

        FFTBatch[] _batches = null;

        // Used to populate data on first frame
        bool _firstUpdate = true;

        // Active material.
        Material _matGenerateWaves;
        // Cache material options.
        Material _matGenerateWavesGlobal;
        Material _matGenerateWavesGeometry;

        // We do not have alpha channel available for blending so use separate shader to do a blend pass.
        Material _blendMaterial;

        static readonly int sp_WaveBuffer = Shader.PropertyToID("_WaveBuffer");
        static readonly int sp_WaveBufferSliceIndex = Shader.PropertyToID("_WaveBufferSliceIndex");
        static readonly int sp_AverageWavelength = Shader.PropertyToID("_AverageWavelength");
        static readonly int sp_RespectShallowWaterAttenuation = Shader.PropertyToID("_RespectShallowWaterAttenuation");
        static readonly int sp_MaximumAttenuationDepth = Shader.PropertyToID("_MaximumAttenuationDepth");
        static readonly int sp_FeatherWaveStart = Shader.PropertyToID("_FeatherWaveStart");
        readonly int sp_AxisX = Shader.PropertyToID("_AxisX");

        static int s_Count = 0;

        /// <summary>
        /// Min wavelength for a cascade in the wave buffer. Does not depend on viewpoint.
        /// </summary>
        public float MinWavelength(int cascadeIdx)
        {
            var diameter = 0.5f * (1 << cascadeIdx);
            // Matches constant with same name in FFTSpectrum.compute
            var WAVE_SAMPLE_FACTOR = 8f;
            return diameter / WAVE_SAMPLE_FACTOR;

            // This used to be:
            //var texelSize = diameter / _resolution;
            //float samplesPerWave = _resolution / 8;
            //return texelSize * samplesPerWave;
        }

        public void CrestUpdate(CommandBuffer buf)
        {
#if UNITY_EDITOR
            UpdateEditorOnly();
#endif

            var updateDataEachFrame = !_spectrumFixedAtRuntime;
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) updateDataEachFrame = true;
#endif
            // Ensure batches assigned to correct slots
            if (_firstUpdate || updateDataEachFrame)
            {
                InitBatches();

                _firstUpdate = false;
            }

            _matGenerateWaves.SetFloat(sp_RespectShallowWaterAttenuation, _respectShallowWaterAttenuation);
            _matGenerateWaves.SetFloat(sp_MaximumAttenuationDepth, OceanRenderer.Instance._lodDataAnimWaves.Settings.MaximumAttenuationDepth);
            _matGenerateWaves.SetFloat(sp_FeatherWaveStart, _featherWaveStart);

            if (_blendMaterial != null)
            {
                _blendMaterial.SetFloat(sp_FeatherWaveStart, _featherWaveStart);
            }

            // If using geo, the primary wave dir is used by the input shader to rotate the waves relative
            // to the geo rotation. If not, the wind direction is already used in the FFT gen.
            var waveDir = _meshForDrawingWaves != null ? PrimaryWaveDirection : Vector2.right;
            _matGenerateWaves.SetVector(sp_AxisX, waveDir);

            // If geometry is being used, the ocean input shader will rotate the waves to align to geo
            var windDirRad = WindDirRadForFFT;
            var windSpeedMPS = WindSpeedForFFT;
            float loopPeriod = LoopPeriod;

            // Don't create tons of generators when values are varying. Notify so that existing generators may be adapted.
            if (_windTurbulenceOld != _windTurbulence || _windDirRadOld != windDirRad || _windSpeedOld != windSpeedMPS || _spectrumOld != _spectrum)
            {
                FFTCompute.OnGenerationDataUpdated(_resolution, loopPeriod, _windTurbulenceOld, _windDirRadOld, _windSpeedOld, _spectrumOld, _windTurbulence, windDirRad, windSpeedMPS, _spectrum);
            }

            var waveData = FFTCompute.GenerateDisplacements(buf, _resolution, loopPeriod, _windTurbulence, windDirRad, windSpeedMPS, OceanRenderer.Instance.CurrentTime, _activeSpectrum, updateDataEachFrame);

            _windTurbulenceOld = _windTurbulence;
            _windDirRadOld = windDirRad;
            _windSpeedOld = windSpeedMPS;
            _spectrumOld = _spectrum;
            _matGenerateWaves.SetTexture(sp_WaveBuffer, waveData);

            ReportMaxDisplacement();
        }

        void CreateOrUpdateSplineMesh()
        {
            if (TryGetComponent<Spline.Spline>(out var spline))
            {
                var radius = _overrideSplineSettings ? _radius : spline.Radius;
                var subdivs = _overrideSplineSettings ? _subdivisions : spline.Subdivisions;
                if (ShapeGerstnerSplineHandling.GenerateMeshFromSpline<SplinePointDataWaves>(spline, transform,
                    subdivs, radius, Vector2.one, ref _meshForDrawingWaves, out _, out _))
                {
                    _meshForDrawingWaves.name = gameObject.name + "_mesh";
                }
            }
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
                _activeSpectrum = DefaultSpectrum;
            }

            // Unassign mesh
            if (_meshForDrawingWaves != null && !TryGetComponent<Spline.Spline>(out _))
            {
                _meshForDrawingWaves = null;
            }
        }

        // Called by Predicated attribute. Signature must not be changed.
        bool IsLocalWaves(Component component)
        {
            return TryGetComponent<MeshRenderer>(out _) || TryGetComponent<Spline.Spline>(out _);
        }
#endif

        void ReportMaxDisplacement()
        {
            // Apply weight or will cause popping due to scale change.
            OceanRenderer.Instance.ReportMaxDisplacementFromShape(_maxHorizontalDisplacement * _weight, _maxVerticalDisplacement * _weight, _maxVerticalDisplacement * _weight);
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
            }

            if (_blendMaterial == null)
            {
                _blendMaterial = new Material(Shader.Find("Crest/Inputs/Animated Waves/Spline Blending"));
            }

            // Queue determines draw order of this input. Global waves should be rendered first. They are additive
            // so not order dependent.
            var queue = int.MinValue;
            var subQueue = transform.GetSiblingIndex();

            if (_meshForDrawingWaves == null)
            {
                if (_matGenerateWavesGlobal == null)
                {
                    _matGenerateWavesGlobal = new Material(Shader.Find("Hidden/Crest/Inputs/Animated Waves/Gerstner Global"));
                }

                _matGenerateWaves = _matGenerateWavesGlobal;
            }
            else
            {
                if (_matGenerateWavesGeometry == null)
                {
                    _matGenerateWavesGeometry = new Material(Shader.Find("Crest/Inputs/Animated Waves/Gerstner Geometry"));
                }

                _matGenerateWaves = _matGenerateWavesGeometry;
                queue = _queue;
            }

            // Submit draws to create the FFT waves
            _batches = new FFTBatch[CASCADE_COUNT];
            for (int i = 0; i < CASCADE_COUNT; i++)
            {
                if (i == -1) break;
                _batches[i] = new FFTBatch(this, MinWavelength(i), i, _matGenerateWaves, _meshForDrawingWaves);
                RegisterLodDataInput<LodDataMgrAnimWaves>.RegisterInput(_batches[i], queue, subQueue);
            }
        }

        void Awake()
        {
            s_Count++;
        }

        void OnDestroy()
        {
            // Since FFTCompute resources are shared we will clear after last ShapeFFT is destroyed.
            if (--s_Count <= 0)
            {
                FFTCompute.CleanUpAll();

                if (s_DefaultSpectrum != null)
                {
                    Helpers.Destroy(s_DefaultSpectrum);
                }
            }
        }

        private void OnEnable()
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

        void OnDisable()
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

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            _resolution = Mathf.ClosestPowerOfTwo(_resolution);
            _resolution = Mathf.Max(_resolution, 16);
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

        void OnGUI()
        {
            if (_debugDrawSlicesInEditor)
            {
                FFTCompute.OnGUI(_resolution, LoopPeriod, _windTurbulence, WindDirRadForFFT, WindSpeedForFFT, _activeSpectrum);
            }
        }
#endif

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
    public partial class ShapeFFT : IValidated
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

#if !CREST_UNITY_MATHEMATICS
            if (_enableBakedCollision)
            {
                showMessage
                (
                    "The <i>Unity Mathematics (com.unity.mathematics)</i> package is required for baking.",
                    "Add the <i>Unity Mathematics</i> package.",
                    ValidatedHelper.MessageType.Warning, this,
                    ValidatedHelper.FixAddMissingMathPackage
                );
            }
#endif

            return isValid;
        }
    }

    [CustomEditor(typeof(ShapeFFT))]
    public class ShapeFFTEditor : CustomBaseEditor
    {
#if CREST_UNITY_MATHEMATICS
        /// <summary>
        /// Display some validation and statistics about the bake.
        /// </summary>
        void BakeHelpBox(ShapeFFT target)
        {
            var message = "";

            FFTBaker.ComputeRequiredOctaves(target._spectrum, target._smallestWavelengthRequired, out var smallestOctaveRequired, out var largestOctaveRequired);
            if (largestOctaveRequired == -1 || smallestOctaveRequired == -1 || smallestOctaveRequired > largestOctaveRequired)
            {
                EditorGUILayout.HelpBox("No waves in spectrum. Increase one or more of the spectrum sliders.", MessageType.Error);
                return;
            }

            message += $"FFT resolution is {target._resolution}.";
            message += $" Spectrum power sliders give {largestOctaveRequired - smallestOctaveRequired + 1} active octaves greater than smallest wavelength {target._smallestWavelengthRequired}m.";
            var scales = largestOctaveRequired - smallestOctaveRequired + 2;
            message += $" Bake data resolution will be {target._resolution} x {target._resolution} x {scales}.";

            message += "\n\n";
            message += $"Period is {target._timeLoopLength}s.";
            message += $" Frames per second setting is {target._timeResolution}.";
            var frameCount = target._timeLoopLength * target._timeResolution;
            message += $" Frame count is {target._timeLoopLength} x {target._timeResolution} = {frameCount}.";

            message += "\n\n";
            var pointsPerFrame = target._resolution * target._resolution * scales;
            var channelCount = 4;
            var bytesPerChannel = 4;
            message += $"Total data size will be {pointsPerFrame * frameCount * channelCount * bytesPerChannel / 1048576f} MB.";

            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        public override void OnInspectorGUI()
        {
            var target = this.target as ShapeFFT;

            base.OnInspectorGUI();

            bool bakingEnabled = target._enableBakedCollision;

            if (bakingEnabled)
            {
                if (target._spectrum == null)
                {
                    EditorGUILayout.HelpBox("A spectrum must be assigned to enable collision baking.", MessageType.Error);
                    return;
                }

                BakeHelpBox(target);
            }

            GUI.enabled = bakingEnabled;
            OnInspectorGUIBaking();
            GUI.enabled = true;
        }

        /// <summary>
        /// Controls & GUI for baking.
        /// </summary>
        void OnInspectorGUIBaking()
        {
            if (OceanRenderer.Instance == null) return;

            var bakeLabel = "Bake to asset";
            var bakeAndAssignLabel = "Bake to asset and assign to current settings";
            var selectCurrentSettingsLabel = "Select current settings";
            if (OceanRenderer.Instance._simSettingsAnimatedWaves != null)
            {
                if (GUILayout.Button(bakeLabel))
                {
                    FFTBaker.BakeShapeFFT(target as ShapeFFT);
                }

                GUI.enabled = GUI.enabled && OceanRenderer.Instance._simSettingsAnimatedWaves != null;
                if (GUILayout.Button(bakeAndAssignLabel))
                {
                    var result = FFTBaker.BakeShapeFFT(target as ShapeFFT);
                    if (result != null)
                    {
                        OceanRenderer.Instance._simSettingsAnimatedWaves.CollisionSource = SimSettingsAnimatedWaves.CollisionSources.BakedFFT;
                        OceanRenderer.Instance._simSettingsAnimatedWaves._bakedFFTData = result;
                        Selection.activeObject = OceanRenderer.Instance._simSettingsAnimatedWaves;

                        // Rebuild ocean
                        OceanRenderer.Instance.Rebuild();
                    }
                }
                GUI.enabled = true;

                if (GUILayout.Button(selectCurrentSettingsLabel))
                {
                    Selection.activeObject = OceanRenderer.Instance._simSettingsAnimatedWaves;
                }
            }
            else
            {
                // No settings available, disable and show tooltip
                GUI.enabled = false;
                GUILayout.Button(new GUIContent(bakeAndAssignLabel, "No settings available to apply to. Assign an Animated Waves Sim Settings to the OceanRenderer component."));
                GUILayout.Button(new GUIContent(selectCurrentSettingsLabel, "No settings available. Assign an Animated Waves Sim Settings to the OceanRenderer component."));
                GUI.enabled = true;
            }
        }
#endif // CREST_UNITY_MATHEMATICS
    }
#endif
}
