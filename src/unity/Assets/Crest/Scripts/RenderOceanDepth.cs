using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Tags this object as an ocean depth provider. Renders depth every frame and should only be used for dynamic objects.
    /// For static objects, use an Ocean Depth Cache.
    /// </summary>
    public class RenderOceanDepth : MonoBehaviour
    {
        private void OnEnable()
        {
            var mat = new Material(Shader.Find("Ocean/Ocean Depth"));
            LodDataSeaFloorDepth.AddRenderOceanDepth(GetComponent<Renderer>(), mat);
        }

        private void OnDisable()
        {
            LodDataSeaFloorDepth.RemoveRenderOceanDepth(GetComponent<Renderer>());
        }

        public void SetMaterial(Material mat)
        {
            var rend = GetComponent<Renderer>();
            LodDataSeaFloorDepth.RemoveRenderOceanDepth(rend);
            LodDataSeaFloorDepth.AddRenderOceanDepth(rend, mat);
        }
    }
}
