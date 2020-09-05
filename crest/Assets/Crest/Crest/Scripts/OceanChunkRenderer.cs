// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    /// <summary>
    /// Sets shader parameters for each geometry tile/chunk.
    /// </summary>
    [ExecuteAlways]
    public class OceanChunkRenderer : MonoBehaviour
    {
        public bool _drawRenderBounds = false;

        public Bounds _boundsLocal;
        Mesh _mesh;
        public Renderer Rend { get; private set; }
        PropertyWrapperMPB _mpb;

        // Cache these off to support regenerating ocean surface
        int _lodIndex = -1;

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
            var newBounds = _boundsLocal;
            ExpandBoundsForDisplacements(transform, ref newBounds);
            _mesh.bounds = newBounds;
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

        private void OnDrawGizmos()
        {
            if (_drawRenderBounds)
            {
                Rend.bounds.GizmosDraw();
            }
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
    public class OceanChunkRendererEditor : Editor
    {
        Renderer renderer;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var oceanChunkRenderer = target as OceanChunkRenderer;

            if (renderer == null)
            {
                renderer = oceanChunkRenderer.GetComponent<Renderer>();
            }

            GUI.enabled = false;
            EditorGUILayout.BoundsField("Expanded Bounds", renderer.bounds);
            GUI.enabled = true;
        }
    }
#endif
}
