using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Tags this object as an ocean depth provider. Renders depth every frame and should only be used for dynamic objects.
    /// For static objects, use an Ocean Depth Cache.
    /// </summary>
    public class RenderOceanDepth : MonoBehaviour
    {
        [SerializeField, Tooltip("Material to use to write depth. Leave null to use the default depth material.")]
        Material _renderDepthMaterial;

        private void OnEnable()
        {
            var mat = _renderDepthMaterial ?? new Material(Shader.Find("Ocean/Ocean Depth"));
            LodDataSeaFloorDepth.AddRenderOceanDepth(GetComponent<Renderer>(), mat);
        }

        private void OnDisable()
        {
            LodDataSeaFloorDepth.RemoveRenderOceanDepth(GetComponent<Renderer>());
        }

        public void SetMaterial(Material mat)
        {
            _renderDepthMaterial = mat;
            var rend = GetComponent<Renderer>();
            LodDataSeaFloorDepth.RemoveRenderOceanDepth(rend);
            LodDataSeaFloorDepth.AddRenderOceanDepth(rend, _renderDepthMaterial);
        }
    }
}
