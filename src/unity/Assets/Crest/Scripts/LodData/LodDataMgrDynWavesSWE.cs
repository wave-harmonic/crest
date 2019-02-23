// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Dynamic wave sim based on shallow water equations.
    /// </summary>
    public class LodDataMgrDynWavesSWE : LodDataMgrPersistent
    {
        public override string SimName { get { return "DynamicWavesSWE"; } }
        protected override string ShaderSim { get { return "Hidden/Ocean/Simulation/Update Dynamic Waves SWE"; } }
        public override RenderTextureFormat[] TextureFormats { get { return new[] { RenderTextureFormat.RGHalf, RenderTextureFormat.ARGBHalf }; } }

        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsWave>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        public bool _rotateLaplacian = true;

        const string DYNWAVES_KEYWORD = "_DYNAMIC_WAVE_SIM_ON";

        bool[] _active;
        public bool SimActive(int lodIdx) { return _active[lodIdx]; }

        protected override void InitData()
        {
            base.InitData();

            _active = new bool[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _active.Length; i++) _active[i] = true;
        }

        private void OnEnable()
        {
            Shader.EnableKeyword(DYNWAVES_KEYWORD);
        }

        private void OnDisable()
        {
            Shader.DisableKeyword(DYNWAVES_KEYWORD);
        }

        protected override bool BuildCommandBufferInternal(int lodIdx)
        {
            if (!base.BuildCommandBufferInternal(lodIdx))
                return false;

            // check if the sim should be running
            float texelWidth = OceanRenderer.Instance._lods[lodIdx]._renderData.Validate(0, this)._texelWidth;
            _active[lodIdx] = texelWidth >= Settings._minGridSize && (texelWidth <= Settings._maxGridSize || Settings._maxGridSize == 0f);

            return true;
        }

        public void BindCopySettings(Material target)
        {
            target.SetFloat("_HorizDisplace", Settings._horizDisplace);
            target.SetFloat("_DisplaceClamp", Settings._displaceClamp);
        }

        protected override void SetAdditionalSimParams(int lodIdx, Material simMaterial)
        {
            base.SetAdditionalSimParams(lodIdx, simMaterial);

            simMaterial.SetFloat("_Damping", Settings._damping);
            simMaterial.SetFloat("_Gravity", OceanRenderer.Instance.Gravity);

            float laplacianKernelAngle = _rotateLaplacian ? Mathf.PI * 2f * Random.value : 0f;
            simMaterial.SetVector("_LaplacianAxisX", new Vector2(Mathf.Cos(laplacianKernelAngle), Mathf.Sin(laplacianKernelAngle)));

            // assign sea floor depth - to slot 1 current frame data. minor bug here - this depth will actually be from the previous frame,
            // because the depth is scheduled to render just before the animated waves, and this sim happens before animated waves.
            if (OceanRenderer.Instance._lodDataSeaDepths)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(lodIdx, 0, 1, simMaterial);
            }
            else
            {
                LodDataMgrSeaFloorDepth.BindNull(1, simMaterial);
            }

            if (OceanRenderer.Instance._lodDataFlow)
            {
                OceanRenderer.Instance._lodDataFlow.BindResultData(lodIdx, 0, 1, simMaterial);
            }
            else
            {
                LodDataMgrFlow.BindNull(1, simMaterial);
            }

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

        protected override int GetNumSubsteps(float dt)
        {
            return Mathf.Max(1,Mathf.Min(MAX_SIM_STEPS, Mathf.CeilToInt(dt / Settings._maxSubstepDt)));
        }

        public float SimDeltaTime
        {
            get
            {
                return Time.deltaTime / GetNumSubsteps(Time.deltaTime);
            }
        }

        static int[][] _paramsSamplers;
        public int ParamIdSampler(int dataIdx, int slot)
        {
            if (_paramsSamplers == null)
            {
                _paramsSamplers = new int[NumDataTextures][];
                for (var i = 0; i < NumDataTextures; i++)
                {
                    LodTransform.CreateParamIDs(ref _paramsSamplers[i], "_LD_Sampler_DynamicWavesSWE" + i + "_");
                }
            }
            return _paramsSamplers[dataIdx][slot];
        }
        protected override int GetParamIdSampler(int dataIdx, int slot)
        {
            Debug.Assert(dataIdx < 2);
            return ParamIdSampler(dataIdx, slot);
        }
        public void BindNull(int dataIdx, int shapeSlot, Material properties)
        {
            properties.SetTexture(ParamIdSampler(dataIdx, shapeSlot), Texture2D.blackTexture);
        }
        public void BindNull(int dataIdx, int shapeSlot, MaterialPropertyBlock properties)
        {
            properties.SetTexture(ParamIdSampler(dataIdx, shapeSlot), Texture2D.blackTexture);
        }

        SimSettingsWave Settings { get { return _settings as SimSettingsWave; } }
    }
}
