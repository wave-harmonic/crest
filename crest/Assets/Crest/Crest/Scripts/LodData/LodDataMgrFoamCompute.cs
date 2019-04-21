// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataMgrFoamCompute : LodDataMgrPersistentCompute
    {
        protected override string ShaderSim { get { return "UpdateFoamCompute"; } }
        public override string SimName { get { return "Foam"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }

        SimSettingsFoam Settings { get { return OceanRenderer.Instance._simSettingsFoam; } }
        public override void UseSettings(SimSettingsBase settings) { OceanRenderer.Instance._simSettingsFoam = settings as SimSettingsFoam; }
        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFoam>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

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

        protected override void SetAdditionalSimParams(int lodIdx, PropertyWrapperCompute simPropertyWrapperCompute)
        {
            base.SetAdditionalSimParams(lodIdx, simPropertyWrapperCompute);

            simPropertyWrapperCompute.SetFloat(Shader.PropertyToID("_FoamFadeRate"), Settings._foamFadeRate);
            simPropertyWrapperCompute.SetFloat(Shader.PropertyToID("_WaveFoamStrength"), Settings._waveFoamStrength);
            simPropertyWrapperCompute.SetFloat(Shader.PropertyToID("_WaveFoamCoverage"), Settings._waveFoamCoverage);
            simPropertyWrapperCompute.SetFloat(Shader.PropertyToID("_ShorelineFoamMaxDepth"), Settings._shorelineFoamMaxDepth);
            simPropertyWrapperCompute.SetFloat(Shader.PropertyToID("_ShorelineFoamStrength"), Settings._shorelineFoamStrength);

            // assign animated waves - to slot 1 current frame data
            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(lodIdx, 1, simPropertyWrapperCompute);

            // assign sea floor depth - to slot 1 current frame data
            if (OceanRenderer.Instance._lodDataSeaDepths)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(lodIdx, 1, simPropertyWrapperCompute);
            }
            else
            {
                LodDataMgrSeaFloorDepth.BindNull(1, simPropertyWrapperCompute);
            }

            // assign flow - to slot 1 current frame data
            if (OceanRenderer.Instance._lodDataFlow)
            {
                OceanRenderer.Instance._lodDataFlow.BindResultData(lodIdx, 1, simPropertyWrapperCompute);
            }
            else
            {
                LodDataMgrFlow.BindNull(1, simPropertyWrapperCompute);
            }
        }

        public override void GetSimSubstepData(float frameDt, out int numSubsteps, out float substepDt)
        {
            // foam always does just one sim step
            substepDt = frameDt;
            numSubsteps = 1;
        }

        static int[] _paramsSampler;
        public static int ParamIdSampler(int slot)
        {
            if (_paramsSampler == null)
                LodTransform.CreateParamIDs(ref _paramsSampler, "_LD_Sampler_Foam_");
            return _paramsSampler[slot];
        }
        protected override int GetParamIdSampler(int slot)
        {
            return ParamIdSampler(slot);
        }
        public static void BindNull(int shapeSlot, Material properties)
        {
            properties.SetTexture(ParamIdSampler(shapeSlot), Texture2D.blackTexture);
        }
        public static void BindNull(int shapeSlot, MaterialPropertyBlock properties)
        {
            properties.SetTexture(ParamIdSampler(shapeSlot), Texture2D.blackTexture);
        }
    }
}
