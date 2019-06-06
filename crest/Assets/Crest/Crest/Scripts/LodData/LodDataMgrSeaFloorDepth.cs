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
            buf.ClearRenderTarget(false, true,  Color.white * 1000f);

            Matrix4x4[] matrixArray = new Matrix4x4[MAX_LOD_COUNT];

            for (int lodIdx = OceanRenderer.Instance.CurrentLodCount - 1; lodIdx >= 0; lodIdx--)
            {
                var lt = OceanRenderer.Instance._lods[lodIdx];
                lt._renderData.Validate(0, this);

                Matrix4x4 worldToClipPos = lt.ProjectionMatrix * lt.WorldToCameraMatrix;
                // TODO (TRC): for some reason, the projection matrix that is sent to the
                // shader by `SetViewProjectionMatrices` in the command buffer has
                // it's middle two rows inverted, which then propagates to the
                // worldToClipPos matrix. We need to find out why this is so
                // this hacky stuff does not need to happen
                worldToClipPos.SetRow(1, worldToClipPos.GetRow(1) * -1);
                worldToClipPos.SetRow(2, worldToClipPos.GetRow(2) * -1);
                matrixArray[lodIdx] = worldToClipPos;
            }

            buf.SetGlobalMatrixArray("_SliceViewProjMatrices", matrixArray);
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

        // TODO(Factor these out to be shared with other classes who have same code
        public static string TextureArrayName = "_LD_TexArray_SeaFloorDepth_";
        public static int ParamIDTextureArray_ThisFrame = Shader.PropertyToID(TextureArrayName + "ThisFrame");
        public static int ParamIDTextureArray_PrevFrame = Shader.PropertyToID(TextureArrayName + "PrevFrame");
        public static int ParamIdSampler(bool prevFrame = false)
        {
            if(prevFrame)
            {
                return ParamIDTextureArray_PrevFrame;
            }
            else
            {
                return ParamIDTextureArray_ThisFrame;
            }
        }
        protected override int GetParamIdSampler(bool prevFrame = false)
        {
            return ParamIdSampler(prevFrame);
        }
        public static void BindNull(IPropertyWrapper properties, bool prevFrame = false)
        {
            properties.SetTexture(ParamIdSampler(prevFrame), TextureArray.Black);
        }
    }
}
