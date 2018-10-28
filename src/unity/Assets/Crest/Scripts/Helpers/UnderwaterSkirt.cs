// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class UnderwaterSkirt : MonoBehaviour
    {
        public int _horizResolution = 64;
        public float _maxDistFromWater = 1f;
        public int _overrideSortingOrder = short.MinValue;

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
            _rend.sortingOrder = _overrideSortingOrder != short.MinValue ? _overrideSortingOrder : - LodData.MAX_LOD_COUNT - 1;

            GetComponent<MeshFilter>().mesh = Mesh2DGrid(0, 2, -0.5f, -0.5f, 1f, 1f, _horizResolution, 1);
        }

        private void LateUpdate()
        {
            Vector3 pos = OceanRenderer.Instance.Viewpoint.position;
            float waterHeight;
            bool gotHeight = OceanRenderer.Instance.CollisionProvider.SampleHeight(ref pos, out waterHeight);
            float heightOffset = pos.y - waterHeight;

            // Disable skirt when camera not close to water. In the first few frames collision may not be avail, in that case no choice
            // but to assume enabled. In the future this could detect if camera is far enough under water, render a simple quad to avoid
            // finding the intersection line.
            _rend.enabled = /*Mathf.Abs*/(heightOffset) < _maxDistFromWater || !gotHeight;

            if (_rend.enabled)
            {
                // Assign lod0 shape - trivial but bound every frame because lod transform comes from here
                if (_mpb == null)
                {
                    _mpb = new MaterialPropertyBlock();
                }
                _rend.GetPropertyBlock(_mpb);
                var ldaws = OceanRenderer.Instance._lodDataAnimWaves;
                // Underwater rendering uses LOD0 for intersecting the waves with the near plane, and LOD1 for sampling ocean depth (see ScatterColour())
                ldaws[0].BindResultData(0, _mpb);
                if (OceanRenderer.Instance._createSeaFloorDepthData) ldaws[1].LDSeaDepth.BindResultData(1, _mpb);
                if (OceanRenderer.Instance._createShadowData) ldaws[1].LDShadow.BindResultData(1, _mpb);
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
