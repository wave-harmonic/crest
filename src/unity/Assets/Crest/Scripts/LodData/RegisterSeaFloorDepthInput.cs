// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Tags this object as an ocean depth provider. Renders depth every frame and should only be used for dynamic objects.
    /// For static objects, use an Ocean Depth Cache.
    /// </summary>
    public class RegisterSeaFloorDepthInput : RegisterLodDataInput<LodDataMgrSeaFloorDepth>
    {
        [SerializeField] bool _assignOceanDepthMaterial = true;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_assignOceanDepthMaterial)
            {
                var rend = GetComponent<Renderer>();
                rend.material = new Material(Shader.Find("Ocean/Inputs/Depth/Ocean Depth From Geometry"));
            }
        }
    }
}
