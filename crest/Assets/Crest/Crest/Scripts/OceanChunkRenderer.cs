﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;
using Crest.Internal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    /// <summary>
    /// Sets shader parameters for each geometry tile/chunk.
    /// </summary>
    [ExecuteDuringEditMode]
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_INTERNAL + "Ocean Chunk Renderer")]
    public class OceanChunkRenderer : CustomMonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public bool _drawRenderBounds = false;

        public Bounds _boundsLocal;
        Mesh _mesh;
        public Renderer Rend { get; private set; }
        internal PropertyWrapperMPB _mpb;

        internal Rect _unexpandedBoundsXZ = new Rect();
        public Rect UnexpandedBoundsXZ => _unexpandedBoundsXZ;

        public bool MaterialOverridden { get; set; }

        // We need to ensure that all ocean data has been bound for the mask to
        // render properly - this is something that needs to happen irrespective
        // of occlusion culling because we need the mask to render as a
        // contiguous surface.
        internal bool _oceanDataHasBeenBound = true;

        int _lodIndex = -1;

        static int sp_ReflectionTex = Shader.PropertyToID("_ReflectionTex");

        void Start()
        {
            Rend = GetComponent<Renderer>();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _mesh = GetComponent<MeshFilter>().sharedMesh;
            }
            else
