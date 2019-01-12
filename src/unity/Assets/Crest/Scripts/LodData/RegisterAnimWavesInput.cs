// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections;
using UnityEngine;

namespace Crest
{
    public class RegisterAnimWavesInput : RegisterLodDataInput<LodDataMgrAnimWaves>
    {
        [SerializeField, Tooltip("Which octave to render into, for example set this to 2 to use render into the 2m-4m octave. These refer to the same octaves as the wave spectrum editor. Set this value to 0 to render into all LODs.")]
        float _octaveWavelength = 0f;
        public float OctaveWavelength
        {
            get
            {
                return _octaveWavelength;
            }
        }

        [SerializeField, Tooltip("Inform ocean how much this input will displace the shape vertically. This is used to set bounding box heights for the ocean tiles.")]
        float _maxDisplacementVertical = 0f;
        [SerializeField, Tooltip("Inform ocean how much this input will displace the shape horizontally. This is used to set bounding box widths for the ocean tiles.")]
        float _maxDisplacementHorizontal = 0f;

        [SerializeField, Tooltip("Use the bounding box of an attached renderer component to determine the max vertical displacement.")]
        bool _reportRendererBoundsToOceanSystem = false;

        Renderer _rend;

        private void Start()
        {
            if (_reportRendererBoundsToOceanSystem || _maxDisplacementVertical > 0f || _maxDisplacementHorizontal > 0f)
            {
                _rend = GetComponent<Renderer>();

                StartCoroutine(ReportDisplacements());
            }
        }

        IEnumerator ReportDisplacements()
        {
            while (true)
            {
                var maxDispVert = 0f;

                // let ocean system know how far from the sea level this shape may displace the surface
                if (_rend != null)
                {
                    var minY = _rend.bounds.min.y;
                    var maxY = _rend.bounds.max.y;
                    var seaLevel = OceanRenderer.Instance.SeaLevel;
                    maxDispVert = Mathf.Max(Mathf.Abs(seaLevel - minY), Mathf.Abs(seaLevel - maxY));
                }

                maxDispVert = Mathf.Max(maxDispVert, _maxDisplacementVertical);

                if (_maxDisplacementHorizontal > 0f || _maxDisplacementVertical > 0f)
                {
                    OceanRenderer.Instance.ReportMaxDisplacementFromShape(_maxDisplacementHorizontal, maxDispVert);
                }

                yield return new WaitForEndOfFrame();
            }
        }
    }
}
