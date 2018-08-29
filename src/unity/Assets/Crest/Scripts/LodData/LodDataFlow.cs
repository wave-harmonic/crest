// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// A persistent flow simulation that moves around with a displacement LOD. The input is fully combined water surface shape.
    /// </summary>
    public class LodDataFlow : LodDataPersistent
    {
        public override SimType LodDataType { get { return SimType.Flow; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/Flow"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RGHalf; } }
        protected override Camera[] SimCameras { get { return OceanRenderer.Instance.Builder._camsFlow; } }

        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFlow>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        protected override void SetAdditionalSimParams(Material simMaterial)
        {
            base.SetAdditionalSimParams(simMaterial);

            simMaterial.SetFloat("_FlowSpeed", Settings._flowSpeed);

            // assign animated waves - to slot 1 current frame data
            OceanRenderer.Instance.Builder._lodDataAnimWaves[LodTransform.LodIndex].BindResultData(1, simMaterial);
            // assign sea floor depth - to slot 1 current frame data
            OceanRenderer.Instance.Builder._lodDataAnimWaves[LodTransform.LodIndex].LDSeaDepth.BindResultData(1, simMaterial);
        }

        SimSettingsFlow Settings { get { return _settings as SimSettingsFlow; } }
    }
}
