// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Crest
{
    using SettingsType = SimSettingsWave;

    /// <summary>
    /// A dynamic shape simulation that moves around with a displacement LOD.
    /// </summary>
    public class LodDataMgrDynWaves : LodDataMgrPersistent
    {
        protected override string ShaderSim => "UpdateDynWaves";
        protected override int krnl_ShaderSim => _shader.FindKernel(ShaderSim);

        public override string SimName => "DynamicWaves";
        protected override GraphicsFormat RequestedTextureFormat => GraphicsFormat.R16G16_SFloat;
        static Texture2DArray s_nullTexture => TextureArrayHelpers.BlackTextureArray;
        protected override Texture2DArray NullTexture => s_nullTexture;

        public bool _rotateLaplacian = true;

        public const string FEATURE_TOGGLE_NAME = "_createDynamicWaveSim";
        public const string FEATURE_TOGGLE_LABEL = "Create Dynamic Wave Sim";
        public const string DYNWAVES_KEYWORD = "CREST_DYNAMIC_WAVE_SIM_ON_INTERNAL";

        bool[] _active;
        public bool SimActive(int lodIdx) => _active[lodIdx];

        readonly int sp_HorizDisplace = Shader.PropertyToID("_HorizDisplace");
        readonly int sp_DisplaceClamp = Shader.PropertyToID("_DisplaceClamp");
        readonly int sp_Damping = Shader.PropertyToID("_Damping");
        readonly int sp_Gravity = Shader.PropertyToID("_Gravity");
        readonly int sp_CourantNumber = Shader.PropertyToID("_CourantNumber");

        public override SimSettingsBase SettingsBase => Settings;
        public SettingsType Settings => _ocean._simSettingsDynamicWaves != null ? _ocean._simSettingsDynamicWaves : GetDefaultSettings<SettingsType>();

        public LodDataMgrDynWaves(OceanRenderer ocean) : base(ocean)
        {
            Start();
        }

        protected override void InitData()
        {
            base.InitData();

            _active = new bool[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _active.Length; i++) _active[i] = true;
        }

        internal override void OnEnable()
        {
            base.OnEnable();

            Shader.EnableKeyword(DYNWAVES_KEYWORD);
        }

        internal override void OnDisable()
        {
            base.OnDisable();

            Shader.DisableKeyword(DYNWAVES_KEYWORD);
        }

        protected override bool BuildCommandBufferInternal(int lodIdx)
        {
            if (!base.BuildCommandBufferInternal(lodIdx))
                return false;

            // check if the sim should be running
            float texelWidth = OceanRenderer.Instance._lodTransform._renderData[lodIdx].Current.Validate(0, SimName)._texelWidth;
            _active[lodIdx] = texelWidth >= Settings._minGridSize && (texelWidth <= Settings._maxGridSize || Settings._maxGridSize == 0f);

            return true;
        }

        public void BindCopySettings(IPropertyWrapper target)
        {
            target.SetFloat(sp_HorizDisplace, Settings._horizDisplace);
            target.SetFloat(sp_DisplaceClamp, Settings._displaceClamp);
        }

        protected override void SetAdditionalSimParams(IPropertyWrapper simMaterial)
        {
            base.SetAdditionalSimParams(simMaterial);

            simMaterial.SetFloat(sp_Damping, Settings._damping);
            simMaterial.SetFloat(sp_Gravity, OceanRenderer.Instance.Gravity * Settings._gravityMultiplier);
            simMaterial.SetFloat(sp_CourantNumber, Settings._courantNumber);
            simMaterial.SetVector(OceanRenderer.sp_oceanCenterPosWorld, OceanRenderer.Instance.Root.position);

            // assign sea floor depth - to slot 1 current frame data. minor bug here - this depth will actually be from the previous frame,
            // because the depth is scheduled to render just before the animated waves, and this sim happens before animated waves.
            LodDataMgrSeaFloorDepth.Bind(simMaterial);
            LodDataMgrFlow.Bind(simMaterial);
        }

        public static void CountWaveSims(int countFrom, out int o_present, out int o_active)
        {
            o_present = OceanRenderer.Instance.CurrentLodCount;
            o_active = 0;
            for (int i = 0; i < o_present; i++)
            {
                if (i < countFrom) continue;
                if (!OceanRenderer.Instance._lodDataDynWaves.SimActive(i)) continue;

                o_active++;
            }
        }

        protected override void GetSimSubstepData(float timeToSimulate, out int numSubsteps, out float substepDt)
        {
            numSubsteps = Mathf.FloorToInt(timeToSimulate * Settings._simulationFrequency);
            substepDt = numSubsteps > 0 ? (1f / Settings._simulationFrequency) : 0f;
        }

        readonly static string s_textureArrayName = "_LD_TexArray_DynamicWaves";
        private static TextureArrayParamIds s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) => s_textureArrayParamIds.GetId(sourceLod);
        protected override int GetParamIdSampler(bool sourceLod = false) => ParamIdSampler(sourceLod);

        public static void Bind(IPropertyWrapper properties)
        {
            if (OceanRenderer.Instance._lodDataDynWaves != null)
            {
                properties.SetTexture(OceanRenderer.Instance._lodDataDynWaves.GetParamIdSampler(), OceanRenderer.Instance._lodDataDynWaves.DataTexture);
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
