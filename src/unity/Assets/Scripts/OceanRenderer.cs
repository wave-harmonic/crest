// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace OceanResearch
{
    /// <summary>
    /// Scales the ocean horizontally based on the camera height, to keep geometry detail uniform-ish in screen space.
    /// </summary>
    public class OceanRenderer : MonoBehaviour
    {
        public bool _scaleHoriz = true;
        public bool _scaleHorizSmoothTransition = true;

        [Range( 0, 15 )]
        [Tooltip( "Min number of verts / shape texels per wave" )]
        public float _minTexelsPerWave = 5f;

        [Delayed]
        [Tooltip( "The scale of the ocean is clamped at this value to prevent the ocean being scaled too small when approached by the camera." )]
        public float _minScale = 128f;

        public float _maxScale = -1f;

        [Header( "Debug Params" )]
        [Tooltip("Smoothly transition geometry LODs")]
        public bool _enableSmoothLOD = true;
        [Tooltip( "Freeze wave shape in place but continues to move geom with camera, useful for hunting down pops" )]
        public bool _freezeTime = false;
        float _elapsedTime = 0f;

        [Header( "Geometry Params" )]
        [SerializeField]
        [Tooltip( "Side dimension in quads of an ocean tile." )]
        float _baseVertDensity = 32f;
        [SerializeField]
        [Tooltip( "Maximum wave amplitude, used to compute bounding box for ocean tiles." )]
        float _maxWaveHeight = 30f;
        [SerializeField]
        [Tooltip( "Number of ocean tile scales/LODs to generate." )]
        int _lodCount = 5;
        [SerializeField]
        [Tooltip( "Whether to generate ocean geometry tiles uniformly (with overlaps)" )]
        bool _uniformTiles = false;
        [SerializeField]
        [Tooltip( "Generate a wide strip of triangles at the outer edge to extend ocean to edge of view frustum" )]
        bool _generateSkirt = true;

        float _viewerAltitudeLevelAlpha = 0f;
        public float ViewerAltitudeLevelAlpha { get { return _viewerAltitudeLevelAlpha; } }

        static OceanRenderer _instance;
        public static OceanRenderer Instance { get { return _instance != null ? _instance : (_instance = FindObjectOfType<OceanRenderer>()); } }

        OceanBuilder _oceanBuilder;
        public OceanBuilder Builder { get { return _oceanBuilder; } }

        void Start()
        {
            _instance = this;

            _oceanBuilder = GetComponent<OceanBuilder>();
            _oceanBuilder.GenerateMesh( MakeBuildParams() );

            SetSmoothLODsShaderParam();
        }

        void LateUpdate()
        {
            if( !_freezeTime )
            {
                _elapsedTime += Time.deltaTime;
            }

            // set global shader params
            Shader.SetGlobalVector( "_OceanCenterPosWorld", transform.position );
            Shader.SetGlobalFloat( "_MyTime", _elapsedTime );
            Shader.SetGlobalFloat( "_TexelsPerWave", _minTexelsPerWave );

            // scale ocean mesh based on camera height to keep uniform detail
            const float HEIGHT_LOD_MUL = 1f; //0.0625f;
            float camY = Mathf.Abs( Camera.main.transform.position.y - transform.position.y );
            float level = camY * HEIGHT_LOD_MUL;
            level = Mathf.Max( level, _minScale );
            if( _maxScale != -1f ) level = Mathf.Min( level, 1.99f * _maxScale );
            if( !_scaleHoriz ) level = _minScale;

            float l2 = Mathf.Log( level ) / Mathf.Log( 2f );
            float l2f = Mathf.Floor( l2 );

            _viewerAltitudeLevelAlpha = _scaleHorizSmoothTransition ? l2 - l2f : 0f;

            float scale = Mathf.Pow( 2f, l2f );

            transform.localScale = new Vector3( Mathf.Sign( transform.position.x ) * scale, 1f, Mathf.Sign( transform.position.z ) * scale );
        }

        void OnGUI()
        {
            GUI.skin.toggle.normal.textColor = Color.black;
            GUI.skin.label.normal.textColor = Color.black;

            _freezeTime = GUI.Toggle( new Rect( 0, 25, 100, 25 ), _freezeTime, "Freeze waves" );

            GUI.changed = false;
            _enableSmoothLOD = GUI.Toggle( new Rect( 0, 50, 150, 25 ), _enableSmoothLOD, "Enable smooth LOD" );
            if( GUI.changed ) SetSmoothLODsShaderParam();

            _minTexelsPerWave = GUI.HorizontalSlider( new Rect( 0, 100, 150, 25 ), _minTexelsPerWave, 0, 15 );
            GUI.Label( new Rect( 0, 75, 150, 25 ), string.Format("Min verts per wave: {0}", _minTexelsPerWave.ToString("0.00") ) );
        }

        OceanBuilder.Params MakeBuildParams()
        {
            OceanBuilder.Params parms = new OceanBuilder.Params();
            parms._baseVertDensity = _baseVertDensity;
            parms._lodCount = _lodCount;
            parms._maxWaveHeight = _maxWaveHeight;
            parms._forceUniformPatches = _uniformTiles;
            parms._generateSkirt = _generateSkirt;
            return parms;
        }

        public void RegenMesh()
        {
            _oceanBuilder.GenerateMesh( MakeBuildParams() );
        }

        void SetSmoothLODsShaderParam()
        {
            Shader.SetGlobalFloat( "_EnableSmoothLODs", _enableSmoothLOD ? 1f : 0f ); // debug
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.DrawIcon( transform.position, "Ocean" );
        }
#endif
    }
}
