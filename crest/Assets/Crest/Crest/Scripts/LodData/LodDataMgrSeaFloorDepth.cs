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

        bool _targetsClear = false;
        private static int sp_SliceViewProjMatrices = Shader.PropertyToID("_SliceViewProjMatrices");
        private static int sp_CurrentLodCount = Shader.PropertyToID("_CurrentLodCount");

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            // if there is nothing in the scene tagged up for depth rendering, and we have cleared the RTs, then we can early out
            if (_drawList.Count == 0 && _targetsClear)
            {
                return;
            }

            Debug.Assert(OceanRenderer.Instance.CurrentLodCount < MAX_LOD_COUNT);

            buf.SetRenderTarget(_targets, 0, CubemapFace.Unknown, -1);
            buf.ClearRenderTarget(false, true, Color.white * 1000f);

            Matrix4x4[] matrixArray = new Matrix4x4[MAX_LOD_COUNT];

            var lt = OceanRenderer.Instance._lodTransform;
            for (int lodIdx = OceanRenderer.Instance.CurrentLodCount - 1; lodIdx >= 0; lodIdx--)
            {
                lt._renderData[lodIdx].Validate(0, this);
                Matrix4x4 platformProjectionMatrix = GL.GetGPUProjectionMatrix(lt.GetProjectionMatrix(lodIdx), true);
                Matrix4x4 worldToClipPos = platformProjectionMatrix * lt.GetWorldToCameraMatrix(lodIdx);
                matrixArray[lodIdx] = worldToClipPos;
            }

            buf.SetGlobalMatrixArray(sp_SliceViewProjMatrices, matrixArray);
            buf.SetGlobalInt(sp_CurrentLodCount, OceanRenderer.Instance.CurrentLodCount);


            foreach (var draw in _drawList)
            {
                buf.DrawRenderer(draw.RendererComponent, draw.RendererComponent.sharedMaterial);
            }

            // targets have now been cleared, we can early out next time around
            if (_drawList.Count == 0)
            {
                _targetsClear = true;
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
