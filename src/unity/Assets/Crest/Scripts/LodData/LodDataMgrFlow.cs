// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A persistent flow simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataMgrFlow : LodDataMgr
    {
        public override LodData.SimType LodDataType { get { return LodData.SimType.Flow; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }
        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Nothing; } }

        [SerializeField]
        protected SimSettingsFlow _settings;
        public override void UseSettings(SimSettingsBase settings) { _settings = settings as SimSettingsFlow; }
        public SimSettingsFlow Settings { get { return _settings as SimSettingsFlow; } }
        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFlow>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        protected override void Start()
        {
            base.Start();

#if UNITY_EDITOR
            if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_FLOW_ON"))
            {
                Debug.LogWarning("Flow is not enabled on the current ocean material and will not be visible.", this);
            }
#endif
        }

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            for (int lodIdx = OceanRenderer.Instance.CurrentLodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(DataTexture(lodIdx));
                buf.ClearRenderTarget(false, true, Color.black);

                var lt = OceanRenderer.Instance._lods[lodIdx];
                buf.SetViewProjectionMatrices(lt._worldToCameraMatrix, lt._projectionMatrix);
                foreach (var draw in _drawList)
                {
                    buf.DrawRenderer(draw, draw.material);
                }
            }
        }
    }
}
