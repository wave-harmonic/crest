// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

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
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RFloat; } }
        protected override bool NeedToReadWriteTextureData { get { return false; } }

        public override SimSettingsBase CreateDefaultSettings() { return null; }
        public override void UseSettings(SimSettingsBase settings) { }

        // Depth render targets need to be cleared to a high value (so we can
        // store ocean depth directly). So we need to clear them at least once,
        // even if nothing is registered to render depth.
        bool _targetsHaveBeenCleared = false;

        private static int sp_SliceViewProjMatrices = Shader.PropertyToID("_SliceViewProjMatrices");
        private static int sp_CurrentLodCount = Shader.PropertyToID("_CurrentLodCount");

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            var gsDrawList = RegisterLodDataInputBase.GetRegistrar(GetType(), LodDataInputType.Geometry);
            var drawList = RegisterLodDataInputBase.GetRegistrar(GetType(), LodDataInputType.Conventional);

            if(_targetsHaveBeenCleared && gsDrawList.Count == 0 && drawList.Count == 0)
            {
                return;
            }

            buf.SetRenderTarget(_targets, 0, CubemapFace.Unknown, -1);
            buf.ClearRenderTarget(false, true, Color.white * 1000f);
            if (gsDrawList.Count != 0)
            {

                Matrix4x4[] matrixArray = new Matrix4x4[MAX_LOD_COUNT];

                var lt = OceanRenderer.Instance._lodTransform;
                for (int lodIdx = OceanRenderer.Instance.CurrentLodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    lt._renderData[lodIdx].Validate(0, this);
                    Matrix4x4 platformProjectionMatrix = GL.GetGPUProjectionMatrix(lt.GetProjectionMatrix(lodIdx), true);
                    Matrix4x4 worldToClipPos = platformProjectionMatrix * lt.GetWorldToCameraMatrix(lodIdx);
                    matrixArray[lodIdx] = worldToClipPos;
                }

                // TODO: We might want to make these more globally available for other LodData types
                buf.SetGlobalMatrixArray(sp_SliceViewProjMatrices, matrixArray);
                buf.SetGlobalInt(sp_CurrentLodCount, OceanRenderer.Instance.CurrentLodCount);

                foreach (var draw in gsDrawList)
                {
                    draw.Draw(buf, 1f, 0);
                }
            }

            if(drawList.Count != 0)
            {
                for (int lodIdx = OceanRenderer.Instance.CurrentLodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    buf.SetGlobalFloat(OceanRenderer.sp_LD_SliceIndex, lodIdx);
                    SubmitDraws(lodIdx, buf);
                }
            }

            // targets have now been cleared, we can early out next time around
            if (drawList.Count == 0 && gsDrawList.Count == 0)
            {
                _targetsHaveBeenCleared = true;
            }
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
