// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Scales the ocean horizontally based on the camera height, to keep geometry detail uniform-ish in screen space.
    /// </summary>
    public class OceanRenderer : MonoBehaviour
    {
        [Tooltip("The viewpoint which drives the ocean detail. Defaults to main camera."), SerializeField]
        Transform _viewpoint;
        public Transform Viewpoint { get { return _viewpoint; } set { _viewpoint = value; } }

        [Tooltip("Optional provider for time, can be used to hardcode time for automation, or provide server time. Defaults to local Unity time."), SerializeField]
        TimeProviderBase _timeProvider;
        public float CurrentTime { get { return _timeProvider.CurrentTime; } }


        [Header("Ocean Params")]

        [SerializeField, Tooltip("Material to use for the ocean surface")]
        Material _material = null;
        public Material OceanMaterial { get { return _material; } }

        [SerializeField]
        string _layerName = "Water";
        public string LayerName { get { return _layerName; } }

        [Tooltip("Wind direction (angle from x axis in degrees)"), Range(-180, 180)]
        public float _windDirectionAngle = 0f;
        public Vector2 WindDir { get { return new Vector2(Mathf.Cos(Mathf.PI * _windDirectionAngle / 180f), Mathf.Sin(Mathf.PI * _windDirectionAngle / 180f)); } }

        [SerializeField, Delayed, Tooltip("Multiplier for physics gravity."), Range(0f, 10f)]
        float _gravityMultiplier = 1f;
        public float Gravity { get { return _gravityMultiplier * Physics.gravity.magnitude; } }


        [Header("Detail Params")]

        [Range(0, 15)]
        [Tooltip("Min number of verts / shape texels per wave.")]
        public float _minTexelsPerWave = 3f;

        [Delayed, Tooltip("The smallest scale the ocean can be.")]
        public float _minScale = 8f;

        [Delayed, Tooltip("The largest scale the ocean can be (-1 for unlimited).")]
        public float _maxScale = 256f;

        [SerializeField, Delayed, Tooltip("Resolution of ocean LOD data. Use even numbers like 256 or 384. This is 4x the old 'Base Vert Density' param, so if you used 64 for this param, set this to 256.")]
        int _lodDataResolution = 256;
        public int LodDataResolution { get { return _lodDataResolution; } }

        [SerializeField, Delayed, Tooltip("How much of the water shape gets tessellated by geometry. If set to e.g. 4, every geometry quad will cover 4x4 LOD data texels. Use power of 2 values like 1, 2, 4...")]
        int _geometryDownSampleFactor = 2;

        [SerializeField, Delayed, Tooltip("Number of ocean tile scales/LODs to generate."), Range(2, LodDataMgr.MAX_LOD_COUNT)]
        int _lodCount = 7;


        [Header("Simulation Params")]

        public SimSettingsAnimatedWaves _simSettingsAnimatedWaves;

        [Tooltip("Water depth information used for shallow water, shoreline foam, wave attenuation, among others."), SerializeField]
        bool _createSeaFloorDepthData = true;
        public bool CreateSeaFloorDepthData { get { return _createSeaFloorDepthData; } }

        [Tooltip("Simulation of foam created in choppy water and dissipating over time."), SerializeField]
        bool _createFoamSim = true;
        public bool CreateFoamSim { get { return _createFoamSim; } }
        public SimSettingsFoam _simSettingsFoam;

        [Tooltip("Dynamic waves generated from interactions with objects such as boats."), SerializeField]
        bool _createDynamicWaveSim = false;
        public bool CreateDynamicWaveSim { get { return _createDynamicWaveSim; } }
        public SimSettingsWave _simSettingsDynamicWaves;

        [Tooltip("Horizontal motion of water body, akin to water currents."), SerializeField]
        bool _createFlowSim = false;
        public bool CreateFlowSim { get { return _createFlowSim; } }
        public SimSettingsFlow _simSettingsFlow;

        [Tooltip("Shadow information used for lighting water."), SerializeField]
        bool _createShadowData = false;
        public bool CreateShadowData { get { return _createShadowData; } }
        [Tooltip("The primary directional light. Required if shadowing is enabled.")]
        public Light _primaryLight;
        public SimSettingsShadow _simSettingsShadow;


        [Header("Debug Params")]

        [Tooltip("Whether to generate ocean geometry tiles uniformly (with overlaps)")]
        public bool _uniformTiles = false;
        [Tooltip("Disable generating a wide strip of triangles at the outer edge to extend ocean to edge of view frustum")]
        public bool _disableSkirt = false;


        float _viewerAltitudeLevelAlpha = 0f;
        /// <summary>
        /// The ocean changes scale when viewer changes altitude, this gives the interpolation param between scales.
        /// </summary>
        public float ViewerAltitudeLevelAlpha { get { return _viewerAltitudeLevelAlpha; } }

        /// <summary>
        /// Sea level is given by y coordinate of GameObject with OceanRenderer script.
        /// </summary>
        public float SeaLevel { get { return transform.position.y; } }

        [HideInInspector] public LodTransform[] _lods;
        [HideInInspector] public LodDataMgrAnimWaves _lodDataAnimWaves;
        [HideInInspector] public LodDataMgrSeaFloorDepth _lodDataSeaDepths;
        [HideInInspector] public LodDataMgrDynWaves _lodDataDynWaves;
        [HideInInspector] public LodDataMgrFlow _lodDataFlow;
        [HideInInspector] public LodDataMgrFoam _lodDataFoam;
        [HideInInspector] public LodDataMgrShadow _lodDataShadow;
        public int CurrentLodCount { get { return _lods.Length; } }

        /// <summary>
        /// Vertical offset of viewer vs water surface
        /// </summary>
        public float ViewerHeightAboveWater { get; private set; }

        SamplingData _samplingData = new SamplingData();

        void Awake()
        {
            if (_material == null)
            {
                Debug.LogError("A material for the ocean must be assigned on the Material property of the OceanRenderer.", this);
                enabled = false;
                return;
            }

            _instance = this;

            OceanBuilder.GenerateMesh(this, _lodDataResolution, _geometryDownSampleFactor, _lodCount);

            if (null == GetComponent<BuildCommandBufferBase>())
            {
                gameObject.AddComponent<BuildCommandBuffer>();
            }

            InitViewpoint();
            InitTimeProvider();
        }

        void InitViewpoint()
        {
            if (_viewpoint == null)
            {
                var camMain = Camera.main;
                if (camMain != null)
                {
                    _viewpoint = camMain.transform;
                }
                else
                {
                    Debug.LogError("Please provide the viewpoint transform, or tag the primary camera as MainCamera.", this);
                }
            }
        }

        void InitTimeProvider()
        {
            if (_timeProvider == null)
            {
                _timeProvider = gameObject.AddComponent<TimeProviderDefault>();
            }
        }

        void Update()
        {
            UpdateCollision();
        }

        public void UpdateCollision()
        {
            if (_simSettingsAnimatedWaves.CachedHeightQueries)
            {
                (CollisionProvider as CollProviderCache).ClearCache();
            }
        }

        void LateUpdate()
        {
            // set global shader params
            Shader.SetGlobalFloat("_TexelsPerWave", _minTexelsPerWave);
            Shader.SetGlobalVector("_WindDirXZ", WindDir);
            Shader.SetGlobalFloat("_CrestTime", CurrentTime);

            LateUpdatePosition();
            LateUpdateScale();
            LateUpdateViewerHeight();
            LateUpdateLods();
        }

        void LateUpdatePosition()
        {
            Vector3 pos = _viewpoint.position;

            // maintain y coordinate - sea level
            pos.y = transform.position.y;

            transform.position = pos;

            Shader.SetGlobalVector("_OceanCenterPosWorld", transform.position);
        }

        void LateUpdateScale()
        {
            // reach maximum detail at slightly below sea level. this should combat cases where visual range can be lost
            // when water height is low and camera is suspended in air. i tried a scheme where it was based on difference
            // to water height but this does help with the problem of horizontal range getting limited at bad times.
            float maxDetailY = SeaLevel - _maxVertDispFromShape / 5f;
            // scale ocean mesh based on camera distance to sea level, to keep uniform detail.
            float camY = Mathf.Max(Mathf.Abs(_viewpoint.position.y) - maxDetailY, 0f);

            const float HEIGHT_LOD_MUL = 2f;
            float level = camY * HEIGHT_LOD_MUL;
            level = Mathf.Max(level, _minScale);
            if (_maxScale != -1f) level = Mathf.Min(level, 1.99f * _maxScale);

            float l2 = Mathf.Log(level) / Mathf.Log(2f);
            float l2f = Mathf.Floor(l2);

            _viewerAltitudeLevelAlpha = l2 - l2f;

            float newScale = Mathf.Pow(2f, l2f);
            transform.localScale = new Vector3(newScale, 1f, newScale);
        }

        void LateUpdateViewerHeight()
        {
            var pos = Viewpoint.position;
            var rect = new Rect(pos.x, pos.z, 0f, 0f);

            float waterHeight;
            if (CollisionProvider.GetSamplingData(ref rect, 0f, _samplingData)
                && CollisionProvider.SampleHeight(ref pos, _samplingData, out waterHeight))
            {
                ViewerHeightAboveWater = pos.y - waterHeight;
            }

            CollisionProvider.ReturnSamplingData(_samplingData);
        }

        void LateUpdateLods()
        {
            foreach (var lt in _lods)
            {
                lt.UpdateTransform();
            }

            if (_lodDataAnimWaves) _lodDataAnimWaves.UpdateLodData();
            if (_lodDataDynWaves) _lodDataDynWaves.UpdateLodData();
            if (_lodDataFlow) _lodDataFlow.UpdateLodData();
            if (_lodDataFoam) _lodDataFoam.UpdateLodData();
            if (_lodDataSeaDepths) _lodDataSeaDepths.UpdateLodData();
            if (_lodDataShadow) _lodDataShadow.UpdateLodData();
        }

        private void OnDestroy()
        {
            _instance = null;
        }

        [ContextMenu("Regenerate mesh")]
        void RegenMesh()
        {
            OceanBuilder.GenerateMesh(this, _lodDataResolution, _geometryDownSampleFactor, _lodCount);
        }
#if UNITY_EDITOR
        [ContextMenu("Regenerate mesh", true)]
        bool RegenPossible() { return UnityEditor.EditorApplication.isPlaying; }
#endif

        public bool ScaleCouldIncrease { get { return _maxScale == -1f || transform.localScale.x < _maxScale * 0.99f; } }
        public bool ScaleCouldDecrease { get { return _minScale == -1f || transform.localScale.x > _minScale * 1.01f; } }

        public int GetLodIndex(float gridSize)
        {
            //gridSize = 4f * transform.lossyScale.x * Mathf.Pow(2f, result) / _lodDataResolution;
            int result = Mathf.RoundToInt(Mathf.Log(_lodDataResolution * gridSize / (4f * transform.lossyScale.x)) / Mathf.Log(2f));

            if (result < 0 || result >= _lodCount)
            {
                result = -1;
            }

            return result;
        }

        /// <summary>
        /// Shape scripts can report in how far they might displace the shape horizontally. The max value is saved here.
        /// Later the bounding boxes for the ocean tiles will be expanded to account for this potential displacement.
        /// </summary>
        public void ReportMaxDisplacementFromShape(float maxHorizDisp, float maxVertDisp)
        {
            if (Time.frameCount != _maxDisplacementCachedTime)
            {
                _maxHorizDispFromShape = _maxVertDispFromShape = 0f;
            }

            _maxHorizDispFromShape += maxHorizDisp;
            _maxVertDispFromShape += maxVertDisp;

            _maxDisplacementCachedTime = Time.frameCount;
        }
        float _maxHorizDispFromShape = 0f, _maxVertDispFromShape = 0f;
        int _maxDisplacementCachedTime = 0;
        /// <summary>
        /// The maximum horizontal distance that the shape scripts are displacing the shape.
        /// </summary>
        public float MaxHorizDisplacement { get { return _maxHorizDispFromShape; } }
        /// <summary>
        /// The maximum height that the shape scripts are displacing the shape.
        /// </summary>
        public float MaxVertDisplacement { get { return _maxVertDispFromShape; } }

        static OceanRenderer _instance;
        public static OceanRenderer Instance { get { return _instance ?? (_instance = FindObjectOfType<OceanRenderer>()); } }

        /// <summary>
        /// Provides ocean shape to CPU.
        /// </summary>
        ICollProvider _collProvider;
        public ICollProvider CollisionProvider { get { return _collProvider != null ? _collProvider : (_collProvider = _simSettingsAnimatedWaves.CreateCollisionProvider()); } }
    }
}
