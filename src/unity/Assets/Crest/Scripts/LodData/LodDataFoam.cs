// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataFoam : LodDataPersistent
    {
        public override SimType LodDataType { get { return SimType.Foam; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/Foam"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }
        protected override Camera[] SimCameras { get { return OceanRenderer.Instance.Builder._camsFoam; } }

        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFoam>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        protected override void SetAdditionalSimParams(Material simMaterial)
        {
            base.SetAdditionalSimParams(simMaterial);

            simMaterial.SetFloat("_FoamFadeRate", Settings._foamFadeRate);
            simMaterial.SetFloat("_WaveFoamStrength", Settings._waveFoamStrength);
            simMaterial.SetFloat("_WaveFoamCoverage", Settings._waveFoamCoverage);
            simMaterial.SetFloat("_ShorelineFoamMaxDepth", Settings._shorelineFoamMaxDepth);
            simMaterial.SetFloat("_ShorelineFoamStrength", Settings._shorelineFoamStrength);

            // assign animated waves - to slot 1 current frame data
            OceanRenderer.Instance.Builder._lodDataAnimWaves[LodTransform.LodIndex].BindResultData(1, simMaterial);
            // assign sea floor depth - to slot 1 current frame data
            OceanRenderer.Instance.Builder._lodDataAnimWaves[LodTransform.LodIndex].LDSeaDepth.BindResultData(1, simMaterial);
        }

        SimSettingsFoam Settings { get { return _settings as SimSettingsFoam; } }
    }
}
