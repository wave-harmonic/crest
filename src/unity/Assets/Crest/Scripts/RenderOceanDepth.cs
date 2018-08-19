using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Tags this object as an ocean depth provider. Renders depth every frame and should only be used for dynamic objects.
    /// For static objects, use an Ocean Depth Cache.
    /// </summary>
    public class RenderOceanDepth : MonoBehaviour
    {
        void Start()
        {
            RefreshOceanDepthRenderers();
        }

        void OnDisable()
        {
            RefreshOceanDepthRenderers();
        }

        void RefreshOceanDepthRenderers()
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance.Builder == null)
                return;

            // notify WDCs that there is a new contributor to ocean depth
            foreach (var ldaw in OceanRenderer.Instance.Builder._lodDataAnimWaves)
            {
                ldaw.LDSeaDepth.OnOceanDepthRenderersChanged();
            }
        }
    }
}
