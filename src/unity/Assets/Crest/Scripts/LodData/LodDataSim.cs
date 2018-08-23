// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataSim : LodDataPersistent
    {
        public SimBase _sim;
        public override SimType LodDataType { get { return SimType.Foam; } }
        protected override string ShaderSim { get { return _sim._simulationShader.name; } }
        public override RenderTextureFormat TextureFormat { get { return _sim._dataFormat; } }
        public override int Depth { get { return _sim._cameraDepthOrder; } }
        protected override Camera[] SimCameras { get { return OceanRenderer.Instance.Builder._camArrays[_sim.GetType()].ToArray(); } }
        public override string SimName { get { return _sim.GetType().Name; } }

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
