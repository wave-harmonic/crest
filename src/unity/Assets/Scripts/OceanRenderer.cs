// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Scales the ocean horizontally based on the camera height, to keep geometry detail uniform-ish in screen space.
    /// </summary>
    public class OceanRenderer : MonoBehaviour
    {
        [Range( 0, 15 )]
        [Tooltip( "Min number of verts / shape texels per wave" )]
        public float _minTexelsPerWave = 5f;

        [Delayed, Tooltip( "The smallest scale the ocean can be" )]
        public float _minScale = 16f;

        [Delayed, Tooltip( "The largest scale the ocean can be (-1 for unlimited)" )]
        public float _maxScale = 128f;

        [Header( "Debug Params" )]
        [Tooltip("Smoothly transition geometry LODs")]
        public bool _enableSmoothLOD = true;
        [Tooltip( "Freeze wave shape in place but continues to move geom with camera, useful for hunting down pops" )]
        public bool _freezeTime = false;
        [Tooltip( "Use debug colours to show where shape is sampled from" )]
        public bool _visualiseLODs = false;

        [Header( "Geometry Params" )]
        [SerializeField]
        [Tooltip( "Side dimension in quads of an ocean tile." )]
        public float _baseVertDensity = 32f;
        [SerializeField]
        [Tooltip( "Maximum wave amplitude, used to compute bounding box for ocean tiles." )]
        float _maxWaveHeight = 30f;
        [SerializeField]
        [Tooltip( "Number of ocean tile scales/LODs to generate." )]
        public int _lodCount = 5;
        [SerializeField]
        [Tooltip( "Whether to generate ocean geometry tiles uniformly (with overlaps)" )]
        bool _uniformTiles = false;
        [SerializeField]
        [Tooltip( "Generate a wide strip of triangles at the outer edge to extend ocean to edge of view frustum" )]
        bool _generateSkirt = true;

        // these have been useful for debug purposes (to freeze the water surface only)
        float _elapsedTime = 0f;
        float _deltaTime = 0f;

        float _viewerAltitudeLevelAlpha = 0f;
        public float ViewerAltitudeLevelAlpha { get { return _viewerAltitudeLevelAlpha; } }

        public static bool _kinematicWaves = false;

        static OceanRenderer _instance;
        public static OceanRenderer Instance { get { return _instance ?? (_instance = FindObjectOfType<OceanRenderer>()); } }

        OceanBuilder _oceanBuilder;
        public OceanBuilder Builder { get { return _oceanBuilder; } }

        void Start()
        {
            _instance = this;

            _oceanBuilder = FindObjectOfType<OceanBuilder>();
            _oceanBuilder.GenerateMesh( MakeBuildParams() );

            SetSmoothLODsShaderParam();
        }

        void LateUpdate()
        {
            _deltaTime = 0f;
            if( !_freezeTime )
            {
                // hack - force simulation to occur at 60fps. this is because the sim stores last and previous values - velocity
                // is implicit and time step is assumed to be constant
                _deltaTime = 1f / 60f; // Time.deltaTime
                _elapsedTime += _deltaTime;
            }

            // set global shader params
            Shader.SetGlobalVector( "_OceanCenterPosWorld", transform.position );
            Shader.SetGlobalFloat( "_MyTime", _elapsedTime );
            Shader.SetGlobalFloat( "_MyDeltaTime", _deltaTime );
            Shader.SetGlobalFloat( "_TexelsPerWave", _minTexelsPerWave );
            Shader.SetGlobalFloat( "_VisualiseLODs", _visualiseLODs ? 1f : 0f );
            Shader.SetGlobalFloat( "_KinematicWaves", _kinematicWaves ? 1f : 0f );

            // scale ocean mesh based on camera height to keep uniform detail
            const float HEIGHT_LOD_MUL = 1f; //0.0625f;
            float camY = Mathf.Abs( Camera.main.transform.position.y - transform.position.y );
            float level = camY * HEIGHT_LOD_MUL;
            level = Mathf.Max( level, _minScale );
            if( _maxScale != -1f ) level = Mathf.Min( level, 1.99f * _maxScale );

            float l2 = Mathf.Log( level ) / Mathf.Log( 2f );
            float l2f = Mathf.Floor( l2 );

            _viewerAltitudeLevelAlpha = l2 - l2f;

            float newScale = Mathf.Pow( 2f, l2f );

            float currentScale = Mathf.Abs( transform.localScale.x );

            if( !Mathf.Approximately( newScale, currentScale ) )
            {
                ShapeWaveSim.Instance.OnOceanScaleChange( newScale < currentScale );
            }

            // sign is used to mirror ocean geometry. without this, gaps can appear between ocean tiles.
            // this is due to the difference in direction of floor/modulus in the ocean vert shader, and the
            // way the ocean geometry tiles have tri strips removed/added.
            transform.localScale = new Vector3( Mathf.Sign( transform.position.x ) * newScale, 1f, Mathf.Sign( transform.position.z ) * newScale );
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

        public void SetSmoothLODsShaderParam()
        {
            Shader.SetGlobalFloat( "_EnableSmoothLODs", _enableSmoothLOD ? 1f : 0f ); // debug
        }

        public bool ScaleCouldIncrease { get { return _maxScale == -1f || Mathf.Abs( transform.localScale.x ) < _maxScale * 0.99f; } }
        public bool ScaleCouldDecrease { get { return _minScale == -1f || Mathf.Abs( transform.localScale.x ) > _minScale * 1.01f; } }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.DrawIcon( transform.position, "Ocean" );
        }
#endif
    }
}
