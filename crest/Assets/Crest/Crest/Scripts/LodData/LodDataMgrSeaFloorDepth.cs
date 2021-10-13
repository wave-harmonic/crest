// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Crest
{
    using SettingsType = SimSettingsSeaFloorDepth;

    /// <summary>
    /// Data that gives depth of the ocean (height of sea level above ocean floor). Stores terrain height and water level
    /// offset in x & y channels.
    /// </summary>
    public class LodDataMgrSeaFloorDepth : LodDataMgr
    {
        public override string SimName => "SeaFloorDepth";
        protected override GraphicsFormat RequestedTextureFormat => Settings._allowVaryingWaterLevel ? GraphicsFormat.R32G32_SFloat : GraphicsFormat.R16_SFloat;
        protected override bool NeedToReadWriteTextureData => false;
        // We want the clear colour to be the min terrain height (-1000m) in X, and sea level offset 0m in Y.
        readonly static Color s_nullColor = Color.red * -1000f;
        static Texture2DArray s_nullTexture;
        protected override Texture2DArray NullTexture => s_nullTexture;

        bool _targetsClear = false;

        public const string FEATURE_TOGGLE_NAME = "_createSeaFloorDepthData";
        public const string FEATURE_TOGGLE_LABEL = "Create Sea Floor Depth Data";

        public const string ShaderName = "Crest/Inputs/Depth/Cached Depths";

        public override SimSettingsBase SettingsBase => Settings;
        public SettingsType Settings => _ocean._simSettingsSeaFloorDepth != null ? _ocean._simSettingsSeaFloorDepth : GetDefaultSettings<SettingsType>();

        public LodDataMgrSeaFloorDepth(OceanRenderer ocean) : base(ocean)
        {
            Start();
        }

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            // If there is nothing in the scene tagged up for depth rendering, and we have cleared the RTs, then we can early out
            var drawList = RegisterLodDataInputBase.GetRegistrar(GetType());
            if (drawList.Count == 0 && _targetsClear)
            {
                return;
            }

            for (int lodIdx = OceanRenderer.Instance.CurrentLodCount - 1; lodIdx >= 0; lodIdx--)
            {
                buf.SetRenderTarget(_targets.Current, 0, CubemapFace.Unknown, lodIdx);
                buf.ClearRenderTarget(false, true, s_nullColor);
                buf.SetGlobalInt(sp_LD_SliceIndex, lodIdx);
                SubmitDraws(lodIdx, buf);
            }

            // Targets are only clear if nothing was drawn
            _targetsClear = drawList.Count == 0;
        }

        readonly static string s_textureArrayName = "_LD_TexArray_SeaFloorDepth";
        private static TextureArrayParamIds s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) => s_textureArrayParamIds.GetId(sourceLod);
        protected override int GetParamIdSampler(bool sourceLod = false) => ParamIdSampler(sourceLod);

        public static void Bind(IPropertyWrapper properties)
        {
            if (OceanRenderer.Instance._lodDataSeaDepths != null)
            {
                properties.SetTexture(OceanRenderer.Instance._lodDataSeaDepths.GetParamIdSampler(), OceanRenderer.Instance._lodDataSeaDepths.DataTexture);
            }
            else
            {
                // TextureArrayHelpers prevents use from using this in a static constructor due to blackTexture usage
                if (s_nullTexture == null)
                {
                    InitNullTexture();
                }

                properties.SetTexture(ParamIdSampler(), s_nullTexture);
            }
        }

        public static void BindNullToGraphicsShaders()
        {
            // TextureArrayHelpers prevents us from using this in a static constructor due to blackTexture usage.
            if (s_nullTexture == null)
            {
                InitNullTexture();
            }

            Shader.SetGlobalTexture(ParamIdSampler(), s_nullTexture);
        }

        static void InitNullTexture()
        {
            // Depth textures use HDR values
            var texture = TextureArrayHelpers.CreateTexture2D(s_nullColor, UnityEngine.TextureFormat.RGB9e5Float);
            s_nullTexture = TextureArrayHelpers.CreateTexture2DArray(texture);
            s_nullTexture.name = "Sea Floor Depth Null Texture";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        }
    }
}
