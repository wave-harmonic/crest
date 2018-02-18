// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#define USING_DISPLACEMENT_TEXTURES

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Sets shader parameters for each geometry tile/chunk.
    /// </summary>
    public class OceanChunkRenderer : MonoBehaviour
    {
#if USING_DISPLACEMENT_TEXTURES
        Bounds _boundsLocal;
        Mesh _mesh;
#endif

        public bool _drawRenderBounds = false;

        int _lodIndex = -1;
        int _totalLodCount = -1;
        float _baseVertDensity = 32f;

        Renderer _rend;

        void Start()
        {
            _rend = GetComponent<Renderer>();

#if USING_DISPLACEMENT_TEXTURES
            _mesh = GetComponent<MeshFilter>().mesh;
            _boundsLocal = _mesh.bounds;
#endif
        }

        // Called when visible to a camera
        void OnWillRenderObject()
        {
            // per instance data

            // blend LOD 0 shape in/out to avoid pop, if the ocean might scale up later (it is smaller than its maximum scale)
            bool needToBlendOutShape = _lodIndex == 0 && OceanRenderer.Instance.ScaleCouldIncrease;
            float meshScaleLerp = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 0f;

            // blend furthest normals scale in/out to avoid pop, if scale could reduce
            bool needToBlendOutNormals = _lodIndex == _totalLodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease;
            float farNormalsWeight = needToBlendOutNormals ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            _rend.material.SetVector( "_InstanceData", new Vector4( meshScaleLerp, farNormalsWeight, _lodIndex ) );

            // geometry data
            float squareSize = transform.lossyScale.x / _baseVertDensity;
            float mul = 1.875f; // fudge 1
            float pow = 1.4f; // fudge 2
            float normalScrollSpeed0 = Mathf.Pow( Mathf.Log( 1f + 2f * squareSize ) * mul, pow );
            float normalScrollSpeed1 = Mathf.Pow( Mathf.Log( 1f + 4f * squareSize ) * mul, pow );
            _rend.material.SetVector( "_GeomData", new Vector4( squareSize, normalScrollSpeed0, normalScrollSpeed1, _baseVertDensity ) );

            // assign shape textures to shader
            // this relies on the render textures being init'd in CreateAssignRenderTexture::Awake().
            Camera[] shapeCams = OceanRenderer.Instance.Builder._shapeCameras;
            WaveDataCam wdc0 = shapeCams[_lodIndex].GetComponent<WaveDataCam>();
            wdc0.ApplyMaterialParams( 0, _rend.material );
            WaveDataCam wdc1 = (_lodIndex + 1) < shapeCams.Length ? shapeCams[_lodIndex + 1].GetComponent<WaveDataCam>() : null;
            if( wdc1 )
            {
                wdc1.ApplyMaterialParams( 1, _rend.material );
            }
            else
            {
                _rend.material.SetTexture( "_WD_Sampler_1", null );
            }

            // killing this as we use heightmaps now, not displacement textures, and im not sure if/when this will change
#if USING_DISPLACEMENT_TEXTURES
            // expand mesh bounds - bounds need to completely encapsulate verts after any dynamic displacement
            Bounds bounds = _boundsLocal;
            float boundsPadding = OceanRenderer.Instance._chop * OceanRenderer.Instance._maxWaveHeight;
            float expand = boundsPadding / transform.lossyScale.x;
            bounds.extents += new Vector3( expand, 0f, expand );
            _mesh.bounds = bounds;
#endif

            if( _drawRenderBounds )
                DebugDrawRendererBounds();
        }

        public void SetInstanceData( int lodIndex, int totalLodCount, float baseVertDensity )
        {
            _lodIndex = lodIndex; _totalLodCount = totalLodCount; _baseVertDensity = baseVertDensity;
        }

        public void DebugDrawRendererBounds()
        {
            // source: https://github.com/UnityCommunity/UnityLibrary
            // license: mit - https://github.com/UnityCommunity/UnityLibrary/blob/master/LICENSE.md

            // draws mesh renderer bounding box using Debug.Drawline

            var b = _rend.bounds;

            // bottom
            var p1 = new Vector3( b.min.x, b.min.y, b.min.z );
            var p2 = new Vector3( b.max.x, b.min.y, b.min.z );
            var p3 = new Vector3( b.max.x, b.min.y, b.max.z );
            var p4 = new Vector3( b.min.x, b.min.y, b.max.z );

            Debug.DrawLine( p1, p2, Color.blue );
            Debug.DrawLine( p2, p3, Color.red );
            Debug.DrawLine( p3, p4, Color.yellow );
            Debug.DrawLine( p4, p1, Color.magenta );

            // top
            var p5 = new Vector3( b.min.x, b.max.y, b.min.z );
            var p6 = new Vector3( b.max.x, b.max.y, b.min.z );
            var p7 = new Vector3( b.max.x, b.max.y, b.max.z );
            var p8 = new Vector3( b.min.x, b.max.y, b.max.z );

            Debug.DrawLine( p5, p6, Color.blue );
            Debug.DrawLine( p6, p7, Color.red );
            Debug.DrawLine( p7, p8, Color.yellow );
            Debug.DrawLine( p8, p5, Color.magenta );

            // sides
            Debug.DrawLine( p1, p5, Color.white );
            Debug.DrawLine( p2, p6, Color.gray );
            Debug.DrawLine( p3, p7, Color.green );
            Debug.DrawLine( p4, p8, Color.cyan );
        }
    }
}
