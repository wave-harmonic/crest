// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    // this doesnt need to be loddatapersistent - it just needs to create its own "rendersim" geometry that catches shadows
    public class LodDataSSS : LodDataPersistent
    {
        public override SimType LodDataType { get { return SimType.SubSurfaceScattering; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/Render Shadow Attenuation"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }
        protected override Camera[] SimCameras { get { return OceanRenderer.Instance.Builder._camsSSS; } }
        public override SimSettingsBase CreateDefaultSettings() { return null; }

        protected override void Start()
        {
            base.Start();

            // This lod data does some pretty special stuff with the render sim geometry - it uses it to actually compute the shadows

            // make sure shadow proxies draw into our camera
            UnityEditor.ArrayUtility.Add(ref GetComponent<ApplyLayers>()._cullIncludeLayers, "ShadowProxy");

            // only create the shadow catcher for the biggest lod
            if (LodTransform.LodIndex == LodTransform.LodCount-1)
            {
                // place at sea level
                _renderSim.transform.localPosition = 100f * Vector3.forward;

                var rend = _renderSim.GetComponent<Renderer>();
                // draw the renderer - we can't use the commandbuffer because unity refuses to set it up and make it work nice
                rend.enabled = true;
                // make sure it receives shadows
                rend.receiveShadows = true;
            }
            else
            {
                // destroy all the others, otherwise theyll all render into EVERY lod camera
                Destroy(_renderSim);
            }

            // for all lods signal that we are taking control of it, and the underlying code will not create any command buffers
            _renderSim = null;
        }
    }
}
