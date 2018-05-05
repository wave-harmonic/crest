// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Scales the ocean horizontally based on the camera height, to keep geometry detail uniform-ish in screen space.
    /// </summary>
    public class OceanRenderer : MonoBehaviour
    {
        [Tooltip("Wind direction (angle from x axis in degrees)"), Range(-180, 180)]
        public float _windDirectionAngle = 0f;
        [Tooltip("Wind speed in m/s"), Range(0, 20), HideInInspector]
        public float _windSpeed = 5f;
        public Vector2 WindDir { get { return new Vector2(Mathf.Cos(Mathf.PI * _windDirectionAngle / 180f), Mathf.Sin(Mathf.PI * _windDirectionAngle / 180f)); } }

        [Range( 0, 15 )]
        [Tooltip( "Min number of verts / shape texels per wave" )]
        public float _minTexelsPerWave = 5f;

        [Delayed, Tooltip( "The smallest scale the ocean can be" )]
        public float _minScale = 16f;

        [Delayed, Tooltip( "The largest scale the ocean can be (-1 for unlimited)" )]
        public float _maxScale = 128f;

        [Tooltip("Scales horizontal displacement up and down."), Range(0f, 1f)]
        public float _chop = 1f;

        [Header( "Debug Params" )]
        [Tooltip("Smoothly transition geometry LODs")]
        public bool _enableSmoothLOD = true;
        [Tooltip( "Freeze wave shape in place but continues to move geom with camera, useful for hunting down pops" )]
        public bool _freezeTime = false;

        [Header( "Geometry Params" )]
        [SerializeField]
        [Delayed, Tooltip( "Side dimension in quads of an ocean tile." )]
        public float _baseVertDensity = 32f;
        [SerializeField, Tooltip( "Maximum wave amplitude, used to compute bounding box for ocean tiles." )]
        public float _maxWaveHeight = 30f;
        [SerializeField, Delayed, Tooltip( "Number of ocean tile scales/LODs to generate." ), ]
        int _lodCount = 6;
        [SerializeField]
        [Tooltip( "Whether to generate ocean geometry tiles uniformly (with overlaps)" )]
        bool _uniformTiles = false;
        [SerializeField]
        [Tooltip( "Generate a wide strip of triangles at the outer edge to extend ocean to edge of view frustum" )]
        bool _generateSkirt = true;

        // these have been useful for debug purposes (to freeze the water surface only)
        float _elapsedTime = 0f;
        public float ElapsedTime { get { return _elapsedTime; } }
        float _deltaTime = 0f;

        float _viewerAltitudeLevelAlpha = 0f;
        public float ViewerAltitudeLevelAlpha { get { return _viewerAltitudeLevelAlpha; } }

        public float SeaLevel { get { return transform.position.y; } }

        public static bool _acceptLargeWavelengthsInLastLOD = true;

        static OceanRenderer _instance;
        public static OceanRenderer Instance { get { return _instance ?? (_instance = FindObjectOfType<OceanRenderer>()); } }

        OceanBuilder _oceanBuilder;
        public OceanBuilder Builder { get { return _oceanBuilder; } }

        void Start()
        {
            _instance = this;

            _oceanBuilder = FindObjectOfType<OceanBuilder>();
            _oceanBuilder.GenerateMesh( MakeBuildParams() );
        }

        void LateUpdate()
        {
            _deltaTime = 0f;
            if( !_freezeTime )
            {
                _deltaTime = Time.deltaTime;
                _elapsedTime += _deltaTime;
            }

            // set global shader params
            Shader.SetGlobalVector( "_OceanCenterPosWorld", transform.position );
            Shader.SetGlobalFloat( "_MyTime", _elapsedTime );
            Shader.SetGlobalFloat( "_MyDeltaTime", _deltaTime );
            Shader.SetGlobalFloat( "_TexelsPerWave", _minTexelsPerWave );
            Shader.SetGlobalFloat("_Chop", _chop);
            Shader.SetGlobalFloat("_EnableSmoothLODs", _enableSmoothLOD ? 1f : 0f); // debug

            LateUpdateScale();
        }

        void LateUpdateScale()
        {
            // scale ocean mesh based on camera height to keep uniform detail
            const float HEIGHT_LOD_MUL = 2f;
            float camY = Mathf.Abs(Camera.main.transform.position.y - transform.position.y);
            float level = camY * HEIGHT_LOD_MUL;
            level = Mathf.Max(level, _minScale);
            if (_maxScale != -1f) level = Mathf.Min(level, 1.99f * _maxScale);

            float l2 = Mathf.Log(level) / Mathf.Log(2f);
            float l2f = Mathf.Floor(l2);

            _viewerAltitudeLevelAlpha = l2 - l2f;

            float newScale = Mathf.Pow(2f, l2f);
            transform.localScale = new Vector3(newScale, 1f, newScale);

            float maxWavelength = MaxWavelength(Builder.CurrentLodCount - 1);
            Shader.SetGlobalFloat("_MaxWavelength", _acceptLargeWavelengthsInLastLOD ? maxWavelength : 1e10f);
            Shader.SetGlobalFloat("_ViewerAltitudeLevelAlpha", _viewerAltitudeLevelAlpha);
        }

        OceanBuilder.Params MakeBuildParams()
        {
            return new OceanBuilder.Params
            {
                _baseVertDensity = _baseVertDensity,
                _lodCount = _lodCount,
                _maxWaveHeight = _maxWaveHeight,
                _forceUniformPatches = _uniformTiles,
                _generateSkirt = _generateSkirt,
            };
        }

        public void RegenMesh()
        {
            _oceanBuilder.GenerateMesh( MakeBuildParams() );
        }

        public float MaxWavelength(int lodIndex)
        {
            float oceanBaseScale = transform.lossyScale.x;
            float maxDiameter = 4f * oceanBaseScale * Mathf.Pow(2f, lodIndex);
            float maxTexelSize = maxDiameter / (4f * _baseVertDensity);
            return 2f * maxTexelSize * _minTexelsPerWave;
        }

        public bool ScaleCouldIncrease { get { return _maxScale == -1f || transform.localScale.x < _maxScale * 0.99f; } }
        public bool ScaleCouldDecrease { get { return _minScale == -1f || transform.localScale.x > _minScale * 1.01f; } }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.DrawIcon( transform.position, "Ocean" );
        }
#endif
    }
}
