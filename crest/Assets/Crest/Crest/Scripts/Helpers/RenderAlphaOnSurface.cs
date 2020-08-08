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
        OceanRenderer _ocean;

        private void Start()
        {
            _rend = GetComponent<Renderer>();
            _mesh = GetComponent<MeshFilter>().mesh;
            _boundsLocal = _mesh.bounds;
            _ocean = OceanRenderer.ClosestInstance(transform.position);

            if (_ocean != null)
            {
                LateUpdateBounds();
            }
        }

        private void LateUpdate()
        {
            _ocean = OceanRenderer.ClosestInstance(transform.position);
            if (_ocean == null)
            {
                return;
            }

            // find which lod this object is overlapping
            var rect = new Rect(transform.position.x, transform.position.z, 0f, 0f);
            var lodIdx = LodDataMgrAnimWaves.SuggestDataLOD(_ocean, rect);

            if (lodIdx > -1)
            {
                if (_mpb == null)
                {
                    _mpb = new PropertyWrapperMPB();
                }

                _rend.GetPropertyBlock(_mpb.materialPropertyBlock);

                var lodCount = _ocean.CurrentLodCount;
                var lodDataAnimWaves = _ocean._lodDataAnimWaves;
                _mpb.SetInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                lodDataAnimWaves.BindResultData(_mpb);
                var lodDataClipSurface = _ocean._lodDataClipSurface;
                if (lodDataClipSurface != null)
                {
                    lodDataClipSurface.BindResultData(_mpb);
                }
                else
                {
                    LodDataMgrClipSurface.BindNull(_mpb);
                }

                // blend LOD 0 shape in/out to avoid pop, if the ocean might scale up later (it is smaller than its maximum scale)
                bool needToBlendOutShape = lodIdx == 0 && _ocean.ScaleCouldIncrease;
                float meshScaleLerp = needToBlendOutShape ? _ocean.ViewerAltitudeLevelAlpha : 0f;

                // blend furthest normals scale in/out to avoid pop, if scale could reduce
                bool needToBlendOutNormals = lodIdx == lodCount - 1 && _ocean.ScaleCouldDecrease;
                float farNormalsWeight = needToBlendOutNormals ? _ocean.ViewerAltitudeLevelAlpha : 1f;
                _mpb.SetVector(OceanChunkRenderer.sp_InstanceData, new Vector3(meshScaleLerp, farNormalsWeight, lodIdx));

                _rend.SetPropertyBlock(_mpb.materialPropertyBlock);
            }

            LateUpdateBounds();
        }

        void LateUpdateBounds()
        {
            // make sure we're at sea level. we will expand the bounds which only works at sea level
            float y = transform.position.y;
            if (!Mathf.Approximately(y, _ocean.SeaLevel))
            {
                transform.position += (_ocean.SeaLevel - y) * Vector3.up;
            }

            var bounds = _boundsLocal;
            OceanChunkRenderer.ExpandBoundsForDisplacements(_ocean, transform, ref bounds);
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
