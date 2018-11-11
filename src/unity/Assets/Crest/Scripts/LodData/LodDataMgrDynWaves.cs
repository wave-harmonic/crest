// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A dynamic shape simulation that moves around with a displacement LOD. It
    /// </summary>
    public class LodDataMgrDynWaves : LodDataMgrPersistent
    {
        public override SimType LodDataType { get { return SimType.DynamicWaves; } }
        protected override string ShaderSim { get { return "Ocean/Simulation/Update Dynamic Waves"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }

        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsWave>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        public bool _rotateLaplacian = true;

        public Material[] _copySimMaterial;

        bool[] _active;
        public bool SimActive(int lodIdx) { return _active[lodIdx]; }

        protected override void Start()
        {
            base.Start();

            _copySimMaterial = new Material[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _copySimMaterial.Length; i++)
            {
                _copySimMaterial[i] = new Material(Shader.Find("Ocean/Simulation/Add Dyn Waves To Anim Waves"));
            }
        }

        protected override void InitData()
        {
            base.InitData();

            _active = new bool[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _active.Length; i++) _active[i] = true;
        }

        protected override bool BuildCommandBufferInternal(int lodIdx)
        {
            if (!base.BuildCommandBufferInternal(lodIdx))
                return false;

            // this sim copies its results into the animated waves

            // check if the sim should be running
            float texelWidth = OceanRenderer.Instance._lods[lodIdx]._renderData.Validate(0, this)._texelWidth;
            _active[lodIdx] = texelWidth >= Settings._minGridSize && (texelWidth <= Settings._maxGridSize || Settings._maxGridSize == 0f);

            // only run simulation if enabled
            if (!_active[lodIdx])
                return false;

            _copySimMaterial[lodIdx].SetFloat("_HorizDisplace", Settings._horizDisplace);
            _copySimMaterial[lodIdx].SetFloat("_DisplaceClamp", Settings._displaceClamp);
            _copySimMaterial[lodIdx].SetFloat("_TexelWidth", OceanRenderer.Instance._lods[lodIdx]._renderData._texelWidth);
            _copySimMaterial[lodIdx].mainTexture = _targets[lodIdx];

            return true;
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
            if (OceanRenderer.Instance._createSeaFloorDepthData)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(lodIdx, 1, simMaterial);
            }
            else
            {
                simMaterial.SetTexture("_LD_Sampler_SeaFloorDepth_1", Texture2D.blackTexture);
            }

            if (OceanRenderer.Instance._createFlowSim)
            {
                OceanRenderer.Instance._lodDataFlow.BindResultData(lodIdx, 1, simMaterial);
            }
            else
            {
                simMaterial.SetTexture("_LD_Sampler_Flow_1", Texture2D.blackTexture);
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

        SimSettingsWave Settings { get { return _settings as SimSettingsWave; } }
    }
}
