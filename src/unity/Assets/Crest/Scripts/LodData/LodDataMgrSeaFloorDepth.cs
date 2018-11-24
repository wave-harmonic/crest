using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Renders relative depth of ocean floor, by rendering the relative height of tagged objects from top down. This loddata rides
    /// on the LodDataAnimatedWaves currently.
    /// </summary>
    public class LodDataMgrSeaFloorDepth : LodDataMgr
    {
        public override SimType LodDataType { get { return SimType.SeaFloorDepth; } }
        public override SimSettingsBase CreateDefaultSettings() { return null; }
        public override void UseSettings(SimSettingsBase settings) { }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RFloat; } }

        bool _targetsClear = false;

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            // if there is nothing in the scene tagged up for depth rendering, and we have cleared the RTs, then we can early out
            if (_drawList.Count == 0 && _targetsClear)
            {
                return;
            }

            for (int lodIdx = OceanRenderer.Instance.CurrentLodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(_targets[lodIdx]);
                buf.ClearRenderTarget(false, true, Color.black);

                SubmitDraws(lodIdx, buf);
            }

            // targets have now been cleared, we can early out next time around
            if(_drawList.Count == 0)
            {
                _targetsClear = true;
            }
        }
    }
}
