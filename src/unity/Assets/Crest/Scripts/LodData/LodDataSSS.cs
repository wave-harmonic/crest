// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class LodDataSSS : LodData
    {
        public override SimType LodDataType { get { return SimType.SubSurfaceScattering; } }
        public override SimSettingsBase CreateDefaultSettings() { return null; }
        public override void UseSettings(SimSettingsBase settings) { }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.R8; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Color; } }
        public override RenderTexture DataTexture { get { return Cam.targetTexture; } }

        readonly string _shaderRenderShadows = "Ocean/Shape/Sim/Render Shadow Attenuation";

        GameObject _renderQuad;

        protected override void Start()
        {
            base.Start();

            // This lod data does some pretty special stuff with the render sim geometry - it uses it to actually compute the shadows

            // make sure shadow proxies draw into our camera
            UnityEditor.ArrayUtility.Add(ref GetComponent<ApplyLayers>()._cullIncludeLayers, "ShadowProxy");

            // only create the shadow catcher for the biggest lod
            if (LodTransform.LodIndex == LodTransform.LodCount - 1)
            {
                // utility quad which will be rasterized by the shape camera
                _renderQuad = CreateRasterQuad("RenderSim_" + SimName);
                _renderQuad.transform.parent = transform;
                _renderQuad.transform.localScale = Vector3.one * 4f;
                // place at sea level
                _renderQuad.transform.localPosition = Vector3.forward * 100f;
                _renderQuad.transform.localRotation = Quaternion.identity;

                // we let the renderer draw - we can't use the commandbuffer because unity refuses to set it up and make it work nice
                var rend = _renderQuad.GetComponent<Renderer>();
                rend.material = new Material(Shader.Find(_shaderRenderShadows));
                // make sure it receives shadows
                rend.receiveShadows = true;

                _renderQuad.AddComponent<ApplyLayers>()._layerName = GetComponent<ApplyLayers>()._layerName;
            }
        }
    }
}
