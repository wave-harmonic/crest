// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Helper script for alpha geometry rendering on top of ocean surface. This is required to select the best
    /// LOD and assign the shape texture to the material.
    /// </summary>
    public class RenderAlphaOnSurface : MonoBehaviour
    {
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
            if(OceanRenderer.Instance == null)
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

                var lodCount = OceanRenderer.Instance.CurrentLodCount;
                var lodDataAnimWaves = OceanRenderer.Instance._lodDataAnimWaves;
                _mpb.SetInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                lodDataAnimWaves.BindResultData(_mpb);
                var lodDataClipSurface = OceanRenderer.Instance._lodDataClipSurface;
                if (lodDataClipSurface != null)
                {
                    lodDataClipSurface.BindResultData(_mpb);
                }
                else
                {
                    LodDataMgrClipSurface.BindNull(_mpb);
                }

                // blend LOD 0 shape in/out to avoid pop, if the ocean might scale up later (it is smaller than its maximum scale)
                bool needToBlendOutShape = lodIdx == 0 && OceanRenderer.Instance.ScaleCouldIncrease;
                float meshScaleLerp = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 0f;

                // blend furthest normals scale in/out to avoid pop, if scale could reduce
                bool needToBlendOutNormals = lodIdx == lodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease;
                float farNormalsWeight = needToBlendOutNormals ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
                _mpb.SetVector(OceanChunkRenderer.sp_InstanceData, new Vector3(meshScaleLerp, farNormalsWeight, lodIdx));

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
