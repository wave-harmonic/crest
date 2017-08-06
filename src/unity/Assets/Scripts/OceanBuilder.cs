// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

//#define PROFILE_CONSTRUCTION

using UnityEngine;
using System.Collections;

namespace Crest
{
    /// <summary>
    /// Instantiates all the ocean geometry, as a set of tiles.
    /// </summary>
    public class OceanBuilder : MonoBehaviour
    {
        [Tooltip("Prefab for an ocean patch.")]
	    public Transform _chunkPrefab;
        [Tooltip("Prefab for a camera that renders ocean shape.")]
        public Transform _shapeCameraPrefab;

        [HideInInspector]
        public Camera[] _shapeCameras;

        /// <summary>
        /// Parameters to use for ocean geometry construction
        /// </summary>
        public class Params
        {
            public float _baseVertDensity = 32f;
            public float _maxWaveHeight = 30f;
            public int _lodCount = 5;
            public bool _forceUniformPatches = false;
            public bool _generateSkirt = true;
        }

        // The following apply to BASE_VERT_DENSITY = 2. The ocean mesh is built up from these patches. Rotational symmetry is
        // used where possible to eliminate combinations. The slim variants are used to eliminate overlap between patches.
        enum PatchType
        {
            /// <summary>
            /// Adds no skirt. Used in interior of highest detail lod (0)
            /// 
            ///    1 -------
            ///      |  |  |
            ///  z   -------
            ///      |  |  |
            ///    0 -------
            ///      0     1
            ///         x
            /// 
            /// </summary>
            Interior,

            /// <summary>
            /// Adds a full skirt all of the way arond a patch
            /// 
            ///      -------------
            ///      |  |  |  |  |
            ///    1 -------------
            ///      |  |  |  |  |
            ///  z   -------------
            ///      |  |  |  |  |
            ///    0 -------------
            ///      |  |  |  |  |
            ///      -------------
            ///         0     1
            ///            x
            ///         
            /// </summary>
            Fat,

            /// <summary>
            /// Adds a skirt on the right hand side of the patch
            /// 
            ///    1 ----------
            ///      |  |  |  |
            ///  z   ----------
            ///      |  |  |  |
            ///    0 ----------
            ///      0     1
            ///         x
            ///         
            /// </summary>
            FatX,

            /// <summary>
            /// Adds a skirt on the right hand side of the patch, removes skirt from top
            /// </summary>
            FatXSlimZ,

            /// <summary>
            /// Outer most side - this adds an extra skirt on the left hand side of the patch,
            /// which will point outwards and be extended to Zfar
            /// 
            ///    1 --------------------------------------------------------------------------------------
            ///      |  |  |                                                                              |
            ///  z   --------------------------------------------------------------------------------------
            ///      |  |  |                                                                              |
            ///    0 --------------------------------------------------------------------------------------
            ///      0     1
            ///         x
            ///         
            /// </summary>
            FatXOuter,

            /// <summary>
            /// Adds skirts at the top and right sides of the patch
            /// </summary>
            FatXZ,

            /// <summary>
            /// Adds skirts at the top and right sides of the patch and pushes them to horizon
            /// </summary>
            FatXZOuter,

            /// <summary>
            /// One less set of verts in x direction
            /// </summary>
            SlimX,

            /// <summary>
            /// One less set of verts in both x and z directions
            /// </summary>
            SlimXZ,

            /// <summary>
            /// One less set of verts in x direction, extra verts at start of z direction
            /// 
            ///      ----
            ///      |  |
            ///    1 ----
            ///      |  |
            ///  z   ----
            ///      |  |
            ///    0 ----
            ///      0     1
            ///         x
            ///
            /// </summary>
            SlimXFatZ,

            /// <summary>
            /// Number of patch types
            /// </summary>
            Count,
        }

