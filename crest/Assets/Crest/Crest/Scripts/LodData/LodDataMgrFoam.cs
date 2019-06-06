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
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }

        SimSettingsFoam Settings { get { return OceanRenderer.Instance._simSettingsFoam; } }
        public override void UseSettings(SimSettingsBase settings) { OceanRenderer.Instance._simSettingsFoam = settings as SimSettingsFoam; }
        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFoam>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        static int sp_FoamFadeRate = Shader.PropertyToID("_FoamFadeRate");
        static int sp_WaveFoamStrength = Shader.PropertyToID("_WaveFoamStrength");
        static int sp_WaveFoamCoverage = Shader.PropertyToID("_WaveFoamCoverage");
        static int sp_ShorelineFoamMaxDepth = Shader.PropertyToID("_ShorelineFoamMaxDepth");
        static int sp_ShorelineFoamStrength = Shader.PropertyToID("_ShorelineFoamStrength");


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

        protected override void SetAdditionalSimParams(int lodIdx, IPropertyWrapper simMaterial)
        {
            base.SetAdditionalSimParams(lodIdx, simMaterial);

            simMaterial.SetFloat(sp_FoamFadeRate, Settings._foamFadeRate);
            simMaterial.SetFloat(sp_WaveFoamStrength, Settings._waveFoamStrength);
            simMaterial.SetFloat(sp_WaveFoamCoverage, Settings._waveFoamCoverage);
            simMaterial.SetFloat(sp_ShorelineFoamMaxDepth, Settings._shorelineFoamMaxDepth);
            simMaterial.SetFloat(sp_ShorelineFoamStrength, Settings._shorelineFoamStrength);
            simMaterial.SetFloat(OceanRenderer.sp_LD_SLICE_Index_ThisLod, lodIdx);

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

        // TODO(Factor these out to be shared with other classes who have same code
        public static string TextureArrayName = "_LD_TexArray_Foam_";
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
            properties.SetTexture(ParamIdSampler(prevFrame), TextureArray.Black);
        }
    }
}
