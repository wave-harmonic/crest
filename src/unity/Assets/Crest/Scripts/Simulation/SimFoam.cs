// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD.
    /// </summary>
    public class SimFoam : SimBase
    {
        public override string SimName { get { return "Foam"; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/Foam"; } }

        // TODO: Delete
        protected override string ShaderRenderResultsIntoDispTexture { get { return "Ocean/Shape/Sim/Foam Add To Disps"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }
        public static readonly int SIM_RENDER_DEPTH = -20;
        public override int Depth { get { return SIM_RENDER_DEPTH; } }
        public override SimSettingsBase CreateDefaultSettings()
        {
            return ScriptableObject.CreateInstance<SimSettingsFoam>();
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
        protected override RenderTexture GetTargetTexture(Camera cam, WaveDataCam wdc) {
            return wdc._rtFoam;
        }
        protected override void DetachFromCamera(Camera cam, WaveDataCam wdc) {
            Graphics.Blit(Texture2D.blackTexture, wdc._rtFoam, _matClearSim);
        }

        SimSettingsFoam Settings { get { return _settings as SimSettingsFoam; } }
    }
}
