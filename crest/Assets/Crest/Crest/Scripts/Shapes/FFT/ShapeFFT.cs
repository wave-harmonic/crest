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
    [ExecuteAlways]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Shape FFT")]
    [CrestHelpURL("user/wave-conditions")]
    public partial class ShapeFFT : MonoBehaviour, LodDataMgrAnimWaves.IShapeUpdatable, IPaintable
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

        public enum Mode
        {
            Global,
            Painted,
            Spline
        }

        [Header("Mode")]
        public Mode _inputMode = Mode.Global;

        public bool ShowPaintingUI => _inputMode == Mode.Painted;

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
        public float WindDirRadForFFT => _inputMode == Mode.Spline ? 0f : _waveDirectionHeadingAngle * Mathf.Deg2Rad;
        public Vector2 PrimaryWaveDirection => new Vector2(Mathf.Cos(Mathf.PI * _waveDirectionHeadingAngle / 180f), Mathf.Sin(Mathf.PI * _waveDirectionHeadingAngle / 180f));

        [Tooltip("When true, uses the wind speed on this component rather than the wind speed from the Ocean Renderer component.")]
        public bool _overrideGlobalWindSpeed = false;
        [Tooltip("Wind speed in km/h. Controls wave conditions."), Range(0, 150f, 2f), Predicated("_overrideGlobalWindSpeed")]
        public float _windSpeed = 20f;
        public float WindSpeedForFFT => (_overrideGlobalWindSpeed ? _windSpeed : OceanRenderer.Instance._globalWindSpeed) / 3.6f;

        [Tooltip("Multiplier for these waves to scale up/down."), Range(0f, 1f)]
        public float _weight = 1f;

        [Predicated("_inputMode", inverted: false, Mode.Global), DecoratedField]
        [Tooltip("How the waves are blended into the wave buffer. Use <i>AlphaBlend</i> to override waves.")]
        public Helpers.BlendPreset _blendMode = Helpers.BlendPreset.AdditiveBlend;

        [Predicated("_inputMode", inverted: true, Mode.Spline), DecoratedField]
        [Tooltip("Order this input will render.")]
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

        #region Painting
        [Header("Paint Mode Settings")]
        [Predicated("_inputMode", inverted: true, Mode.Painted), DecoratedField]
        public CPUTexture2DPaintable_RG16_AddBlend _paintData;
        void PreparePaintInputMaterial(Material mat)
        {
            _paintData.CenterPosition3 = transform.position;
            _paintData.PrepareMaterial(mat, CPUTexture2DHelpers.ColorConstructFnTwoChannel);
        }
        void UpdatePaintInputMaterial(Material mat)
        {
            _paintData.CenterPosition3 = transform.position;
            _paintData.UpdateMaterial(mat, CPUTexture2DHelpers.ColorConstructFnTwoChannel);
            Helpers.SetBlendFromPreset(mat, _blendMode);
        }

        public Vector2 WorldSize => _paintData.WorldSize;
        public Transform Transform => transform;

        public void ClearData() => _paintData.Clear(this, Vector2.zero);
        public void MakeDirty() => _paintData.MakeDirty();

        public bool Paint(Vector3 paintPosition3, Vector2 paintDir, float paintWeight, bool remove)
        {
            _paintData.CenterPosition3 = transform.position;

            return _paintData.PaintSmoothstep(this, paintPosition3, 0.0125f * paintWeight, paintDir, _paintData.BrushRadius, _paintData._brushStrength, CPUTexturePaintHelpers.PaintFnAdditivePlusRemoveBlendSaturateVector2, remove);
        }
        #endregion

        [Header("Spline Mode Settings")]
        [SerializeField, Predicated("_inputMode", inverted: true, Mode.Spline), DecoratedField]
        float _featherWaveStart = 0.1f;
        [SerializeField, Predicated("_inputMode", inverted: true, Mode.Spline), DecoratedField]
        bool _overrideSplineSettings = false;
        [SerializeField, Predicated("_overrideSplineSettings"), DecoratedField]
        float _radius = 50f;
        [SerializeField, Predicated("_overrideSplineSettings"), Delayed]
        int _subdivisions = 1;


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

        public IPaintedData PaintedData => _paintData;
        public Shader PaintedInputShader => null;

        Mesh MeshForGeneration => _inputMode == Mode.Spline ? _meshForDrawingWavesSpline : null;

        // Spline mesh
        Mesh _meshForDrawingWavesSpline;

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

