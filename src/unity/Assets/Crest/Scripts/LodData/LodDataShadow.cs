// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class LodDataShadow : LodData
    {
        public readonly static int FIRST_SHADOW_LOD = 2;

        public override SimType LodDataType { get { return SimType.Shadow; } }
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

            // make specific variants of some of the params - because the shadow loddata will be different scales etc - they don't track the
            // geometry 1:1
            CreateParamIDs(ref _paramsOceanParams, "_LD_Shadow_Params_");
            CreateParamIDs(ref _paramsPosScaleScaleAlpha, "_LD_Shadow_Pos_Scale_ScaleAlpha_");
            CreateParamIDs(ref _paramsLodIdx, "_LD_Shadow_LodIdx_");

            // This lod data does some pretty special stuff with the render sim geometry - it uses it to actually compute the shadows

            // make sure shadow proxies draw into our camera
            UnityEditor.ArrayUtility.Add(ref GetComponent<ApplyLayers>()._cullIncludeLayers, "ShadowProxy");

            // only create the shadow catcher for the bigger lod
            if (LodTransform.LodIndex == FIRST_SHADOW_LOD + 1)
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

        public static void BindNoShadows(int slot, MaterialPropertyBlock properties)
        {
            var shadow = OceanRenderer.Instance.Builder._lodDataAnimWaves[FIRST_SHADOW_LOD].LDShadow;
            if (shadow == null) return;
            properties.SetTexture(shadow._paramsLodDataSampler[slot], Texture2D.whiteTexture);
        }

        public static void BindToOceanMaterial(MaterialPropertyBlock mpb)
        {
            var shapeCams = OceanRenderer.Instance.Builder._lodDataAnimWaves;

            if (!shapeCams[FIRST_SHADOW_LOD + 0].LDShadow || !shapeCams[FIRST_SHADOW_LOD + 1].LDShadow)
            {
                BindNoShadows(0, mpb);
                BindNoShadows(1, mpb);
            }
            else
            {
                shapeCams[FIRST_SHADOW_LOD + 0].LDShadow.BindResultData(0, mpb);
                shapeCams[FIRST_SHADOW_LOD + 1].LDShadow.BindResultData(1, mpb);
            }
        }
    }
}
