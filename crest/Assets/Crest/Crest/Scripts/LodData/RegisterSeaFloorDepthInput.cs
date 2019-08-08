// Crest Ocean System

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

        public override float Wavelength => 0f;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_assignOceanDepthMaterial)
            {
                var rend = GetComponent<Renderer>();
                rend.material = new Material(Shader.Find("Crest/Inputs/Depth/Ocean Depth From Geometry"));
            }
        }
    }


    /// <summary>
    /// Tags this object as an ocean depth provider with geometry shader support (for advanced users).
    ///
    /// This is similar to RegisterSeaFloorDepthInput, but allows geometry depth to be rendered to all lod levels in a
    /// single dispatch. The Ocean Depth Cache supports this mode of rendering, so use it's implementation as a
    /// reference on how to do this.
    /// </summary>
    public class RegisterSeaFloorDepthInputGeometry : RegisterLodDataInput<LodDataMgrSeaFloorDepth>
    {
        [SerializeField] bool _assignOceanDepthMaterial = true;

        public override float Wavelength => 0f;
        public override LodDataInputType LodDataInputType => LodDataInputType.Geometry;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_assignOceanDepthMaterial)
            {
                var rend = GetComponent<Renderer>();
                rend.material = new Material(Shader.Find("Crest/Inputs/Depth/Ocean Depth From Geometry"));
            }
        }
    }
}
