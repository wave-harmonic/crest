// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    // todo - set bounds so that it doesnt cull
    public class UnderwaterSkirt : MonoBehaviour
    {
        MaterialPropertyBlock _mpb;
        Renderer _rend;
        Mesh _mesh;

        private void Start()
        {
            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }

            _rend = GetComponent<Renderer>();
            _mesh = GetComponent<MeshFilter>().mesh;
        }

        private void LateUpdate()
        {
            // underwater skirt always applies to LOD0
            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
            }

            _rend.GetPropertyBlock(_mpb);

            var ldaws = OceanRenderer.Instance._lodDataAnimWaves;
            ldaws[0].BindResultData(0, _mpb);

            _rend.SetPropertyBlock(_mpb);
        }
    }
}
