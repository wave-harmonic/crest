// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;

// TODO !RunningWithoutGPU !RunningHeadless

namespace Crest
{
    [System.Serializable]
    public class FoamSimulation : Simulation<LodDataMgrFoam, SimSettingsFoam>
    {
        public override string Name => "Foam";

        protected override void AddData(OceanRenderer ocean)
        {
            _data = new LodDataMgrFoam(ocean, this);
        }
    }

    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataMgrFoam : LodDataMgrPersistent
    {
        protected override string ShaderSim => "UpdateFoam";
        protected override int krnl_ShaderSim => _shader.FindKernel(ShaderSim);
        public override string SimName => "Foam";
        protected override GraphicsFormat RequestedTextureFormat => _simulation.Settings._renderTextureGraphicsFormat;
        static Texture2DArray s_nullTexture => TextureArrayHelpers.BlackTextureArray;
        protected override Texture2DArray NullTexture => s_nullTexture;

        internal static readonly string MATERIAL_KEYWORD_PROPERTY = "_Foam";
        internal static readonly string MATERIAL_KEYWORD = MATERIAL_KEYWORD_PREFIX + "_FOAM_ON";
        internal const string ERROR_MATERIAL_KEYWORD_MISSING = "Foam is not enabled on the ocean material and will not be visible.";
        internal const string ERROR_MATERIAL_KEYWORD_MISSING_FIX = "Tick the <i>Enable</i> option in the <i>Foam</i> parameter section on the material currently assigned to the <i>OceanRenderer</i> component.";
        internal const string ERROR_MATERIAL_KEYWORD_ON_FEATURE_OFF = "The Foam feature is disabled on this component but is enabled on the ocean material.";
        internal const string ERROR_MATERIAL_KEYWORD_ON_FEATURE_OFF_FIX = "If this is not intentional, either enable the <i>Create Foam Sim</i> option on this component to turn it on, or disable the <i>Foam</i> feature on the ocean material to save performance.";

        readonly int sp_FoamFadeRate = Shader.PropertyToID("_FoamFadeRate");
        readonly int sp_WaveFoamStrength = Shader.PropertyToID("_WaveFoamStrength");
        readonly int sp_WaveFoamCoverage = Shader.PropertyToID("_WaveFoamCoverage");
        readonly int sp_ShorelineFoamMaxDepth = Shader.PropertyToID("_ShorelineFoamMaxDepth");
        readonly int sp_ShorelineFoamStrength = Shader.PropertyToID("_ShorelineFoamStrength");
        readonly int sp_NeedsPrewarming = Shader.PropertyToID("_NeedsPrewarming");

        readonly FoamSimulation _simulation;

        public LodDataMgrFoam(OceanRenderer ocean, FoamSimulation simulation) : base(ocean)
        {
            _simulation = simulation;
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

            // Prewarm simulation for first frame or teleporting. It will not be the same results as running the
            // simulation for multiple frames - but good enough.
            simMaterial.SetFloat(sp_NeedsPrewarming, _simulation.Settings._prewarm && _needsPrewarmingThisStep ? 1f : 0f);
            simMaterial.SetFloat(sp_FoamFadeRate, _simulation.Settings._foamFadeRate);
            simMaterial.SetFloat(sp_WaveFoamStrength, _simulation.Settings._waveFoamStrength);
            simMaterial.SetFloat(sp_WaveFoamCoverage, _simulation.Settings._waveFoamCoverage);
            simMaterial.SetFloat(sp_ShorelineFoamMaxDepth, _simulation.Settings._shorelineFoamMaxDepth);
            simMaterial.SetFloat(sp_ShorelineFoamStrength, _simulation.Settings._shorelineFoamStrength);
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
            numSubsteps = Mathf.FloorToInt(timeToSimulate * _simulation.Settings._simulationFrequency);

            substepDt = numSubsteps > 0 ? (1f / _simulation.Settings._simulationFrequency) : 0f;
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
