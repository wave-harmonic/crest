// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Sets shader parameters for each geometry tile/chunk.
    /// </summary>
    public class OceanChunkRenderer : MonoBehaviour
    {
        public bool _drawRenderBounds = false;

        Bounds _boundsLocal;
        Mesh _mesh;
        Renderer _rend;
        MaterialPropertyBlock _mpb;

        int _lodIndex = -1;
        int _totalLodCount = -1;
        float _baseVertDensity = 32f;

        int _reflectionTexId = -1;

        void Start()
        {
            _rend = GetComponent<Renderer>();
            _mesh = GetComponent<MeshFilter>().mesh;

            _boundsLocal = _mesh.bounds;

            _reflectionTexId = Shader.PropertyToID("_ReflectionTex");

            UpdateMeshBounds();
        }

        private void Update()
        {
            // This needs to be called on Update because the bounds depend on transform scale which can change. Also OnWillRenderObject depends on
            // the bounds being correct. This could however be called on scale change events, but would add slightly more complexity.
            UpdateMeshBounds();
        }

        void UpdateMeshBounds()
        {
            var newBounds = _boundsLocal;
            ExpandBoundsForDisplacements(transform, ref newBounds);
            _mesh.bounds = newBounds;
        }

        // Called when visible to a camera
        void OnWillRenderObject()
        {
            // Depth texture is used by ocean shader for transparency/depth fog, and for fading out foam at shoreline.
            Camera.current.depthTextureMode |= DepthTextureMode.Depth;

            // per instance data

            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
            }
            _rend.GetPropertyBlock(_mpb);

            // blend LOD 0 shape in/out to avoid pop, if the ocean might scale up later (it is smaller than its maximum scale)
            bool needToBlendOutShape = _lodIndex == 0 && OceanRenderer.Instance.ScaleCouldIncrease;
            float meshScaleLerp = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 0f;

            // blend furthest normals scale in/out to avoid pop, if scale could reduce
            bool needToBlendOutNormals = _lodIndex == _totalLodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease;
            float farNormalsWeight = needToBlendOutNormals ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            _mpb.SetVector("_InstanceData", new Vector4(meshScaleLerp, farNormalsWeight, _lodIndex));

            // geometry data
            // compute grid size of geometry. take the long way to get there - make sure we land exactly on a power of two
            // and not inherit any of the lossy-ness from lossyScale.
            float squareSize = Mathf.Pow(2f, Mathf.Round(Mathf.Log(transform.lossyScale.x) / Mathf.Log(2f))) / _baseVertDensity;
            float mul = 1.875f; // fudge 1
            float pow = 1.4f; // fudge 2
            float normalScrollSpeed0 = Mathf.Pow(Mathf.Log(1f + 2f * squareSize) * mul, pow);
            float normalScrollSpeed1 = Mathf.Pow(Mathf.Log(1f + 4f * squareSize) * mul, pow);
            _mpb.SetVector("_GeomData", new Vector3(squareSize, normalScrollSpeed0, normalScrollSpeed1));

            // assign lod data to ocean shader
            var ldaws = OceanRenderer.Instance._lodDataAnimWaves;
            var ldsds = OceanRenderer.Instance._lodDataSeaDepths;
            var ldfoam = OceanRenderer.Instance._lodDataFoam;
            var ldflow = OceanRenderer.Instance._lodDataFlow;
            var ldshadows = OceanRenderer.Instance._lodDataShadow;

            ldaws.BindResultData(_lodIndex, 0, _mpb);
            if (OceanRenderer.Instance._createFlowSim) ldflow.BindResultData(_lodIndex, 0, _mpb);
            if (OceanRenderer.Instance._createFoamSim) ldfoam.BindResultData(_lodIndex, 0, _mpb);
            if (OceanRenderer.Instance._createSeaFloorDepthData) ldsds.BindResultData(_lodIndex, 0, _mpb);
            if (OceanRenderer.Instance._createShadowData) ldshadows.BindResultData(_lodIndex, 0, _mpb);

            if (_lodIndex + 1 < OceanRenderer.Instance.CurrentLodCount)
            {
                ldaws.BindResultData(_lodIndex + 1, 1, _mpb);
                if (OceanRenderer.Instance._createFlowSim) ldflow.BindResultData(_lodIndex + 1, 1, _mpb);
                if (OceanRenderer.Instance._createFoamSim) ldfoam.BindResultData(_lodIndex + 1, 1, _mpb);
                if (OceanRenderer.Instance._createSeaFloorDepthData) ldsds.BindResultData(_lodIndex + 1, 1, _mpb);
                if (OceanRenderer.Instance._createShadowData) ldshadows.BindResultData(_lodIndex + 1, 1, _mpb);
            }

            var reflTex = OceanPlanarReflection.GetRenderTexture(Camera.current.targetDisplay);
            if (reflTex)
            {
                _mpb.SetTexture(_reflectionTexId, reflTex);
            }
            else
            {
                _mpb.SetTexture(_reflectionTexId, Texture2D.blackTexture);
            }

            // Hack - due to SV_IsFrontFace occasionally coming through as true for backfaces,
            // add a param here that forces ocean to be in undrwater state. I think the root
            // cause here might be imprecision or numerical issues at ocean tile boundaries, although
            // i'm not sure why cracks are not visible in this case.
            float heightOffset = OceanRenderer.Instance.ViewerHeightAboveWater;
            _mpb.SetFloat("_ForceUnderwater", heightOffset < -2f ? 1f : 0f);

            _rend.SetPropertyBlock(_mpb);

            if (_drawRenderBounds)
            {
                _rend.bounds.DebugDraw();
            }
        }

        // this is called every frame because the bounds are given in world space and depend on the transform scale, which
        // can change depending on view altitude
        public static void ExpandBoundsForDisplacements(Transform transform, ref Bounds bounds)
        {
            float boundsPadding = OceanRenderer.Instance.MaxHorizDisplacement;
            float expandXZ = boundsPadding / transform.lossyScale.x;
            float boundsY = OceanRenderer.Instance.MaxVertDisplacement / transform.lossyScale.y;
            // extend the kinematic bounds slightly to give room for dynamic sim stuff
            boundsY = Mathf.Max(boundsY, 1f);
            bounds.extents = new Vector3(bounds.extents.x + expandXZ, boundsY, bounds.extents.z + expandXZ);
        }

        public void SetInstanceData(int lodIndex, int totalLodCount, float baseVertDensity)
        {
            _lodIndex = lodIndex; _totalLodCount = totalLodCount; _baseVertDensity = baseVertDensity;
        }
    }

    static class BoundsHelper
    {
        public static void DebugDraw(this Bounds b)
        {
            // source: https://github.com/UnityCommunity/UnityLibrary
            // license: mit - https://github.com/UnityCommunity/UnityLibrary/blob/master/LICENSE.md

            // bounding box using Debug.Drawline

            // bottom
            var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
            var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
            var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
            var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

            Debug.DrawLine(p1, p2, Color.blue);
            Debug.DrawLine(p2, p3, Color.red);
            Debug.DrawLine(p3, p4, Color.yellow);
            Debug.DrawLine(p4, p1, Color.magenta);

            // top
            var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
            var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
            var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
            var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

            Debug.DrawLine(p5, p6, Color.blue);
            Debug.DrawLine(p6, p7, Color.red);
            Debug.DrawLine(p7, p8, Color.yellow);
            Debug.DrawLine(p8, p5, Color.magenta);

            // sides
            Debug.DrawLine(p1, p5, Color.white);
            Debug.DrawLine(p2, p6, Color.gray);
            Debug.DrawLine(p3, p7, Color.green);
            Debug.DrawLine(p4, p8, Color.cyan);
        }
    }
}
