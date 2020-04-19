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

        public const string FLOW_KEYWORD = "CREST_FLOW_ON_INTERNAL";

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
            var drawList = RegisterLodDataInputBase.GetRegistrar(GetType());
            if (drawList.Count == 0 && _targetsClear)
            {
                return;
            }

            for (int lodIdx = OceanRenderer.Instance.CurrentLodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(_targets, 0, CubemapFace.Unknown, lodIdx);
                buf.ClearRenderTarget(false, true, Color.black);
                buf.SetGlobalInt(sp_LD_SliceIndex, lodIdx);
                SubmitDraws(lodIdx, buf);
            }

            // targets have now been cleared, we can early out next time around
            if (drawList.Count == 0)
            {
                _targetsClear = true;
            }
        }

        readonly static string s_textureArrayName = "_LD_TexArray_Flow";
        private static TextureArrayParamIds s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) { return s_textureArrayParamIds.GetId(sourceLod); }
        protected override int GetParamIdSampler(bool sourceLod = false)
        {
            return ParamIdSampler(sourceLod);
        }
        public static void BindNull(IPropertyWrapper properties, bool sourceLod = false)
        {
            properties.SetTexture(ParamIdSampler(sourceLod), TextureArrayHelpers.BlackTextureArray);
        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        }
    }
}
