// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Crest
{
    using SettingsType = SimSettingsAlbedo;

    /// <summary>
    /// A colour layer that can be composited onto the water surface.
    /// </summary>
    public class LodDataMgrAlbedo : LodDataMgr
    {
        public override string SimName => "Albedo";
        protected override GraphicsFormat RequestedTextureFormat => GraphicsFormat.R8G8B8A8_UNorm;
        protected override bool NeedToReadWriteTextureData => false;
        static Texture2DArray s_nullTexture => TextureArrayHelpers.BlackTextureArray;
        protected override Texture2DArray NullTexture => s_nullTexture;
        protected override int ResolutionOverride => Settings._resolution;

        internal static readonly string MATERIAL_KEYWORD_PROPERTY = "_Albedo";
        internal static readonly string MATERIAL_KEYWORD = MATERIAL_KEYWORD_PREFIX + "_ALBEDO_ON";
        internal const string ERROR_MATERIAL_KEYWORD_MISSING = "Albedo is not enabled on the ocean material and will not be visible.";
        internal const string ERROR_MATERIAL_KEYWORD_MISSING_FIX = "Tick the <i>Enable</i> option in the <i>Albedo</i> parameter section on the material currently assigned to the <i>OceanRenderer</i> component.";
        internal const string ERROR_MATERIAL_KEYWORD_ON_FEATURE_OFF = "The Albedo feature is disabled on the this but is enabled on the ocean material.";
        internal const string ERROR_MATERIAL_KEYWORD_ON_FEATURE_OFF_FIX = "If this is not intentional, either enable the <i>Create Albedo Data</i> option on this component to turn it on, or disable the <i>Albedo</i> feature on the ocean material to save performance.";
        bool _targetsClear = false;

        public override SimSettingsBase SettingsBase => Settings;
        public SettingsType Settings => _ocean._settingsAlbedo != null ? _ocean._settingsAlbedo : GetDefaultSettings<SettingsType>();

        public LodDataMgrAlbedo(OceanRenderer ocean) : base(ocean)
        {
            Start();
        }

        public override void Start()
        {
            base.Start();

#if UNITY_EDITOR
            if (OceanRenderer.Instance.OceanMaterial != null
                && OceanRenderer.Instance.OceanMaterial.HasProperty(MATERIAL_KEYWORD_PROPERTY)
                && !OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled(MATERIAL_KEYWORD))
            {
                Debug.LogWarning("Crest: " + ERROR_MATERIAL_KEYWORD_MISSING + " " + ERROR_MATERIAL_KEYWORD_MISSING_FIX, _ocean);
            }
#endif
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
                buf.SetRenderTarget(_targets.Current, 0, CubemapFace.Unknown, lodIdx);
                buf.ClearRenderTarget(false, true, Color.clear);
                buf.SetGlobalInt(sp_LD_SliceIndex, lodIdx);
                SubmitDraws(lodIdx, buf);
            }

            // Targets are only clear if nothing was drawn
            _targetsClear = drawList.Count == 0;
        }

        readonly static string s_textureArrayName = "_LD_TexArray_Albedo";
        private static TextureArrayParamIds s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) => s_textureArrayParamIds.GetId(sourceLod);
        protected override int GetParamIdSampler(bool sourceLod = false) => ParamIdSampler(sourceLod);

        public static void Bind(IPropertyWrapper properties)
        {
            if (OceanRenderer.Instance._lodDataAlbedo != null)
            {
                properties.SetTexture(OceanRenderer.Instance._lodDataAlbedo.GetParamIdSampler(), OceanRenderer.Instance._lodDataAlbedo.DataTexture);
            }
            else
            {
                properties.SetTexture(ParamIdSampler(), s_nullTexture);
            }
        }

        public static void BindNullToGraphicsShaders() => Shader.SetGlobalTexture(ParamIdSampler(), s_nullTexture);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        }
    }
}