#if UNITY_EDITOR
        internal bool _isPrefabStageInstance = false;
#endif

        public class FFTBatch : ILodDataInput
        {
            ShapeFFT _shapeFFT;

            Material _material;
            Mesh _mesh;

            int _waveBufferSliceIndex;

            public FFTBatch(ShapeFFT shapeFFT, float wavelength, int waveBufferSliceIndex, Material material, Mesh mesh)
            {
                _shapeFFT = shapeFFT;
                // Need sample higher than Nyquist to get good results, especially when waves flowing
                Wavelength = wavelength / OceanRenderer.Instance._lodDataAnimWaves.Settings.WaveResolutionMultiplier;
                _waveBufferSliceIndex = waveBufferSliceIndex;
                _mesh = mesh;
                _material = material;
            }

            // The ocean input system uses this to decide which lod this batch belongs in
            public float Wavelength { get; private set; }

            public bool Enabled { get => true; set { } }

            public Material Material => _material;

            public void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
            {
                var finalWeight = weight * _shapeFFT._weight;
                if (finalWeight > 0f)
                {
                    buf.SetGlobalInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                    buf.SetGlobalFloat(RegisterLodDataInputBase.sp_Weight, finalWeight);
                    buf.SetGlobalInt(sp_WaveBufferSliceIndex, _waveBufferSliceIndex);
                    buf.SetGlobalFloat(sp_AverageWavelength, Wavelength * 1.5f / OceanRenderer.Instance._lodDataAnimWaves.Settings.WaveResolutionMultiplier);

                    // Either use a full screen quad, or a provided mesh renderer to draw the waves
                    if (_mesh == null)
                    {
                        buf.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
                    }
                    else if (_material != null)
                    {
                        buf.DrawMesh(_mesh, _shapeFFT.transform.localToWorldMatrix, _material);
                    }
                }
            }
        }

        public const int CASCADE_COUNT = 16;

        FFTBatch[] _batches = null;

        // Used to populate data on first frame
        bool _firstUpdate = true;

        // Various material options
        Material _matGenerateWavesGlobal;
        Material _matGenerateWavesPainted;
        // This is used both by spline and by user provided geo.. so left here
        Material _matGenerateWavesGeometry;

        Material MaterialForGeneration
        {
            get
            {
                if (_inputMode == Mode.Global) return _matGenerateWavesGlobal;
                if (_inputMode == Mode.Painted) return _matGenerateWavesPainted;
                if (_inputMode == Mode.Spline) return _matGenerateWavesGeometry;
                return _matGenerateWavesGeometry;
            }
        }

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

        public bool AutoDetectMode(out Mode mode)
        {
            // Ease of use - set mode based on attached components
            if (TryGetComponent<Spline.Spline>(out _))
            {
                mode = Mode.Spline;
                return true;
            }

            mode = Mode.Global;
            return false;
        }

        private void Reset()
        {
            if (AutoDetectMode(out var mode))
            {
                _inputMode = mode;
            }
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

            var mat = MaterialForGeneration;

            bool needToRefreshMaterials = _batches == null || mat == null || _batches.Length < 1 || _batches[0].Material != mat;

            // Ensure batches assigned to correct slots
            if (_firstUpdate || updateDataEachFrame || needToRefreshMaterials)
            {
                InitBatches();

                mat = MaterialForGeneration;

                _firstUpdate = false;
            }


            mat.SetFloat(sp_RespectShallowWaterAttenuation, _respectShallowWaterAttenuation);
            mat.SetFloat(sp_MaximumAttenuationDepth, OceanRenderer.Instance._lodDataAnimWaves.Settings.MaximumAttenuationDepth);
            mat.SetFloat(sp_FeatherWaveStart, _featherWaveStart);

            if (_inputMode == Mode.Painted)
            {
                UpdatePaintInputMaterial(mat);
            }

            if (_inputMode == Mode.Spline && _matGenerateWavesGeometry != null)
            {
                Helpers.SetBlendFromPreset(_matGenerateWavesGeometry, _blendMode);
            }

            if (_inputMode == Mode.Global && _matGenerateWavesGlobal != null)
            {
                Helpers.SetBlendFromPreset(_matGenerateWavesGlobal, Helpers.BlendPreset.AdditiveBlend);
            }

            // If using geo, the primary wave dir is used by the input shader to rotate the waves relative
            // to the geo rotation. If not, the wind direction is already used in the FFT gen.
            var waveDir = _inputMode == Mode.Spline ? PrimaryWaveDirection : Vector2.right;
            mat.SetVector(sp_AxisX, waveDir);

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
            mat.SetTexture(sp_WaveBuffer, waveData);

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
                _activeSpectrum = DefaultSpectrum;
            }
        }
