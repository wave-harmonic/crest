// Crest Ocean System

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
        public override string SimName { get { return "Flow"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }
        protected override bool NeedToReadWriteTextureData { get { return false; } }

        public SimSettingsFlow Settings { get { return OceanRenderer.Instance._simSettingsFlow; } }
        public override void UseSettings(SimSettingsBase settings) { OceanRenderer.Instance._simSettingsFlow = settings as SimSettingsFlow; }
        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFlow>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        bool _targetsClear = false;

        const string FLOW_KEYWORD = "_FLOW_ON";

        protected override void Start()
        {
            base.Start();

#if UNITY_EDITOR
            if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled(FLOW_KEYWORD))
            {
                Debug.LogWarning("Flow is not enabled on the current ocean material and will not be visible.", this);
            }
#endif
        }

        private void OnEnable()
        {
            Shader.EnableKeyword(FLOW_KEYWORD);
        }

        private void OnDisable()
        {
            Shader.DisableKeyword(FLOW_KEYWORD);
        }

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
                buf.SetRenderTarget(_targets, 0, CubemapFace.Unknown, lodIdx);
                buf.ClearRenderTarget(false, true, Color.black);
                buf.SetGlobalFloat("_LD_SLICE_Index_ThisLod", lodIdx);
                SubmitDraws(lodIdx, buf);
            }

            // targets have now been cleared, we can early out next time around
            if (_drawList.Count == 0)
            {
                _targetsClear = true;
            }
        }

        // TODO(Factor these out to be shared with other classes who have same code
        public static string TextureArrayName = "_LD_TexArray_Flow_";
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
            properties.SetTexture(ParamIdSampler(prevFrame), Texture2D.blackTexture);
        }
    }
}
