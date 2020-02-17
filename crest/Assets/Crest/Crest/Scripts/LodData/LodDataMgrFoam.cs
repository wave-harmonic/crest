// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataMgrFoam : LodDataMgrPersistent
    {
        protected override string ShaderSim { get { return "UpdateFoam"; } }
        protected override int krnl_ShaderSim { get { return _shader.FindKernel(ShaderSim); }}
        public override string SimName { get { return "Foam"; } }
        public override RenderTextureFormat TextureFormat { get { return Settings._renderTextureFormat; } }

        SimSettingsFoam Settings { get { return OceanRenderer.Instance._simSettingsFoam; } }
        public override void UseSettings(SimSettingsBase settings) { OceanRenderer.Instance._simSettingsFoam = settings as SimSettingsFoam; }
        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFoam>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        readonly int sp_FoamFadeRate = Shader.PropertyToID("_FoamFadeRate");
        readonly int sp_WaveFoamStrength = Shader.PropertyToID("_WaveFoamStrength");
        readonly int sp_WaveFoamCoverage = Shader.PropertyToID("_WaveFoamCoverage");
        readonly int sp_ShorelineFoamMaxDepth = Shader.PropertyToID("_ShorelineFoamMaxDepth");
        readonly int sp_ShorelineFoamStrength = Shader.PropertyToID("_ShorelineFoamStrength");


        protected override void Start()
        {
            base.Start();

#if UNITY_EDITOR
            if (!OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_FOAM_ON"))
            {
                Debug.LogWarning("Foam is not enabled on the current ocean material and will not be visible.", this);
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

            // assign animated waves - to slot 1 current frame data
            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(simMaterial);

            // assign sea floor depth - to slot 1 current frame data
            if (OceanRenderer.Instance._lodDataSeaDepths)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(simMaterial);
            }
            else
            {
                LodDataMgrSeaFloorDepth.BindNull(simMaterial);
            }

            // assign flow - to slot 1 current frame data
            if (OceanRenderer.Instance._lodDataFlow)
            {
                OceanRenderer.Instance._lodDataFlow.BindResultData(simMaterial);
            }
            else
            {
                LodDataMgrFlow.BindNull(simMaterial);
            }
        }

        public override void GetSimSubstepData(float frameDt, out int numSubsteps, out float substepDt)
        {
            // foam always does just one sim step
            substepDt = frameDt;
            numSubsteps = 1;
        }

        readonly static string s_textureArrayName = "_LD_TexArray_Foam";
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
