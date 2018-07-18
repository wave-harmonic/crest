// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD.
    /// </summary>
    public class SimFoam : SimBase
    {
        [Range(0f, 5f)]
        public float _foamFadeRate = 0.8f;
        [Range(0f, 5f)]
        public float _WaveFoamStrength = 1.25f;
        [Range(0f, 1f)]
        public float _WaveFoamCoverage = 0.8f;

        public override string SimName { get { return "Foam"; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/Foam"; } }
        protected override string ShaderRenderResultsIntoDispTexture { get { return "Ocean/Shape/Sim/Foam Add To Disps"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }
        public override int Depth { get { return -20; } }

        protected override void SetAdditionalSimParams(Material simMaterial)
        {
            base.SetAdditionalSimParams(simMaterial);

            simMaterial.SetFloat("_FoamFadeRate", _foamFadeRate);
            simMaterial.SetFloat("_WaveFoamStrength", _WaveFoamStrength);
            simMaterial.SetFloat("_WaveFoamCoverage", _WaveFoamCoverage);
        }
    }
}
