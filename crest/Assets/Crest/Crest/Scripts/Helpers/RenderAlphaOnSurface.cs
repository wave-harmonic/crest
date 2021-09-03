// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Helper script for alpha geometry rendering on top of ocean surface. This is required to select the best
    /// LOD and assign the shape texture to the material.
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Render Alpha On Surface")]
    public class RenderAlphaOnSurface : MonoBehaviour
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public bool _drawBounds = false;

        PropertyWrapperMPB _mpb;
        Renderer _rend;
        Mesh _mesh;
        Bounds _boundsLocal;

        private void Start()
        {
            _rend = GetComponent<Renderer>();
            _mesh = GetComponent<MeshFilter>().mesh;
            _boundsLocal = _mesh.bounds;

            if (OceanRenderer.Instance != null)
            {
                LateUpdateBounds();
            }
        }

        private void LateUpdate()
        {
            if (OceanRenderer.Instance == null)
            {
                return;
            }

            // find which lod this object is overlapping
            var rect = new Rect(transform.position.x, transform.position.z, 0f, 0f);
            var lodIdx = LodDataMgrAnimWaves.SuggestDataLOD(rect);

            if (lodIdx > -1)
            {
                if (_mpb == null)
                {
                    _mpb = new PropertyWrapperMPB();
                }

                _rend.GetPropertyBlock(_mpb.materialPropertyBlock);
                _mpb.SetInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                _rend.SetPropertyBlock(_mpb.materialPropertyBlock);
            }

            LateUpdateBounds();
        }

        void LateUpdateBounds()
        {
            // make sure we're at sea level. we will expand the bounds which only works at sea level
            float y = transform.position.y;
            if (!Mathf.Approximately(y, OceanRenderer.Instance.SeaLevel))
            {
                transform.position += (OceanRenderer.Instance.SeaLevel - y) * Vector3.up;
            }

            var bounds = _boundsLocal;
            OceanChunkRenderer.ExpandBoundsForDisplacements(transform, ref bounds);
            _mesh.bounds = bounds;

            if (_drawBounds)
            {
#if UNITY_EDITOR
                _rend.bounds.DebugDraw();
#endif
            }
        }
    }
}