        public void GenerateMesh( Params parms )
        {
            if( parms._lodCount < 1 )
            {
                Debug.LogError( "Invalid LOD count: " + parms._lodCount.ToString(), this );
                return;
            }

#if UNITY_EDITOR
            if( !UnityEditor.EditorApplication.isPlaying )
            {
                Debug.LogError( "Ocean mesh meant to be (re)generated in play mode", this );
                return;
            }
#endif

#if PROFILE_CONSTRUCTION
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif

            // create mesh data
            Mesh[] meshInsts = new Mesh[(int)PatchType.Count];
            for( int i = 0; i < (int)PatchType.Count; i++ )
                meshInsts[i] = BuildOceanPatch( (PatchType)i, parms );

            // create the shape cameras
            var scs = new Transform[parms._lodCount];
            _shapeCameras = new Camera[parms._lodCount];
            for( int i = 0; i < parms._lodCount; i++ )
            {
                scs[i] = Instantiate( _shapeCameraPrefab ) as Transform;
                _shapeCameras[i] = scs[i].GetComponent<Camera>();
                var wdc = _shapeCameras[i].GetComponent<WaveDataCam>();
                wdc._lodIndex = i;
                wdc._lodCount = parms._lodCount;
                var cart = _shapeCameras[i].GetComponent<CreateAssignRenderTexture>();
                cart._targetName = "shapeRT" + i.ToString();
                cart._width = cart._height = (int)(4f * parms._baseVertDensity);
            }

            int startLevel = 0;
            for( int i = 0; i < parms._lodCount; i++ )
            {
                bool biggestLOD = i == parms._lodCount - 1;
                GameObject nextLod = CreateLOD( i, biggestLOD, meshInsts, parms );
                nextLod.transform.parent = transform;

                // scale only horizontally, otherwise culling bounding box will be scaled up in y
                float horizScale = Mathf.Pow( 2f, (float)(i + startLevel) );
                nextLod.transform.localScale = new Vector3( horizScale, 1f, horizScale );
            }

#if PROFILE_CONSTRUCTION
            sw.Stop();
            Debug.Log( "Finished generating " + parms._lodCount.ToString() + " LODs, time: " + (1000.0*sw.Elapsed.TotalSeconds).ToString(".000") + "ms" );
#endif
        }

        Mesh BuildOceanPatch( PatchType pt, Params parms )
        {
            ArrayList verts = new ArrayList();
            ArrayList indices = new ArrayList();

            // stick a bunch of verts into a 1m x 1m patch (scaling happens later)
            float dx = 1f / parms._baseVertDensity;


            //////////////////////////////////////////////////////////////////////////////////
            // verts

            // see comments within PatchType for diagrams of each patch mesh

            // skirt widths on left, right, bottom and top (in order)
            float skirtXminus = 0f, skirtXplus = 0f;
            float skirtZminus = 0f, skirtZplus = 0f;
            // set the patch size
            if( pt == PatchType.Fat ) { skirtXminus = skirtXplus = skirtZminus = skirtZplus = 1f; }
            else if( pt == PatchType.FatX || pt == PatchType.FatXOuter ) { skirtXplus = 1f; }
            else if( pt == PatchType.FatXZ || pt == PatchType.FatXZOuter ) { skirtXplus = skirtZplus = 1f; }
            else if( pt == PatchType.FatXSlimZ ) { skirtXplus = 1f; skirtZplus = -1f; }
            else if( pt == PatchType.SlimX ) { skirtXplus = -1f; }
            else if( pt == PatchType.SlimXZ ) { skirtXplus = skirtZplus = -1f; }
            else if( pt == PatchType.SlimXFatZ ) { skirtXplus = -1f; skirtZplus = 1f; }

            float sideLength_verts_x = 1f + parms._baseVertDensity + skirtXminus + skirtXplus;
            float sideLength_verts_z = 1f + parms._baseVertDensity + skirtZminus + skirtZplus;

            float start_x = -0.5f - skirtXminus * dx;
            float start_z = -0.5f - skirtZminus * dx;
            float   end_x =  0.5f + skirtXplus * dx;
            float   end_z =  0.5f + skirtZplus * dx;

            for( float j = 0; j < sideLength_verts_z; j++ )
            {
                // interpolate z across patch
                float z = Mathf.Lerp( start_z, end_z, j / (sideLength_verts_z - 1f) );

                // push outermost edge out to horizon
                if( pt == PatchType.FatXZOuter && j == sideLength_verts_z - 1f )
                    z *= 100f;

                for( float i = 0; i < sideLength_verts_x; i++ )
                {
                    // interpolate x across patch
                    float x = Mathf.Lerp( start_x, end_x, i / (sideLength_verts_x - 1f) );

                    // push outermost edge out to horizon
                    if( i == sideLength_verts_x - 1f && (pt == PatchType.FatXOuter || pt == PatchType.FatXZOuter) )
                        x *= 100f;

                    // could store something in y, although keep in mind this is a shared mesh that is shared across multiple lods
                    verts.Add( new Vector3( x, 0f, z ) );
                }
            }


            //////////////////////////////////////////////////////////////////////////////////
            // indices

            int sideLength_squares_x = (int)sideLength_verts_x - 1;
            int sideLength_squares_z = (int)sideLength_verts_z - 1;

            for( int j = 0; j < sideLength_squares_z; j++ )
            {
                for( int i = 0; i < sideLength_squares_x; i++ )
                {
                    bool flipEdge = false;

                    if( i % 2 == 1 ) flipEdge = !flipEdge;
                    if( j % 2 == 1 ) flipEdge = !flipEdge;

                    int i0 = i + j * (sideLength_squares_x + 1);
                    int i1 = i0 + 1;
                    int i2 = i0 + (sideLength_squares_x + 1);
                    int i3 = i2 + 1;

                    if( !flipEdge )
                    {
                        // tri 1
                        indices.Add( i3 );
                        indices.Add( i1 );
                        indices.Add( i0 );

                        // tri 2
                        indices.Add( i0 );
                        indices.Add( i2 );
                        indices.Add( i3 );
                    }
                    else
                    {
                        // tri 1
                        indices.Add( i3 );
                        indices.Add( i1 );
                        indices.Add( i2 );

                        // tri 2
                        indices.Add( i0 );
                        indices.Add( i2 );
                        indices.Add( i1 );
                    }
                }
            }


            //////////////////////////////////////////////////////////////////////////////////
            // create mesh

            Mesh mesh = new Mesh();
            if( verts != null && verts.Count > 0 )
            {
                Vector3[] arrV = new Vector3[verts.Count];
                verts.CopyTo( arrV );

                int[] arrI = new int[indices.Count];
                indices.CopyTo( arrI );

                mesh.SetIndices( null, MeshTopology.Triangles, 0 );
                mesh.vertices = arrV;
                mesh.normals = null;
                mesh.SetIndices( arrI, MeshTopology.Triangles, 0 );

                // make sure bounds allows for verts to be pulled around by snapping, and also contains max possible y displacement
                mesh.RecalculateBounds();
                Bounds bounds = mesh.bounds;
                bounds.extents = new Vector3( bounds.extents.x + dx, parms._maxWaveHeight, bounds.extents.z + dx );
                mesh.bounds = bounds;

                mesh.name = pt.ToString();
            }

            return mesh;
        }

