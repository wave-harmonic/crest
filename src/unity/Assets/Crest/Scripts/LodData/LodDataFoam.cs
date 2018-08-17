// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD.
    /// </summary>
    public class LodDataFoam : LodDataPersistent
    {
        public override string SimName { get { return "Foam"; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/Foam"; } }
        protected override string ShaderRenderResultsIntoDispTexture { get { return "Ocean/Shape/Sim/Foam Add To Disps"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }
        public static readonly int SIM_RENDER_DEPTH = -20;
        public override int Depth { get { return SIM_RENDER_DEPTH; } }
        protected override Camera[] SimCameras { get { return OceanRenderer.Instance.Builder._foamCameras; } }

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
        }

        SimSettingsFoam Settings { get { return _settings as SimSettingsFoam; } }
    }
}
