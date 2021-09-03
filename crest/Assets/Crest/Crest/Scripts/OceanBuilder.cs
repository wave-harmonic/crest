// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

//#define PROFILE_CONSTRUCTION

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Instantiates all the ocean geometry, as a set of tiles.
    /// </summary>
    public static class OceanBuilder
    {
        // The comments below illustrate case when BASE_VERT_DENSITY = 2. The ocean mesh is built up from these patches. Rotational symmetry
        // is used where possible to eliminate combinations. The slim variants are used to eliminate overlap between patches.
        enum PatchType
        {
            /// <summary>
            /// Adds no skirt. Used in interior of highest detail LOD (0)
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
            /// Adds a full skirt all of the way around a patch
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

        public static Transform GenerateMesh(OceanRenderer ocean, List<OceanChunkRenderer> tiles, int lodDataResolution, int geoDownSampleFactor, int lodCount)
        {
            if (lodCount < 1)
            {
                Debug.LogError("Crest: Invalid LOD count: " + lodCount.ToString(), ocean);
                return null;
            }

            int oceanLayer = ocean.Layer;

#pragma warning disable 0618
            if (ocean.LayerName != "")
            {
                oceanLayer = LayerMask.NameToLayer(ocean.LayerName);
                if (oceanLayer == -1)
                {
                    Debug.LogError("Crest: Invalid ocean layer: " + ocean.LayerName + " please add this layer.", ocean);
                    oceanLayer = 0;
                }
            }
#pragma warning restore 0618

#if PROFILE_CONSTRUCTION
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif

            ClearOutTiles(ocean, tiles);

            var root = new GameObject("Root");
            Debug.Assert(root != null, "Crest: The ocean Root transform could not be immediately constructed. Please report this issue to the Crest developers via our support email or GitHub at https://github.com/wave-harmonic/crest/issues .");

            root.hideFlags = ocean._hideOceanTileGameObjects ? HideFlags.HideAndDontSave : HideFlags.DontSave;
            root.transform.parent = ocean.transform;
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            if (!OceanRenderer.RunningHeadless && !OceanRenderer.RunningWithoutGPU)
            {
                // create mesh data
                Mesh[] meshInsts = new Mesh[(int)PatchType.Count];
                Bounds[] meshBounds = new Bounds[(int)PatchType.Count];
                // 4 tiles across a LOD, and support lowering density by a factor
                var tileResolution = Mathf.Round(0.25f * lodDataResolution / geoDownSampleFactor);
                for (int i = 0; i < (int)PatchType.Count; i++)
                {
                    meshInsts[i] = BuildOceanPatch((PatchType)i, tileResolution, out meshBounds[i]);
                }

                for (int i = 0; i < lodCount; i++)
                {
                    CreateLOD(ocean, tiles, root.transform, i, lodCount, meshInsts, meshBounds, lodDataResolution, geoDownSampleFactor, oceanLayer);
                }
            }

#if PROFILE_CONSTRUCTION
            sw.Stop();
            Debug.Log( "Crest: Finished generating " + lodCount.ToString() + " LODs, time: " + (1000.0*sw.Elapsed.TotalSeconds).ToString(".000") + "ms" );
#endif

            return root.transform;
        }

        public static void ClearOutTiles(OceanRenderer ocean, List<OceanChunkRenderer> tiles)
        {
            tiles.Clear();

            if (ocean.Root == null)
            {
                return;
            }

            // Remove existing LODs
            for (int i = 0; i < ocean.Root.childCount; i++)
            {
                var child = ocean.Root.GetChild(i);
                if (child.name.StartsWith("Tile_L"))
                {
                    DestroyGO(child);

                    i--;
                }
            }

            DestroyGO(ocean.Root);
        }

        static void DestroyGO(Transform go)
        {
            go.parent = null;

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
            {
                Object.Destroy(go.gameObject);
            }
            else
            {
                Object.DestroyImmediate(go.gameObject);
            }
#else
            Object.Destroy(go.gameObject);
#endif
        }

        static Mesh BuildOceanPatch(PatchType pt, float vertDensity, out Bounds bounds)
        {
            ArrayList verts = new ArrayList();
            ArrayList indices = new ArrayList();

            // stick a bunch of verts into a 1m x 1m patch (scaling happens later)
            float dx = 1f / vertDensity;


            //////////////////////////////////////////////////////////////////////////////////
            // verts

            // see comments within PatchType for diagrams of each patch mesh

            // skirt widths on left, right, bottom and top (in order)
            float skirtXminus = 0f, skirtXplus = 0f;
            float skirtZminus = 0f, skirtZplus = 0f;
            // set the patch size
            if (pt == PatchType.Fat) { skirtXminus = skirtXplus = skirtZminus = skirtZplus = 1f; }
            else if (pt == PatchType.FatX || pt == PatchType.FatXOuter) { skirtXplus = 1f; }
            else if (pt == PatchType.FatXZ || pt == PatchType.FatXZOuter) { skirtXplus = skirtZplus = 1f; }
            else if (pt == PatchType.FatXSlimZ) { skirtXplus = 1f; skirtZplus = -1f; }
            else if (pt == PatchType.SlimX) { skirtXplus = -1f; }
            else if (pt == PatchType.SlimXZ) { skirtXplus = skirtZplus = -1f; }
            else if (pt == PatchType.SlimXFatZ) { skirtXplus = -1f; skirtZplus = 1f; }

            float sideLength_verts_x = 1f + vertDensity + skirtXminus + skirtXplus;
            float sideLength_verts_z = 1f + vertDensity + skirtZminus + skirtZplus;

            float start_x = -0.5f - skirtXminus * dx;
            float start_z = -0.5f - skirtZminus * dx;
            float end_x = 0.5f + skirtXplus * dx;
            float end_z = 0.5f + skirtZplus * dx;

            for (float j = 0; j < sideLength_verts_z; j++)
            {
                // interpolate z across patch
                float z = Mathf.Lerp(start_z, end_z, j / (sideLength_verts_z - 1f));

                // push outermost edge out to horizon
                if (pt == PatchType.FatXZOuter && j == sideLength_verts_z - 1f)
                    z *= 100f;

                for (float i = 0; i < sideLength_verts_x; i++)
                {
                    // interpolate x across patch
                    float x = Mathf.Lerp(start_x, end_x, i / (sideLength_verts_x - 1f));

                    // push outermost edge out to horizon
                    if (i == sideLength_verts_x - 1f && (pt == PatchType.FatXOuter || pt == PatchType.FatXZOuter))
                        x *= 100f;

                    // could store something in y, although keep in mind this is a shared mesh that is shared across multiple lods
                    verts.Add(new Vector3(x, 0f, z));
                }
            }


            //////////////////////////////////////////////////////////////////////////////////
            // indices

            int sideLength_squares_x = (int)sideLength_verts_x - 1;
            int sideLength_squares_z = (int)sideLength_verts_z - 1;

            for (int j = 0; j < sideLength_squares_z; j++)
            {
                for (int i = 0; i < sideLength_squares_x; i++)
                {
                    bool flipEdge = false;

                    if (i % 2 == 1) flipEdge = !flipEdge;
                    if (j % 2 == 1) flipEdge = !flipEdge;

                    int i0 = i + j * (sideLength_squares_x + 1);
                    int i1 = i0 + 1;
                    int i2 = i0 + (sideLength_squares_x + 1);
                    int i3 = i2 + 1;

                    if (!flipEdge)
                    {
                        // tri 1
                        indices.Add(i3);
                        indices.Add(i1);
                        indices.Add(i0);

                        // tri 2
                        indices.Add(i0);
                        indices.Add(i2);
                        indices.Add(i3);
                    }
                    else
                    {
                        // tri 1
                        indices.Add(i3);
                        indices.Add(i1);
                        indices.Add(i2);

                        // tri 2
                        indices.Add(i0);
                        indices.Add(i2);
                        indices.Add(i1);
                    }
                }
            }


            //////////////////////////////////////////////////////////////////////////////////
            // create mesh

            Mesh mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSave;
            if (verts != null && verts.Count > 0)
            {
                Vector3[] arrV = new Vector3[verts.Count];
                verts.CopyTo(arrV);

                int[] arrI = new int[indices.Count];
                indices.CopyTo(arrI);

                mesh.SetIndices(null, MeshTopology.Triangles, 0);
                mesh.vertices = arrV;
                mesh.normals = null;
                mesh.SetIndices(arrI, MeshTopology.Triangles, 0);

                // recalculate bounds. add a little allowance for snapping. in the chunk renderer script, the bounds will be expanded further
                // to allow for horizontal displacement
                mesh.RecalculateBounds();
                bounds = mesh.bounds;
                bounds.extents = new Vector3(bounds.extents.x + dx, 100f, bounds.extents.z + dx);
                mesh.bounds = bounds;
                mesh.name = pt.ToString();
            }
            else
            {
                bounds = new Bounds();
            }

            return mesh;
        }

        static void CreateLOD(OceanRenderer ocean, List<OceanChunkRenderer> tiles, Transform parent, int lodIndex, int lodCount, Mesh[] meshData, Bounds[] meshBounds, int lodDataResolution, int geoDownSampleFactor, int oceanLayer)
        {
            float horizScale = Mathf.Pow(2f, lodIndex);

            bool isBiggestLOD = lodIndex == lodCount - 1;
            bool generateSkirt = isBiggestLOD && !ocean._disableSkirt;

            Vector2[] offsets;
            PatchType[] patchTypes;

            PatchType leadSideType = generateSkirt ? PatchType.FatXOuter : PatchType.SlimX;
            PatchType trailSideType = generateSkirt ? PatchType.FatXOuter : PatchType.FatX;
            PatchType leadCornerType = generateSkirt ? PatchType.FatXZOuter : PatchType.SlimXZ;
            PatchType trailCornerType = generateSkirt ? PatchType.FatXZOuter : PatchType.FatXZ;
            PatchType tlCornerType = generateSkirt ? PatchType.FatXZOuter : PatchType.SlimXFatZ;
            PatchType brCornerType = generateSkirt ? PatchType.FatXZOuter : PatchType.FatXSlimZ;

            if (lodIndex != 0)
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
            if (ocean._uniformTiles)
            {
                for (int i = 0; i < patchTypes.Length; i++)
                {
                    patchTypes[i] = PatchType.Fat;
                }
            }

            // create the ocean patches
            for (int i = 0; i < offsets.Length; i++)
            {
                // instantiate and place patch
                var patch = new GameObject($"Tile_L{lodIndex}_{patchTypes[i]}");
                patch.hideFlags = HideFlags.DontSave;
                patch.layer = oceanLayer;
                patch.transform.parent = parent;
                Vector2 pos = offsets[i];
                patch.transform.localPosition = horizScale * new Vector3(pos.x, 0f, pos.y);
                // scale only horizontally, otherwise culling bounding box will be scaled up in y
                patch.transform.localScale = new Vector3(horizScale, 1f, horizScale);

                {
                    var oceanChunkRenderer = patch.AddComponent<OceanChunkRenderer>();
                    oceanChunkRenderer._boundsLocal = meshBounds[(int)patchTypes[i]];
                    patch.AddComponent<MeshFilter>().sharedMesh = meshData[(int)patchTypes[i]];
                    oceanChunkRenderer.SetInstanceData(lodIndex);
                    tiles.Add(oceanChunkRenderer);
                }

                var mr = patch.AddComponent<MeshRenderer>();

                // Sorting order to stop unity drawing it back to front. make the innermost 4 tiles draw first, followed by
                // the rest of the tiles by LOD index. all this happens before layer 0 - the sorting layer takes priority over the
                // render queue it seems! ( https://cdry.wordpress.com/2017/04/28/unity-render-queues-vs-sorting-layers/ ). This pushes
                // ocean rendering way early, so transparent objects will by default render afterwards, which is typical for water rendering.
                mr.sortingOrder = -lodCount + (patchTypes[i] == PatchType.Interior ? -1 : lodIndex);

                // I don't think one would use light probes for a purely specular water surface? (although diffuse foam shading would benefit)
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // arbitrary - could be turned on if desired
                mr.receiveShadows = false; // this setting is ignored by unity for the transparent ocean shader
                mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                mr.material = ocean.OceanMaterial;

                // rotate side patches to point the +x side outwards
                bool rotateXOutwards = patchTypes[i] == PatchType.FatX || patchTypes[i] == PatchType.FatXOuter || patchTypes[i] == PatchType.SlimX || patchTypes[i] == PatchType.SlimXFatZ;
                if (rotateXOutwards)
                {
                    if (Mathf.Abs(pos.y) >= Mathf.Abs(pos.x))
                        patch.transform.localEulerAngles = -Vector3.up * 90f * Mathf.Sign(pos.y);
                    else
                        patch.transform.localEulerAngles = pos.x < 0f ? Vector3.up * 180f : Vector3.zero;
                }

                // rotate the corner patches so the +x and +z sides point outwards
                bool rotateXZOutwards = patchTypes[i] == PatchType.FatXZ || patchTypes[i] == PatchType.SlimXZ || patchTypes[i] == PatchType.FatXSlimZ || patchTypes[i] == PatchType.FatXZOuter;
                if (rotateXZOutwards)
                {
                    // xz direction before rotation
                    Vector3 from = new Vector3(1f, 0f, 1f).normalized;
                    // target xz direction is outwards vector given by local patch position - assumes this patch is a corner (checked below)
                    Vector3 to = patch.transform.localPosition.normalized;
                    if (Mathf.Abs(patch.transform.localPosition.x) < 0.0001f || Mathf.Abs(Mathf.Abs(patch.transform.localPosition.x) - Mathf.Abs(patch.transform.localPosition.z)) > 0.001f)
                    {
                        Debug.LogWarning("Crest: Skipped rotating a patch because it isn't a corner, click here to highlight.", patch);
                        continue;
                    }

                    // Detect 180 degree rotations as it doesn't always rotate around Y
                    if (Vector3.Dot(from, to) < -0.99f)
                        patch.transform.localEulerAngles = Vector3.up * 180f;
                    else
                        patch.transform.localRotation = Quaternion.FromToRotation(from, to);
                }
            }
        }
    }
}
