// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Experiment - store clip values on water, instead of using depth mask which doesnt work in HDRP, and is awkward to use. Notes:
// - The clip values only really need 8bits. Should they be stored in a different texture? I guess this would mean a different LOD
//   data. The clip values are sampled from the fragment shader unlike the depths, so it makes sense from a perf perspective. Also
//   it's pretty awkward attaching a 'Register Ocean Depth Input' to the clipping shader, so a new LOD data would probably be good
//   for that.
// - Does this clipping work on both URP and HDRP, using the alpha cutoff feature?
// - I've set precision to be half, from float. Was it float because depth used to be 'height' (inverted)?

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Renders depth of the ocean (height of sea level above ocean floor), by rendering the relative height of tagged objects from top down.
    /// </summary>
    public class LodDataMgrSeaFloorDepth : LodDataMgr
    {
        public override string SimName { get { return "SeaFloorDepth"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }
        protected override bool NeedToReadWriteTextureData { get { return false; } }

        public override SimSettingsBase CreateDefaultSettings() { return null; }
        public override void UseSettings(SimSettingsBase settings) { }

        bool _targetsClear = false;

        public const string ShaderName = "Crest/Inputs/Depth/Cached Depths";

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            // If there is nothing in the scene tagged up for depth rendering, and we have cleared the RTs, then we can early out
            var drawList = RegisterLodDataInputBase.GetRegistrar(GetType());
            if (drawList.Count == 0 && _targetsClear)
            {
                return;
            }

            for (int lodIdx = OceanRenderer.Instance.CurrentLodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(_targets, 0, CubemapFace.Unknown, lodIdx);
                buf.ClearRenderTarget(false, true, Color.red * 1000f);
                buf.SetGlobalInt(sp_LD_SliceIndex, lodIdx);
                SubmitDraws(lodIdx, buf);
            }

            // Targets are only clear if nothing was drawn
            _targetsClear = drawList.Count == 0;
        }

        public static string TextureArrayName = "_LD_TexArray_SeaFloorDepth";
        private static TextureArrayParamIds textureArrayParamIds = new TextureArrayParamIds(TextureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) { return textureArrayParamIds.GetId(sourceLod); }
        protected override int GetParamIdSampler(bool sourceLod = false)
        {
            return ParamIdSampler(sourceLod);
        }
        public static void BindNull(IPropertyWrapper properties, bool sourceLod = false)
        {
            properties.SetTexture(ParamIdSampler(sourceLod), TextureArrayHelpers.BlackTextureArray);
        }
    }
}
