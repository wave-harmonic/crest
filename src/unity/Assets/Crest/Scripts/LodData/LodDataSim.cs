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
        public override string SimName { get { return _sim.name; } }
        public override bool BindResultToOceanMaterial { get { return _sim._bindResultToOceanMaterial; } }

        public override SimSettingsBase CreateDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<SimSettingsFoam>();
            settings.name = SimName + " Auto-generated Settings";
            return settings;
        }

        protected override void SetAdditionalSimParams(Material simMaterial)
        {
            base.SetAdditionalSimParams(simMaterial);

            _sim.SetSimParams(simMaterial);

            foreach(var input in _sim._inputs)
            {
                if (!OceanRenderer.Instance.Builder._simLodDatas.ContainsKey(input.ToString()))
                {
                    Debug.LogError(_sim.name + " requires input " + input.ToString() + " which is not present, please add this sim to the OceanRenderer component.", this);
                    continue;
                }
                OceanRenderer.Instance.Builder._simLodDatas[input.ToString()][LodTransform.LodIndex].BindResultData(1, simMaterial);
            }
        }

        SimSettingsFoam Settings { get { return _settings as SimSettingsFoam; } }
    }
}
