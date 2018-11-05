// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataMgrFoam : LodDataMgrPersistent
    {
        public override LodData.SimType LodDataType { get { return LodData.SimType.Foam; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/Foam"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }

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

        protected override void SetAdditionalSimParams(int lodIdx, Material simMaterial)
        {
            base.SetAdditionalSimParams(lodIdx, simMaterial);

            simMaterial.SetFloat("_FoamFadeRate", Settings._foamFadeRate);
            simMaterial.SetFloat("_WaveFoamStrength", Settings._waveFoamStrength);
            simMaterial.SetFloat("_WaveFoamCoverage", Settings._waveFoamCoverage);
            simMaterial.SetFloat("_ShorelineFoamMaxDepth", Settings._shorelineFoamMaxDepth);
            simMaterial.SetFloat("_ShorelineFoamStrength", Settings._shorelineFoamStrength);

            // assign animated waves - to slot 1 current frame data
            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(lodIdx, 1, simMaterial);

            // assign sea floor depth - to slot 1 current frame data
            if (OceanRenderer.Instance._createSeaFloorDepthData)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(lodIdx, 1, simMaterial);
            }

            // assign flow - to slot 1 current frame data
            if (OceanRenderer.Instance._createFlowSim)
            {
                OceanRenderer.Instance._lodDataFlow.BindResultData(lodIdx, 1, simMaterial);
            }
        }

        SimSettingsFoam Settings { get { return _settings as SimSettingsFoam; } }
    }
}
