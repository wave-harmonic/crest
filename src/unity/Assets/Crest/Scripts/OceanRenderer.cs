// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Scales the ocean horizontally based on the camera height, to keep geometry detail uniform-ish in screen space.
    /// </summary>
    public class OceanRenderer : MonoBehaviour
    {
        [Tooltip("The viewpoint which drives the ocean detail. Defaults to main camera.")]
        public Transform _viewpoint;

        [Tooltip("Wind direction (angle from x axis in degrees)"), Range(-180, 180)]
        public float _windDirectionAngle = 0f;
        public Vector2 WindDir { get { return new Vector2(Mathf.Cos(Mathf.PI * _windDirectionAngle / 180f), Mathf.Sin(Mathf.PI * _windDirectionAngle / 180f)); } }

        [SerializeField, Delayed, Tooltip("Multiplier for physics gravity."), Range(0f, 10f)]
        float _gravityMultiplier = 1f;
        public float Gravity { get { return _gravityMultiplier * Physics.gravity.magnitude; } }

        [SerializeField, Tooltip("Cache CPU requests for ocean height. Requires restart.")]
        bool _cachedCpuOceanQueries = false;
        public bool CachedCpuOceanQueries { get { return _cachedCpuOceanQueries; } }

        [Range(0, 15)]
        [Tooltip("Min number of verts / shape texels per wave.")]
        public float _minTexelsPerWave = 3f;

        [Delayed, Tooltip("The smallest scale the ocean can be.")]
        public float _minScale = 16f;

        [Delayed, Tooltip("The largest scale the ocean can be (-1 for unlimited).")]
        public float _maxScale = 128f;

        [Header( "Geometry Params" )]
        [SerializeField, Delayed, Tooltip( "Side dimension in quads of an ocean tile." )]
        public float _baseVertDensity = 32f;
        [SerializeField, Delayed, Tooltip( "Number of ocean tile scales/LODs to generate." ), ]
        int _lodCount = 6;
        public int LodDataResolution { get { return (int)(4f * _baseVertDensity); } }

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

        void Start()
        {
            _instance = this;

            _oceanBuilder = FindObjectOfType<OceanBuilder>();
            _oceanBuilder.GenerateMesh(_baseVertDensity, _lodCount);

            // set render orders, event hooks, etc
            var scheduler = GetComponent<IOceanScheduler>();
            if (scheduler == null) scheduler = gameObject.AddComponent<OceanScheduler>();
            scheduler.ApplySchedule(_oceanBuilder);

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

        void Update()
        {
            if(_cachedCpuOceanQueries)
            {
                (CollisionProvider as CollProviderCache).ClearCache();
            }
        }

        void LateUpdate()
        {
            // set global shader params
            Shader.SetGlobalFloat( "_TexelsPerWave", _minTexelsPerWave );
            Shader.SetGlobalVector("_WindDirXZ", WindDir);

            LateUpdatePosition();
            LateUpdateScale();

            float maxWavelength = Builder._lodDataAnimWaves[Builder._lodDataAnimWaves.Length - 1].MaxWavelength();
            Shader.SetGlobalFloat("_MaxWavelength", maxWavelength);
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
            // scale ocean mesh based on camera height to keep uniform detail. this could be abs() if camera can go below water.
            float camY = Mathf.Max(_viewpoint.position.y - maxDetailY, 0f);

            const float HEIGHT_LOD_MUL = 2f;
            float level = camY * HEIGHT_LOD_MUL;
            level = Mathf.Max(level, _minScale);
            if (_maxScale != -1f) level = Mathf.Min(level, 1.99f * _maxScale);

            float l2 = Mathf.Log(level) / Mathf.Log(2f);
            float l2f = Mathf.Floor(l2);

            _viewerAltitudeLevelAlpha = l2 - l2f;

            float newScale = Mathf.Pow(2f, l2f);
            transform.localScale = new Vector3(newScale, 1f, newScale);

            Shader.SetGlobalFloat("_ViewerAltitudeLevelAlpha", _viewerAltitudeLevelAlpha);
        }

        private void OnDestroy()
        {
            _instance = null;
        }

        [ContextMenu("Regenerate mesh")]
        void RegenMesh()
        {
            _oceanBuilder.GenerateMesh(_baseVertDensity, _lodCount);
        }
#if UNITY_EDITOR
        [ContextMenu("Regenerate mesh", true)]
        bool RegenPossible() { return UnityEditor.EditorApplication.isPlaying; }
#endif

        public bool ScaleCouldIncrease { get { return _maxScale == -1f || transform.localScale.x < _maxScale * 0.99f; } }
        public bool ScaleCouldDecrease { get { return _minScale == -1f || transform.localScale.x > _minScale * 1.01f; } }

        public int GetLodIndex(float gridSize)
        {
            //gridSize = 4f * transform.lossyScale.x * Mathf.Pow(2f, result) / (4f * _baseVertDensity);
            int result = Mathf.RoundToInt(Mathf.Log((4f * _baseVertDensity) * gridSize / (4f * transform.lossyScale.x)) / Mathf.Log(2f));

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

        OceanBuilder _oceanBuilder;
        public OceanBuilder Builder { get { return _oceanBuilder; } }

        ICollProvider _collProvider; public ICollProvider CollisionProvider { get { return _collProvider ?? 
                    (_collProvider = _cachedCpuOceanQueries ? new CollProviderCache(_collProvider) as ICollProvider : new CollProviderDispTexs()); } }
    }
}