#endif
            {
                // An unshared mesh will break instancing, but a shared mesh will break culling due to shared bounds.
                _mesh = GetComponent<MeshFilter>().mesh;
            }

            UpdateMeshBounds();

            SetOneTimeMPBParams();
        }

        void SetOneTimeMPBParams()
        {
            if (_mpb == null)
            {
                _mpb = new PropertyWrapperMPB();
            }

            Rend.GetPropertyBlock(_mpb.materialPropertyBlock);

            _mpb.SetInt(LodDataMgr.sp_LD_SliceIndex, _lodIndex);

            Rend.SetPropertyBlock(_mpb.materialPropertyBlock);
        }

        private void Update()
        {
            // This needs to be called on Update because the bounds depend on transform scale which can change. Also OnWillRenderObject depends on
            // the bounds being correct. This could however be called on scale change events, but would add slightly more complexity.
            UpdateMeshBounds();
        }

        void UpdateMeshBounds()
        {
            if (WaterBody.WaterBodies.Count > 0)
            {
                _unexpandedBoundsXZ = ComputeBoundsXZ(transform, ref _boundsLocal);
            }

            var newBounds = _boundsLocal;
            ExpandBoundsForDisplacements(transform, ref newBounds);
            _mesh.bounds = newBounds;
        }

        public static Rect ComputeBoundsXZ(Transform transform, ref Bounds bounds)
        {
            // Since chunks are axis-aligned it is safe to rotate the bounds.
            var center = transform.rotation * bounds.center * transform.lossyScale.x + transform.position;
            var size = transform.rotation * bounds.size * transform.lossyScale.x;
            // Rotation can make size negative.
            return new Rect(0, 0, Mathf.Abs(size.x), Mathf.Abs(size.z))
            {
                center = center.XZ(),
            };
        }

        static Camera _currentCamera = null;

        private static void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // Camera.current is only supported in the built-in pipeline. This provides the current camera for
            // OnWillRenderObject for SRPs. BeginCameraRendering is called for each active camera in every frame.
            // OnWillRenderObject is called after BeginCameraRendering for the current camera so this works.
            _currentCamera = camera;
        }

        // Used by the ocean mask system if we need to render the ocean mask in situations
        // where the ocean itself doesn't need to be rendered or has otherwise been disabled
        internal void BindOceanData(Camera camera)
        {
            _oceanDataHasBeenBound = true;
            if (OceanRenderer.Instance == null || Rend == null)
            {
                return;
            }

            if (!MaterialOverridden && Rend.sharedMaterial != OceanRenderer.Instance.OceanMaterial)
            {
                Rend.sharedMaterial = OceanRenderer.Instance.OceanMaterial;
            }

            // per instance data

            if (_mpb == null)
            {
                _mpb = new PropertyWrapperMPB();
            }
            Rend.GetPropertyBlock(_mpb.materialPropertyBlock);

            // Only done here because current camera is defined. This could be done just once, probably on the OnRender function
            // or similar on the OceanPlanarReflection script?
            var reflTex = PreparedReflections.GetRenderTexture(camera.GetHashCode());
            if (reflTex)
            {
                _mpb.SetTexture(sp_ReflectionTex, reflTex);
            }
            else
            {
                _mpb.SetTexture(sp_ReflectionTex, Texture2D.blackTexture);
            }

            Rend.SetPropertyBlock(_mpb.materialPropertyBlock);
        }


        // Called when visible to a camera
        void OnWillRenderObject()
        {
            // Camera.current is only supported in built-in pipeline.
            if (Camera.current != null)
            {
                _currentCamera = Camera.current;
            }

            // If only the game view is visible, this reference will be dropped for SRP on recompile.
            if (_currentCamera == null)
            {
                return;
            }

            // Depth texture is used by ocean shader for transparency/depth fog, and for fading out foam at shoreline.
            _currentCamera.depthTextureMode |= DepthTextureMode.Depth;

            BindOceanData(_currentCamera);

            if (_drawRenderBounds)
            {
                Rend.bounds.DebugDraw();
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

        public void SetInstanceData(int lodIndex)
        {
            _lodIndex = lodIndex;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            _currentCamera = null;
        }

        [RuntimeInitializeOnLoadMethod]
        static void RunOnStart()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Rend.bounds.GizmosDraw();

            if (WaterBody.WaterBodies.Count > 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube
                (
                    _unexpandedBoundsXZ.center.XNZ(transform.position.y),
                    _unexpandedBoundsXZ.size.XNZ()
                );
            }
        }

        private void OnDrawGizmos()
        {
            if (_drawRenderBounds)
            {
                Rend.bounds.GizmosDraw();
            }
        }
#endif
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

        public static void GizmosDraw(this Bounds b)
        {
            var xmin = b.min.x;
            var ymin = b.min.y;
            var zmin = b.min.z;
            var xmax = b.max.x;
            var ymax = b.max.y;
            var zmax = b.max.z;

            Gizmos.DrawLine(new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymin, zmax));
            Gizmos.DrawLine(new Vector3(xmin, ymin, zmin), new Vector3(xmax, ymin, zmin));
            Gizmos.DrawLine(new Vector3(xmax, ymin, zmax), new Vector3(xmin, ymin, zmax));
            Gizmos.DrawLine(new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymin, zmin));

            Gizmos.DrawLine(new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymax, zmax));
            Gizmos.DrawLine(new Vector3(xmin, ymax, zmin), new Vector3(xmax, ymax, zmin));
            Gizmos.DrawLine(new Vector3(xmax, ymax, zmax), new Vector3(xmin, ymax, zmax));
            Gizmos.DrawLine(new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymax, zmin));

            Gizmos.DrawLine(new Vector3(xmax, ymax, zmax), new Vector3(xmax, ymin, zmax));
            Gizmos.DrawLine(new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymax, zmin));
            Gizmos.DrawLine(new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymax, zmin));
            Gizmos.DrawLine(new Vector3(xmin, ymax, zmax), new Vector3(xmin, ymin, zmax));
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(OceanChunkRenderer)), CanEditMultipleObjects]
    public class OceanChunkRendererEditor : CustomBaseEditor
    {
        Renderer renderer;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var target = this.target as OceanChunkRenderer;

            if (renderer == null)
            {
                renderer = target.GetComponent<Renderer>();
            }

            GUI.enabled = false;
            var boundsXZ = new Bounds(target._unexpandedBoundsXZ.center.XNZ(), target._unexpandedBoundsXZ.size.XNZ());
            EditorGUILayout.BoundsField("Bounds XZ", boundsXZ);
            EditorGUILayout.BoundsField("Expanded Bounds", renderer.bounds);
            GUI.enabled = true;
        }
    }
#endif
}
