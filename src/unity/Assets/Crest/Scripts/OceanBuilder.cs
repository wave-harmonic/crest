// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

//#define PROFILE_CONSTRUCTION

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Crest
{
    /// <summary>
    /// Instantiates all the ocean geometry, as a set of tiles.
    /// </summary>
    public class OceanBuilder : MonoBehaviour
    {
        [SerializeField, Tooltip("Material to use for the ocean surface")]
        Material _oceanMaterial;

        [HideInInspector]
        public LodDataAnimatedWaves[] _lodDataAnimWaves;

        [HideInInspector]
        public Camera[] _camsAnimWaves;
        [HideInInspector]
        public Camera[] _camsFoam;
        [HideInInspector]
        public Camera[] _camsDynWaves;
        [HideInInspector]
        public Camera[] _camsDynWavesNew;

        [Header("Simulations")]
        public bool _createFoamSim = true;
        public SimSettingsFoam _simSettingsFoam;
        public bool _createDynamicWaveSim = false;
        public SimSettingsWave _simSettingsDynamicWaves;
        public bool _createDynamicWaveSimNew = false;

        public int CurrentLodCount { get { return _camsAnimWaves.Length; } }

        // The comments below illustrate casse when BASE_VERT_DENSITY = 2. The ocean mesh is built up from these patches. Rotational symmetry
        // is used where possible to eliminate combinations. The slim variants are used to eliminate overlap between patches.
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

        public void GenerateMesh(float baseVertDensity, int lodCount)
        {
            if (lodCount < 1)
            {
                Debug.LogError( "Invalid LOD count: " + lodCount.ToString(), this );
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
            for (int i = 0; i < (int)PatchType.Count; i++)
            {
                meshInsts[i] = BuildOceanPatch((PatchType)i, baseVertDensity);
            }

            // create the shape cameras
            _camsAnimWaves = new Camera[lodCount];
            _lodDataAnimWaves = new LodDataAnimatedWaves[lodCount];
            _camsFoam = new Camera[lodCount];
            _camsDynWaves = new Camera[lodCount];
            _camsDynWavesNew = new Camera[lodCount];

            var cachedSettings = new Dictionary<System.Type, SimSettingsBase>();
            if (_simSettingsFoam != null) cachedSettings.Add(typeof(LodDataFoam), _simSettingsFoam);
            if (_simSettingsDynamicWaves != null)
            {
                cachedSettings.Add(typeof(LodDataDynamicWaves), _simSettingsDynamicWaves);
                cachedSettings.Add(typeof(LodDataDynamicWavesNew), _simSettingsDynamicWaves);
            }

            for ( int i = 0; i < lodCount; i++ )
            {
                {
                    var go = LodData.CreateLodData(i, lodCount, baseVertDensity, LodData.SimType.AnimatedWaves, cachedSettings);
                    _camsAnimWaves[i] = go.GetComponent<Camera>();
                    _lodDataAnimWaves[i] = go.GetComponent<LodDataAnimatedWaves>();
                }

                if (_createFoamSim)
                {
                    var go = LodData.CreateLodData(i, lodCount, baseVertDensity, LodData.SimType.Foam, cachedSettings);
                    _camsFoam[i] = go.GetComponent<Camera>();
                }

                if (_createDynamicWaveSim)
                {
                    var go = LodData.CreateLodData(i, lodCount, baseVertDensity, LodData.SimType.DynamicWaves, cachedSettings);
                    _camsDynWaves[i] = go.GetComponent<Camera>();
                }

                if (_createDynamicWaveSimNew)
                {
                    var go = LodData.CreateLodData(i, lodCount, baseVertDensity, LodData.SimType.DynamicWavesNew, cachedSettings);
                    _camsDynWavesNew[i] = go.GetComponent<Camera>();
                }
            }

            // remove existing LODs
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("LOD"))
                {
                    child.parent = null;
                    Destroy(child.gameObject);
                    i--;
                }
            }

            int startLevel = 0;
            for( int i = 0; i < lodCount; i++ )
            {
                bool biggestLOD = i == lodCount - 1;
                GameObject nextLod = CreateLOD(i, lodCount, biggestLOD, meshInsts, baseVertDensity);
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

        static Mesh BuildOceanPatch(PatchType pt, float baseVertDensity)
        {
            ArrayList verts = new ArrayList();
            ArrayList indices = new ArrayList();

            // stick a bunch of verts into a 1m x 1m patch (scaling happens later)
            float dx = 1f / baseVertDensity;


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

            float sideLength_verts_x = 1f + baseVertDensity + skirtXminus + skirtXplus;
            float sideLength_verts_z = 1f + baseVertDensity + skirtZminus + skirtZplus;

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

                // recalculate bounds. add a little allowance for snapping. in the chunk renderer script, the bounds will be expanded further
                // to allow for horizontal displacement
                mesh.RecalculateBounds();
                Bounds bounds = mesh.bounds;
                bounds.extents = new Vector3(bounds.extents.x + dx, 100f, bounds.extents.z + dx);
                mesh.bounds = bounds;
                mesh.name = pt.ToString();
            }
            return mesh;
        }

        void PlaceLodData(Transform transform, Transform parent)
        {
            transform.parent = parent;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.up * 100f;
            transform.localEulerAngles = Vector3.right * 90f;
        }

        GameObject CreateLOD( int lodIndex, int lodCount, bool biggestLOD, Mesh[] meshData, float baseVertDensity )
        {
            // first create parent gameobject for the lod level. the scale of this transform sets the size of the lod.
            GameObject parent = new GameObject();
            parent.name = "LOD" + lodIndex;
            parent.transform.parent = transform;
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localRotation = Quaternion.identity;

            // add lod data cameras into this lod
            PlaceLodData(_camsAnimWaves[lodIndex].transform, parent.transform);
            if (_camsFoam[lodIndex] != null) PlaceLodData(_camsFoam[lodIndex].transform, parent.transform);
            if (_camsDynWaves[lodIndex] != null) PlaceLodData(_camsDynWaves[lodIndex].transform, parent.transform);
            if (_camsDynWavesNew[lodIndex] != null) PlaceLodData(_camsDynWavesNew[lodIndex].transform, parent.transform);
            
            bool generateSkirt = biggestLOD && !OceanRenderer.Instance._disableSkirt;

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
            if (OceanRenderer.Instance._uniformTiles)
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
                var patch = new GameObject( string.Format( "Tile_L{0}", lodIndex ) );
                patch.transform.parent = parent.transform;
                Vector2 pos = offsets[i];
                patch.transform.localPosition = new Vector3( pos.x, 0f, pos.y );
                patch.transform.localScale = Vector3.one;

                patch.AddComponent<OceanChunkRenderer>().SetInstanceData( lodIndex, lodCount, baseVertDensity ); ;
                patch.AddComponent<MeshFilter>().mesh = meshData[(int)patchTypes[i]];

                var mr = patch.AddComponent<MeshRenderer>();
                // i dont think one would use lightprobes for a purely specular water surface? (although diffuse foam shading would benefit)
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // arbitrary - could be turned on if desired
                mr.receiveShadows = false; // arbitrary - could be turned on if desired
                mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion; // TODO
                mr.material = _oceanMaterial;

                // rotate side patches to point the +x side outwards
                bool rotateXOutwards = patchTypes[i] == PatchType.FatX || patchTypes[i] == PatchType.FatXOuter || patchTypes[i] == PatchType.SlimX || patchTypes[i] == PatchType.SlimXFatZ;
                if( rotateXOutwards )
                {
                    if( Mathf.Abs( pos.y ) >= Mathf.Abs( pos.x ) )
                        patch.transform.localEulerAngles = -Vector3.up * 90f * Mathf.Sign( pos.y );
                    else
                        patch.transform.localEulerAngles = pos.x < 0f ? Vector3.up * 180f : Vector3.zero;
                }

                // rotate the corner patches so the +x and +z sides point outwards
                bool rotateXZOutwards = patchTypes[i] == PatchType.FatXZ || patchTypes[i] == PatchType.SlimXZ || patchTypes[i] == PatchType.FatXSlimZ || patchTypes[i] == PatchType.FatXZOuter;
                if( rotateXZOutwards )
                {
                    // xz direction before rotation
                    Vector3 from = new Vector3( 1f, 0f, 1f ).normalized;
                    // target xz direction is outwards vector given by local patch position - assumes this patch is a corner (checked below)
                    Vector3 to = patch.transform.localPosition.normalized;
                    if( Mathf.Abs( patch.transform.localPosition.x ) < 0.0001f || Mathf.Abs( Mathf.Abs( patch.transform.localPosition.x ) - Mathf.Abs( patch.transform.localPosition.z ) ) > 0.001f )
                    {
                        Debug.LogWarning( "Skipped rotating a patch because it isn't a corner, click here to highlight.", patch );
                        continue;
                    }

                    // detect 180 degree rotations as it doesnt always rotate around Y
                    if( Vector3.Dot( from, to ) < -0.99f )
                        patch.transform.localEulerAngles = Vector3.up * 180f;
                    else
                        patch.transform.localRotation = Quaternion.FromToRotation( from, to );
                }
            }

            return parent;
        }
    }
}
