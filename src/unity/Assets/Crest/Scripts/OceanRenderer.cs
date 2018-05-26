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

        [Header( "Geometry Params" )]
        [SerializeField, Delayed, Tooltip( "Side dimension in quads of an ocean tile." )]
        public float _baseVertDensity = 32f;
        [SerializeField, Delayed, Tooltip( "Number of ocean tile scales/LODs to generate." ), ]
        int _lodCount = 6;

        [Header("Debug Params")]
        [Tooltip("Freeze wave shape in place but continues to move geom with camera, useful for hunting down pops")]
        public bool _freezeTime = false;
        [Tooltip("Whether to generate ocean geometry tiles uniformly (with overlaps)")]
        public bool _uniformTiles = false;
        [Tooltip("Disable generating a wide strip of triangles at the outer edge to extend ocean to edge of view frustum")]
        public bool _disableSkirt = false;

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
            _oceanBuilder.GenerateMesh(_baseVertDensity, _lodCount);
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
            Shader.SetGlobalVector("_WindDirXZ", WindDir);
            Shader.SetGlobalFloat("_SeaLevel", SeaLevel);

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

            float maxWavelength = Builder._shapeWDCs[Builder._shapeWDCs.Length - 1].MaxWavelength();
            Shader.SetGlobalFloat("_MaxWavelength", _acceptLargeWavelengthsInLastLOD ? maxWavelength : 1e10f);
            Shader.SetGlobalFloat("_ViewerAltitudeLevelAlpha", _viewerAltitudeLevelAlpha);
        }

        private void OnDestroy()
        {
            _instance = null;
        }

        public void RegenMesh()
        {
            _oceanBuilder.GenerateMesh(_baseVertDensity, _lodCount);
        }

        public bool ScaleCouldIncrease { get { return _maxScale == -1f || transform.localScale.x < _maxScale * 0.99f; } }
        public bool ScaleCouldDecrease { get { return _minScale == -1f || transform.localScale.x > _minScale * 1.01f; } }

        /// <summary>
        /// Returns index of lod that completely covers the sample area, and contains wavelengths that repeat no more than twice across the smaller
        /// spatial length. If no such lod available, returns -1. This means high frequency wavelengths are filtered out, and the lod index can
        /// be used for each sample in the sample area.
        /// </summary>
        public static int SuggestCollisionLOD(Rect sampleAreaXZ)
        {
            return SuggestCollisionLOD(sampleAreaXZ, Mathf.Min(sampleAreaXZ.width, sampleAreaXZ.height));
        }

        public static int SuggestCollisionLOD(Rect sampleAreaXZ, float minSpatialLength)
        {
            var wdcs = Instance.Builder._shapeWDCs;
            for (int lod = 0; lod < wdcs.Length; lod++)
            {
                // shape texture needs to completely contain sample area
                var wdc = wdcs[lod];
                var wdcRect = wdc.CollisionDataRectXZ;
                if (!wdcRect.Contains(sampleAreaXZ.min) || !wdcRect.Contains(sampleAreaXZ.max))
                    continue;

                // the smallest wavelengths should repeat no more than twice across the smaller spatial length
                var minWL = wdc.MaxWavelength() / 2f;
                if (minWL < minSpatialLength / 2f)
                    continue;

                return lod;
            }

            return -1;
        }

        /// <summary>
        /// Samples ocean surface displacement at a point. Searches for most detailed LOD available at the query position.
        /// </summary>
        public static bool SampleDisplacement(ref Vector3 worldPos, ref Vector3 displacement)
        {
            int lod = SuggestCollisionLOD(new Rect(worldPos.x, worldPos.z, 0f, 0f), 0f);
            return Instance.Builder._shapeWDCs[lod].SampleDisplacement(ref worldPos, ref displacement);
        }

        /// <summary>
        /// Samples ocean surface height at a point. Searches for most detailed LOD available at the query position.
        /// </summary>
        public static bool SampleHeight(ref Vector3 worldPos, ref float height)
        {
            int lod = SuggestCollisionLOD(new Rect(worldPos.x, worldPos.z, 0f, 0f), 0f);
            if (lod == -1) return false;
            height = Instance.Builder._shapeWDCs[lod].GetHeight(ref worldPos);
            return true;
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

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.DrawIcon( transform.position, "Ocean" );
        }
#endif
    }
}