        GameObject CreateLOD( int lodIndex, bool biggestLOD, Mesh[] meshData, Params parms )
        {
            // first create parent gameobject for the lod level. the scale of this transform sets the size of the lod.

            string lodParentName = "LOD" + lodIndex;

            // if it exists already, destroy it so it can be created fresh
            Transform parentTransform = transform.Find( lodParentName );
            if( parentTransform != null )
            {
                DestroyImmediate( parentTransform.gameObject );
                parentTransform = null;
            }

            GameObject parent = new GameObject();
            parent.name = "LOD" + lodIndex;
            parent.transform.parent = transform;
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localRotation = Quaternion.identity;

            // add a shape camera below it
            _shapeCameras[lodIndex].transform.parent = parent.transform;
            _shapeCameras[lodIndex].transform.localScale = Vector3.one;
            _shapeCameras[lodIndex].transform.localPosition = Vector3.zero;

            bool generateSkirt = parms._generateSkirt && biggestLOD;

            Vector2[] offsets;
            PatchType[] patchTypes;

            PatchType leadSideType = generateSkirt ? PatchType.FatXOuter : PatchType.SlimX;
            PatchType trailSideType = generateSkirt ? PatchType.FatXOuter : PatchType.FatX;
            PatchType leadCornerType = generateSkirt ? PatchType.FatXZOuter : PatchType.SlimXZ;
            PatchType trailCornerType = generateSkirt ? PatchType.FatXZOuter : PatchType.FatXZ;
            PatchType tlCornerType = generateSkirt ? PatchType.FatXZOuter : PatchType.SlimXFatZ;
            PatchType brCornerType = generateSkirt ? PatchType.FatXZOuter : PatchType.FatXSlimZ;

            if( lodIndex != 0 )
            {
                // instance indices:
                //    0  1  2  3
                //    4        5
                //    6        7
                //    8  9  10 11
                offsets = new Vector2[] {
                    new Vector2(-1.5f,1.5f),    new Vector2(-0.5f,1.5f),    new Vector2(0.5f,1.5f),     new Vector2(1.5f,1.5f),
                    new Vector2(-1.5f,0.5f),                                                            new Vector2(1.5f,0.5f),
                    new Vector2(-1.5f,-0.5f),                                                           new Vector2(1.5f,-0.5f),
                    new Vector2(-1.5f,-1.5f),   new Vector2(-0.5f,-1.5f),   new Vector2(0.5f,-1.5f),    new Vector2(1.5f,-1.5f),
                };

                // usually rings have an extra side of verts that point inwards. the outermost ring has both the inward
                // verts and also and additional outwards set of verts that go to the horizon
                patchTypes = new PatchType[] {
                    tlCornerType,         leadSideType,           leadSideType,         leadCornerType,
                    trailSideType,                                                      leadSideType,
                    trailSideType,                                                      leadSideType,
                    trailCornerType,      trailSideType,          trailSideType,        brCornerType,
                };
            }
            else
            {
                // first LOD has inside bit as well:
                //    0  1  2  3
                //    4  5  6  7
                //    8  9  10 11
                //    12 13 14 15
                offsets = new Vector2[] {
                    new Vector2(-1.5f,1.5f),    new Vector2(-0.5f,1.5f),    new Vector2(0.5f,1.5f),     new Vector2(1.5f,1.5f),
                    new Vector2(-1.5f,0.5f),    new Vector2(-0.5f,0.5f),    new Vector2(0.5f,0.5f),     new Vector2(1.5f,0.5f),
                    new Vector2(-1.5f,-0.5f),   new Vector2(-0.5f,-0.5f),   new Vector2(0.5f,-0.5f),    new Vector2(1.5f,-0.5f),
                    new Vector2(-1.5f,-1.5f),   new Vector2(-0.5f,-1.5f),   new Vector2(0.5f,-1.5f),    new Vector2(1.5f,-1.5f),
                };


                // all interior - the "side" types have an extra skirt that points inwards - this means that this inner most
                // section doesn't need any skirting. this is good - this is the highest density part of the mesh.
                patchTypes = new PatchType[] {
                    tlCornerType,       leadSideType,           leadSideType,           leadCornerType,
                    trailSideType,      PatchType.Interior,     PatchType.Interior,     leadSideType,
                    trailSideType,      PatchType.Interior,     PatchType.Interior,     leadSideType,
                    trailCornerType,    trailSideType,          trailSideType,          brCornerType,
                };
            }

            // debug toggle to force all patches to be the same. they'll be made with a surrounding skirt to make sure patches
            // overlap
            if( parms._forceUniformPatches )
            {
                for( int i = 0; i < patchTypes.Length; i++ )
                {
                    patchTypes[i] = PatchType.Fat;
                }
            }

            // create the ocean patches
            for( int i = 0; i < offsets.Length; i++ )
            {
                // instantiate and place patch
                Transform inst = Instantiate( _chunkPrefab ) as Transform;
                inst.parent = parent.transform;
                Vector2 pos = offsets[i];
                inst.localPosition = new Vector3( pos.x, 0f, pos.y );
                inst.localScale = Vector3.one;

                OceanChunkRenderer ocr = inst.GetComponent<OceanChunkRenderer>();
                ocr.SetInstanceData( lodIndex, parms._lodCount, parms._baseVertDensity );

                inst.GetComponent<MeshFilter>().mesh = meshData[(int)patchTypes[i]];

                // rotate side patches to point the +x side outwards
                bool rotateXOutwards = patchTypes[i] == PatchType.FatX || patchTypes[i] == PatchType.FatXOuter || patchTypes[i] == PatchType.SlimX || patchTypes[i] == PatchType.SlimXFatZ;
                if( rotateXOutwards )
                {
                    if( Mathf.Abs( pos.y ) >= Mathf.Abs( pos.x ) )
                        inst.localEulerAngles = -Vector3.up * 90f * Mathf.Sign( pos.y );
                    else
                        inst.localEulerAngles = pos.x < 0f ? Vector3.up * 180f : Vector3.zero;
                }

                // rotate the corner patches so the +x and +z sides point outwards
                bool rotateXZOutwards = patchTypes[i] == PatchType.FatXZ || patchTypes[i] == PatchType.SlimXZ || patchTypes[i] == PatchType.FatXSlimZ || patchTypes[i] == PatchType.FatXZOuter;
                if( rotateXZOutwards )
                {
                    // xz direction before rotation
                    Vector3 from = new Vector3( 1f, 0f, 1f ).normalized;
                    // target xz direction is outwards vector given by local patch position - assumes this patch is a corner (checked below)
                    Vector3 to = inst.localPosition.normalized;
                    if( Mathf.Abs( inst.localPosition.x ) < 0.0001f || Mathf.Abs( Mathf.Abs( inst.localPosition.x ) - Mathf.Abs( inst.localPosition.z ) ) > 0.001f )
                    {
                        Debug.LogWarning( "Skipped rotating a patch because it isn't a corner, click here to highlight.", inst );
                        continue;
                    }

                    // detect 180 degree rotations as it doesnt always rotate around Y
                    if( Vector3.Dot( from, to ) < -0.99f )
                        inst.localEulerAngles = Vector3.up * 180f;
                    else
                        inst.localRotation = Quaternion.FromToRotation( from, to );
                }
            }

            return parent;
        }
    }
}
