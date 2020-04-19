// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.Rendering;
#else
using UnityEngine.Rendering;
#endif

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
        PropertyWrapperMPB _mpb;

        // Cache these off to support regenerating ocean surface
        int _lodIndex = -1;
        int _totalLodCount = -1;
        int _lodDataResolution = 256;
        int _geoDownSampleFactor = 1;

        static int sp_ReflectionTex = Shader.PropertyToID("_ReflectionTex");
        static int sp_GeomData = Shader.PropertyToID("_GeomData");
        static int sp_ForceUnderwater = Shader.PropertyToID("_ForceUnderwater");
        // MeshScaleLerp, FarNormalsWeight, LODIndex (debug)
        public static int sp_InstanceData = Shader.PropertyToID("_InstanceData");

        void Start()
        {
            _rend = GetComponent<Renderer>();
            _mesh = GetComponent<MeshFilter>().mesh;
            _boundsLocal = _mesh.bounds;

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

        static Camera _currentCamera = null;

#if UNITY_2018
        private static void BeginCameraRendering(Camera camera)
#else
        private static void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
#endif
        {
            _currentCamera = camera;
        }

        // Called when visible to a camera
        void OnWillRenderObject()
        {
            // check if built-in pipeline being used
            if (Camera.current != null)
            {
                _currentCamera = Camera.current;
            }

            // Depth texture is used by ocean shader for transparency/depth fog, and for fading out foam at shoreline.
            _currentCamera.depthTextureMode |= DepthTextureMode.Depth;

            if (_rend.sharedMaterial != OceanRenderer.Instance.OceanMaterial)
            {
                _rend.sharedMaterial = OceanRenderer.Instance.OceanMaterial;
            }

            // per instance data

            if (_mpb == null)
            {
                _mpb = new PropertyWrapperMPB();
            }
            _rend.GetPropertyBlock(_mpb.materialPropertyBlock);

            // blend LOD 0 shape in/out to avoid pop, if the ocean might scale up later (it is smaller than its maximum scale)
            var needToBlendOutShape = _lodIndex == 0 && OceanRenderer.Instance.ScaleCouldIncrease;
            var meshScaleLerp = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 0f;

            // blend furthest normals scale in/out to avoid pop, if scale could reduce
            var needToBlendOutNormals = _lodIndex == _totalLodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease;
            var farNormalsWeight = needToBlendOutNormals ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
            _mpb.SetVector(sp_InstanceData, new Vector3(meshScaleLerp, farNormalsWeight, _lodIndex));

            // geometry data
            // compute grid size of geometry. take the long way to get there - make sure we land exactly on a power of two
            // and not inherit any of the lossy-ness from lossyScale.
            var scale_pow_2 = OceanRenderer.Instance.CalcLodScale(_lodIndex);
            var gridSizeGeo = scale_pow_2 / (0.25f * _lodDataResolution / _geoDownSampleFactor);
            var gridSizeLodData = gridSizeGeo / _geoDownSampleFactor;
            var mul = 1.875f; // fudge 1
            var pow = 1.4f; // fudge 2
            var normalScrollSpeed0 = Mathf.Pow(Mathf.Log(1f + 2f * gridSizeLodData) * mul, pow);
            var normalScrollSpeed1 = Mathf.Pow(Mathf.Log(1f + 4f * gridSizeLodData) * mul, pow);
            _mpb.SetVector(sp_GeomData, new Vector4(gridSizeLodData, gridSizeGeo, normalScrollSpeed0, normalScrollSpeed1));

            // Assign LOD data to ocean shader
            var ldaws = OceanRenderer.Instance._lodDataAnimWaves;
            var ldsds = OceanRenderer.Instance._lodDataSeaDepths;
            var ldclip = OceanRenderer.Instance._lodDataClipSurface;
            var ldfoam = OceanRenderer.Instance._lodDataFoam;
            var ldflow = OceanRenderer.Instance._lodDataFlow;
            var ldshadows = OceanRenderer.Instance._lodDataShadow;

            _mpb.SetInt(LodDataMgr.sp_LD_SliceIndex, _lodIndex);
            ldaws.BindResultData(_mpb);
            if (ldflow) ldflow.BindResultData(_mpb);
            if (ldfoam) ldfoam.BindResultData(_mpb); else LodDataMgrFoam.BindNull(_mpb);
            if (ldsds) ldsds.BindResultData(_mpb); else LodDataMgrSeaFloorDepth.BindNull(_mpb);
            if (ldclip) ldclip.BindResultData(_mpb); else LodDataMgrClipSurface.BindNull(_mpb);
            if (ldshadows) ldshadows.BindResultData(_mpb); else LodDataMgrShadow.BindNull(_mpb);

            var reflTex = PreparedReflections.GetRenderTexture(_currentCamera.GetHashCode());
            if (reflTex)
            {
                _mpb.SetTexture(sp_ReflectionTex, reflTex);
            }
            else
            {
                _mpb.SetTexture(sp_ReflectionTex, Texture2D.blackTexture);
            }

            // Hack - due to SV_IsFrontFace occasionally coming through as true for back faces,
            // add a param here that forces ocean to be in underwater state. I think the root
            // cause here might be imprecision or numerical issues at ocean tile boundaries, although
            // i'm not sure why cracks are not visible in this case.
            var heightOffset = OceanRenderer.Instance.ViewerHeightAboveWater;
            _mpb.SetFloat(sp_ForceUnderwater, heightOffset < -2f ? 1f : 0f);

            _rend.SetPropertyBlock(_mpb.materialPropertyBlock);

            if (_drawRenderBounds)
            {
                _rend.bounds.DebugDraw();
            }
        }

        // this is called every frame because the bounds are given in world space and depend on the transform scale, which
        // can change depending on view altitude
        public static void ExpandBoundsForDisplacements(Transform transform, ref Bounds bounds)
        {
            var boundsPadding = OceanRenderer.Instance.MaxHorizDisplacement;
            var expandXZ = boundsPadding / transform.lossyScale.x;
            var boundsY = OceanRenderer.Instance.MaxVertDisplacement;
            // extend the kinematic bounds slightly to give room for dynamic sim stuff
            boundsY += 5f;
            bounds.extents = new Vector3(bounds.extents.x + expandXZ, boundsY / transform.lossyScale.y, bounds.extents.z + expandXZ);
        }

        public void SetInstanceData(int lodIndex, int totalLodCount, int lodDataResolution, int geoDownSampleFactor)
        {
            _lodIndex = lodIndex; _totalLodCount = totalLodCount; _lodDataResolution = lodDataResolution; _geoDownSampleFactor = geoDownSampleFactor;
        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            sp_ReflectionTex = Shader.PropertyToID("_ReflectionTex");
            sp_GeomData = Shader.PropertyToID("_GeomData");
            sp_ForceUnderwater = Shader.PropertyToID("_ForceUnderwater");
            sp_InstanceData = Shader.PropertyToID("_InstanceData");
            _currentCamera = null;
        }

        [RuntimeInitializeOnLoadMethod]
        static void RunOnStart()
        {
#if UNITY_2018
            RenderPipeline.beginCameraRendering -= BeginCameraRendering;
            RenderPipeline.beginCameraRendering += BeginCameraRendering;
#else
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
#endif
        }
    }

    public static class BoundsHelper
    {
        public static void DebugDraw(this Bounds b)
        {
            var xmin = b.min.x;
            var ymin = b.min.y;
            var zmin = b.min.z;
            var xmax = b.max.x;
            var ymax = b.max.y;
            var zmax = b.max.z;

            Debug.DrawLine(new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymin, zmax));
            Debug.DrawLine(new Vector3(xmin, ymin, zmin), new Vector3(xmax, ymin, zmin));
            Debug.DrawLine(new Vector3(xmax, ymin, zmax), new Vector3(xmin, ymin, zmax));
            Debug.DrawLine(new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymin, zmin));

            Debug.DrawLine(new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymax, zmax));
            Debug.DrawLine(new Vector3(xmin, ymax, zmin), new Vector3(xmax, ymax, zmin));
            Debug.DrawLine(new Vector3(xmax, ymax, zmax), new Vector3(xmin, ymax, zmax));
            Debug.DrawLine(new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymax, zmin));

            Debug.DrawLine(new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax));
            Debug.DrawLine(new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin));
            Debug.DrawLine(new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymax, zmin));
            Debug.DrawLine(new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymin, zmax));
        }
    }
}
