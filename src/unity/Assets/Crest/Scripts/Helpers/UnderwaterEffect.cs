// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Handles effects that need to track the water surface. Feeds in wave data and disables rendering when
    /// not close to water.
    /// </summary>
    public class UnderwaterEffect : MonoBehaviour
    {
        [SerializeField] float _maxHeightAboveWater = 1.5f;
        [SerializeField] bool _overrideSortingOrder = false;
        [SerializeField] int _overridenSortingOrder = 0;

        [Header("Copy params from Ocean material")]
        [SerializeField] bool _copyParamsOnStartup = true;
        [SerializeField] bool _copyParamsEachFrame = false;

        // how many vertical edges to add to curtain geometry
        const int GEOM_HORIZ_DIVISIONS = 64;

        MaterialPropertyBlock _mpb;
        Renderer _rend;

        private void Start()
        {
            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }

            _rend = GetComponent<Renderer>();

            // Render before the surface mesh
            _rend.sortingOrder = _overrideSortingOrder ? _overridenSortingOrder : -LodDataMgr.MAX_LOD_COUNT - 1;
            GetComponent<MeshFilter>().mesh = Mesh2DGrid(0, 2, -0.5f, -0.5f, 1f, 1f, GEOM_HORIZ_DIVISIONS, 1);

            // hack - push forward so the geometry wont be frustum culled. there might be better ways to draw
            // this stuff.
            if (transform.parent.GetComponent<Camera>() == null)
            {
                Debug.LogError("Underwater effects expect to be parented to a camera.", this);
                enabled = false;
                return;
            }
            transform.localPosition = Vector3.forward;

            ConfigureMaterial();
        }

        void ConfigureMaterial()
        {
            var keywords = _rend.material.shaderKeywords;
            foreach (var keyword in keywords)
            {
                if (keyword == "_COMPILESHADERWITHDEBUGINFO_ON") continue;

                if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled(keyword))
                {
                    Debug.LogWarning("Keyword " + keyword + " was enabled on the ocean material but not on the underwater material " + _rend.sharedMaterial.name + ", underwater appearance may not match ocean surface in standalone builds.", this);
                }
            }

            if (_copyParamsOnStartup)
            {
                _rend.material.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
            }
        }

        private void LateUpdate()
        {
            float heightOffset = OceanRenderer.Instance.ViewerHeightAboveWater;

            // Disable skirt when camera not close to water. In the first few frames collision may not be avail, in that case no choice
            // but to assume enabled. In the future this could detect if camera is far enough under water, render a simple quad to avoid
            // finding the intersection line.
            _rend.enabled = heightOffset < _maxHeightAboveWater;

            if (_rend.enabled)
            {
                if(_copyParamsEachFrame)
                {
                    _rend.material.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
                }

                // Assign lod0 shape - trivial but bound every frame because lod transform comes from here
                if (_mpb == null)
                {
                    _mpb = new MaterialPropertyBlock();
                }
                _rend.GetPropertyBlock(_mpb);

                // Underwater rendering uses displacements for intersecting the waves with the near plane, and ocean depth/shadows for ScatterColour()
                OceanRenderer.Instance._lodDataAnimWaves.BindResultData(0, 0, _mpb);

                if (OceanRenderer.Instance._createSeaFloorDepthData)
                {
                    OceanRenderer.Instance._lodDataSeaDepths.BindResultData(0, 0, _mpb);
                }
                else
                {
                    LodDataMgrSeaFloorDepth.BindNull(0, _mpb);
                }

                if (OceanRenderer.Instance._createShadowData)
                {
                    OceanRenderer.Instance._lodDataShadow.BindResultData(0, 0, _mpb);
                }
                else
                {
                    LodDataMgrShadow.BindNull(0, _mpb);
                }

                _mpb.SetFloat("_HeightOffset", heightOffset);

                _rend.SetPropertyBlock(_mpb);
            }
        }

        static Mesh Mesh2DGrid(int dim0, int dim1, float start0, float start1, float width0, float width1, int divs0, int divs1)
        {
            Vector3[] verts = new Vector3[(divs1 + 1) * (divs0 + 1)];
            Vector2[] uvs = new Vector2[(divs1 + 1) * (divs0 + 1)];
            float dx0 = width0 / divs0, dx1 = width1 / divs1;
            for( int i1 = 0; i1 < divs1 + 1; i1++)
            {
                float v = i1 / (float)divs1;

                for (int i0 = 0; i0 < divs0 + 1; i0++)
                {
                    int i = (divs0 + 1) * i1 + i0;
                    verts[i][dim0] = start0 + i0 * dx0;
                    verts[i][dim1] = start1 + i1 * dx1;

                    uvs[i][0] = i0 / (float)divs0;
                    uvs[i][1] = v;
                }
            }

            int[] indices = new int[divs0 * divs1 * 2 * 3];
            for (int i1 = 0; i1 < divs1; i1++)
            {
                for (int i0 = 0; i0 < divs0; i0++)
                {
                    int i00 = (divs0 + 1) * (i1 + 0) + (i0 + 0);
                    int i01 = (divs0 + 1) * (i1 + 0) + (i0 + 1);
                    int i10 = (divs0 + 1) * (i1 + 1) + (i0 + 0);
                    int i11 = (divs0 + 1) * (i1 + 1) + (i0 + 1);

                    int tri;

                    tri = 0;
                    indices[(i1 * divs0 + i0) * 6 + tri * 3 + 0] = i00;
                    indices[(i1 * divs0 + i0) * 6 + tri * 3 + 1] = i11;
                    indices[(i1 * divs0 + i0) * 6 + tri * 3 + 2] = i01;
                    tri = 1;
                    indices[(i1 * divs0 + i0) * 6 + tri * 3 + 0] = i00;
                    indices[(i1 * divs0 + i0) * 6 + tri * 3 + 1] = i10;
                    indices[(i1 * divs0 + i0) * 6 + tri * 3 + 2] = i11;
                }
            }

            var mesh = new Mesh();
            mesh.name = "Grid2D_" + divs0 + "x" + divs1;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            return mesh;
        }
    }
}
