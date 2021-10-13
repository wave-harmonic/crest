// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    using SettingsType = SimSettingsFoam;

    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataMgrFoam : LodDataMgrPersistent
    {
        protected override string ShaderSim => "UpdateFoam";
        protected override int krnl_ShaderSim => _shader.FindKernel(ShaderSim);
        public override string SimName => "Foam";
        protected override GraphicsFormat RequestedTextureFormat => Settings._renderTextureGraphicsFormat;
        static Texture2DArray s_nullTexture => TextureArrayHelpers.BlackTextureArray;
        protected override Texture2DArray NullTexture => s_nullTexture;

        internal const string MATERIAL_KEYWORD_PROPERTY = "_Foam";
        internal const string MATERIAL_KEYWORD = MATERIAL_KEYWORD_PREFIX + "_FOAM_ON";
        internal const string ERROR_MATERIAL_KEYWORD_MISSING = "Foam is not enabled on the ocean material and will not be visible.";
        internal const string ERROR_MATERIAL_KEYWORD_MISSING_FIX = "Tick the <i>Enable</i> option in the <i>Foam</i> parameter section on the material currently assigned to the <i>OceanRenderer</i> component.";
        internal const string ERROR_MATERIAL_KEYWORD_ON_FEATURE_OFF = "The Foam feature is disabled on this component but is enabled on the ocean material.";
        internal const string ERROR_MATERIAL_KEYWORD_ON_FEATURE_OFF_FIX = "If this is not intentional, either enable the <i>Create Foam Sim</i> option on this component to turn it on, or disable the <i>Foam</i> feature on the ocean material to save performance.";

        readonly int sp_FoamFadeRate = Shader.PropertyToID("_FoamFadeRate");
        readonly int sp_WaveFoamStrength = Shader.PropertyToID("_WaveFoamStrength");
        readonly int sp_WaveFoamCoverage = Shader.PropertyToID("_WaveFoamCoverage");
        readonly int sp_ShorelineFoamMaxDepth = Shader.PropertyToID("_ShorelineFoamMaxDepth");
        readonly int sp_ShorelineFoamStrength = Shader.PropertyToID("_ShorelineFoamStrength");

        public override SimSettingsBase SettingsBase => Settings;
        public SettingsType Settings => _ocean._simSettingsFoam != null ? _ocean._simSettingsFoam : GetDefaultSettings<SettingsType>();

        public LodDataMgrFoam(OceanRenderer ocean) : base(ocean)
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

        protected override void SetAdditionalSimParams(IPropertyWrapper simMaterial)
        {
            base.SetAdditionalSimParams(simMaterial);

            simMaterial.SetFloat(sp_FoamFadeRate, Settings._foamFadeRate);
            simMaterial.SetFloat(sp_WaveFoamStrength, Settings._waveFoamStrength);
            simMaterial.SetFloat(sp_WaveFoamCoverage, Settings._waveFoamCoverage);
            simMaterial.SetFloat(sp_ShorelineFoamMaxDepth, Settings._shorelineFoamMaxDepth);
            simMaterial.SetFloat(sp_ShorelineFoamStrength, Settings._shorelineFoamStrength);
            simMaterial.SetVector(OceanRenderer.sp_oceanCenterPosWorld, OceanRenderer.Instance.Root.position);

            // assign animated waves - to slot 1 current frame data
            LodDataMgrAnimWaves.Bind(simMaterial);

            // assign sea floor depth - to slot 1 current frame data
            LodDataMgrSeaFloorDepth.Bind(simMaterial);

            // assign flow - to slot 1 current frame data
            LodDataMgrFlow.Bind(simMaterial);
        }

        protected override void GetSimSubstepData(float timeToSimulate, out int numSubsteps, out float substepDt)
        {
            numSubsteps = Mathf.FloorToInt(timeToSimulate * Settings._simulationFrequency);

            substepDt = numSubsteps > 0 ? (1f / Settings._simulationFrequency) : 0f;
        }

        readonly static string s_textureArrayName = "_LD_TexArray_Foam";
        private static TextureArrayParamIds s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) => s_textureArrayParamIds.GetId(sourceLod);
        protected override int GetParamIdSampler(bool sourceLod = false) => ParamIdSampler(sourceLod);

        public static void Bind(IPropertyWrapper properties)
        {
            if (OceanRenderer.Instance._lodDataFoam != null)
            {
                properties.SetTexture(OceanRenderer.Instance._lodDataFoam.GetParamIdSampler(), OceanRenderer.Instance._lodDataFoam.DataTexture);
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
