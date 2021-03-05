// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Crest
{
    using SettingsType = SimSettingsFlow;

    /// <summary>
    /// A persistent flow simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataMgrFlow : LodDataMgr
    {
        public override string SimName { get { return "Flow"; } }
        protected override GraphicsFormat RequestedTextureFormat => GraphicsFormat.R16G16_SFloat;
        protected override bool NeedToReadWriteTextureData { get { return false; } }

        internal const string MATERIAL_KEYWORD = MATERIAL_KEYWORD_PREFIX + "_FLOW_ON";
        internal const string ERROR_MATERIAL_KEYWORD_MISSING = "Flow must be enabled on the ocean material. Tick the <i>Enable</i> option in the <i>Flow</i> parameter section on the material currently assigned to the OceanRenderer component.";
        internal const string ERROR_MATERIAL_KEYWORD_ON_FEATURE_OFF = "The Flow feature is disabled on the this but is enabled on the ocean material. If this is not intentional, either enable the <i>Create Flow Data</i> option on this component to turn it on, or disable the Flow feature on the ocean material to save performance.";
        bool _targetsClear = false;

        public const string FLOW_KEYWORD = "CREST_FLOW_ON_INTERNAL";

        SettingsType _defaultSettings;
        public SettingsType Settings
        {
            get
            {
                if (_ocean._simSettingsFlow != null) return _ocean._simSettingsFlow;

                if (_defaultSettings == null)
                {
                    _defaultSettings = ScriptableObject.CreateInstance<SettingsType>();
                    _defaultSettings.name = SimName + " Auto-generated Settings";
                }
                return _defaultSettings;
            }
        }

        public LodDataMgrFlow(OceanRenderer ocean) : base(ocean)
        {
            Start();
        }

        public override void Start()
        {
            base.Start();

#if UNITY_EDITOR
            if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled(MATERIAL_KEYWORD))
            {
                Debug.LogWarning(ERROR_MATERIAL_KEYWORD_MISSING, _ocean);
            }
#endif
        }

        internal override void OnEnable()
        {
            base.OnEnable();

            Shader.EnableKeyword(FLOW_KEYWORD);
        }

        internal override void OnDisable()
        {
            base.OnDisable();

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

        public static void Bind(IPropertyWrapper properties)
        {
            if (OceanRenderer.Instance._lodDataFlow != null)
            {
                properties.SetTexture(OceanRenderer.Instance._lodDataFlow.GetParamIdSampler(), OceanRenderer.Instance._lodDataFlow.DataTexture);
            }
            else
            {
                properties.SetTexture(ParamIdSampler(), TextureArrayHelpers.BlackTextureArray);
            }
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