#endif

        void ReportMaxDisplacement()
        {
            // Apply weight or will cause popping due to scale change.
            OceanRenderer.Instance.ReportMaxDisplacementFromShape(_maxHorizontalDisplacement * _weight, _maxVerticalDisplacement * _weight, _maxVerticalDisplacement * _weight);
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


            if (_inputMode == Mode.Global)
            {
                if (_matGenerateWavesGlobal == null)
                {
                    _matGenerateWavesGlobal = new Material(Shader.Find("Hidden/Crest/Inputs/Animated Waves/Generate Waves"));
                }

                Helpers.SetBlendFromPreset(_matGenerateWavesGlobal, Helpers.BlendPreset.AdditiveBlend);
            }
            else if (_inputMode == Mode.Painted)
            {
                if (_matGenerateWavesPainted == null)
                {
                    _matGenerateWavesPainted = new Material(Shader.Find("Hidden/Crest/Inputs/Animated Waves/Generate Waves"));
                }

                // This should probably warn or error on multiple input types (GetComponents<IUserAuthoredInput>().length > 1) in
                // validation
                _paintData.PrepareMaterial(_matGenerateWavesPainted, CPUTexture2DHelpers.ColorConstructFnTwoChannel);

                Helpers.SetBlendFromPreset(_matGenerateWavesPainted, _blendMode);
            }
            else if (_inputMode == Mode.Spline)
            {
                if (TryGetComponent<Spline.Spline>(out var splineForWaves))
                {
                    // Init mesh
                    var radius = _overrideSplineSettings ? _radius : splineForWaves.Radius;
                    var subdivs = _overrideSplineSettings ? _subdivisions : splineForWaves.Subdivisions;
                    if (ShapeGerstnerSplineHandling.GenerateMeshFromSpline<SplinePointDataWaves>(splineForWaves, transform, subdivs,
                        radius, Vector2.one, ref _meshForDrawingWavesSpline, out _, out _))
                    {
                        _meshForDrawingWavesSpline.name = gameObject.name + "_mesh";
                    }

                    // Init material
                    if (_matGenerateWavesGeometry == null)
                    {
                        _matGenerateWavesGeometry = new Material(Shader.Find("Crest/Inputs/Animated Waves/Gerstner Geometry"));
                    }

                    Helpers.SetBlendFromPreset(_matGenerateWavesGeometry, _blendMode);
                }
            }

            var usingGeometryToGenerate = _inputMode == Mode.Spline;
            if (!usingGeometryToGenerate || MeshForGeneration)
            {
                // Submit draws to create the FFT waves
                _batches = new FFTBatch[CASCADE_COUNT];
                var generationMaterial = MaterialForGeneration;
                for (int i = 0; i < CASCADE_COUNT; i++)
                {
                    if (i == -1) break;
                    _batches[i] = new FFTBatch(this, MinWavelength(i), i, generationMaterial, MeshForGeneration);
                    // Use the queue if local waves.
                    registered.Add(MeshForGeneration != null ? _queue : int.MinValue, _batches[i]);
                }
            }
        }

        void Awake()
        {
#if UNITY_EDITOR
            // Store whether this instance was created in a prefab stage.
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            _isPrefabStageInstance = stage != null && gameObject.scene == stage.scene;
#endif

            s_Count++;
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            if (_isPrefabStageInstance)
            {
                return;
            }
#endif

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
#if UNITY_EDITOR
            if (_isPrefabStageInstance)
            {
                return;
            }
#endif

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
#if UNITY_EDITOR
            if (_isPrefabStageInstance)
            {
                return;
            }
#endif

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
        private void OnValidate()
        {
            _resolution = Mathf.ClosestPowerOfTwo(_resolution);
            _resolution = Mathf.Max(_resolution, 16);
        }

        private void OnDrawGizmosSelected()
        {
            if (_inputMode == Mode.Painted)
            {
                PaintableEditor.DrawPaintAreaGizmo(this, Color.green);
            }
        }

        void DrawMesh()
        {
            if (_inputMode == Mode.Spline && _meshForDrawingWavesSpline != null)
            {
                Gizmos.color = RegisterAnimWavesInput.s_gizmoColor;
                Gizmos.DrawWireMesh(_meshForDrawingWavesSpline, 0, transform.position, transform.rotation, transform.lossyScale);
            }
        }

        void OnGUI()
        {
            if (_debugDrawSlicesInEditor)
            {
                FFTCompute.OnGUI(_resolution, LoopPeriod, _windTurbulence, WindDirRadForFFT, WindSpeedForFFT, _activeSpectrum);
            }
        }

        public void OnSplinePointDrawGizmosSelected(SplinePoint point)
        {
            DrawMesh();
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
    // Validation
    public partial class ShapeFFT : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            if (_inputMode == Mode.Spline)
            {
                if (TryGetComponent<Spline.Spline>(out var spline))
                {
                    if (!spline.Validate(ocean, ValidatedHelper.Suppressed))
                    {
                        showMessage
                        (
                            "A <i>Spline</i> component is attached but it has validation errors.",
                            "Check this component in the Inspector for issues.",
                            ValidatedHelper.MessageType.Error, this
                        );

                        isValid = false;
                    }
                }
                else
                {
                    showMessage
                    (
                        "A <i>Crest Spline</i> component is required to drive this data and none is attached to this ocean input GameObject.",
                        "Attach a <i>Crest Spline</i> component.",
                        ValidatedHelper.MessageType.Error, gameObject,
                        ValidatedHelper.FixAttachComponent<Spline.Spline>
                    );

                    isValid = false;
                }
            }

            // Don't show the below soft suggestion if there are errors present as it may just be noise.
            if (isValid)
            {
                // Suggest that if a Spline is present, perhaps mode should be changed to use it (but only make suggestions if no errors)
                if (_inputMode != Mode.Spline && TryGetComponent<Spline.Spline>(out _))
                {
                    showMessage
                    (
                        "A <i>Spline</i> component is present on this GameObject but will not be used for this input.",
                        "Change the mode to <i>Spline</i> to use this renderer as the input.",
                        ValidatedHelper.MessageType.Info, this, so => RegisterLodDataInputBase.FixSetMode(so, (int)Mode.Spline)
                    );
                }
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

    // Here for the help boxes
    [CustomEditor(typeof(ShapeFFT))]
    public class ShapeFFTEditor : PaintableEditor
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

            if (target._isPrefabStageInstance)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(Internal.Constants.k_NoPrefabModeSupportWarning, MessageType.Warning);
                EditorGUILayout.Space();
            }

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

    // Ensure preview works (preview does not apply to derived classes so done per type)
    [CustomPreview(typeof(ShapeFFT))]
    public class ShapeFFTPreview : UserPaintedDataPreview
    {
    }
#endif // UNITY_EDITOR
}
