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

            Debug.Assert(OceanRenderer.Instance.CurrentLodCount < SLICE_COUNT);

            buf.SetRenderTarget(_targets, 0, CubemapFace.Unknown, -1);
            buf.ClearRenderTarget(false, true,  Color.white * 1000f);

            Matrix4x4[] matrixArray = new Matrix4x4[SLICE_COUNT];

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

        static int[] _paramsSampler;
        public static int ParamIdSampler(int slot)
        {
            if (_paramsSampler == null)
                LodTransform.CreateParamIDs(ref _paramsSampler, "_LD_TexArray_SeaFloorDepth_");
            return _paramsSampler[slot];
        }
        protected override int GetParamIdSampler(int slot)
        {
            return ParamIdSampler(slot);
        }
        public static void BindNull(int shapeSlot, IPropertyWrapper properties)
        {
            properties.SetTexture(ParamIdSampler(shapeSlot), Texture2D.blackTexture);
        }
    }
}
