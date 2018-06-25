// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A dynamic shape simulation that moves around with a displacement LOD.
    /// </summary>
    public class SimWave : SimBase
    {
        public override string SimName { get { return "Wave"; } }
        protected override string ShaderSim { get { return "Ocean/Shape/Sim/2D Wave Equation"; } }
        protected override string ShaderTextureLastSimResult { get { return "_WavePPTSource"; } }
        protected override string ShaderRenderResultsIntoDispTexture { get { return "Ocean/Shape/Sim/Wave Add To Disps"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.ARGBHalf; } }
        public override int Depth { get { return -21; } }

        [Range(0f, 0.1f)]
        public float _minAccel = 0f;
        [Range(0f, 0.1f)]
        public float _maxAccel = 0.05f;

        SimFoam _foamSim;
        Material _generateFoamFromSim;

        protected override void AddPostRenderCommands(CommandBuffer postRenderCmdBuf)
        {
            base.AddPostRenderCommands(postRenderCmdBuf);

            if (_foamSim == null)
            {
                _foamSim = FindObjectOfType<SimFoam>();
            }

            if (_generateFoamFromSim == null)
            {
                _generateFoamFromSim = new Material(Shader.Find("Ocean/Shape/Sim/Wave Generate Foam"));
            }

            _generateFoamFromSim.SetFloat("_MinAccel", _minAccel);
            _generateFoamFromSim.SetFloat("_MaxAccel", _maxAccel);

            _generateFoamFromSim.mainTexture = PPRTs.Target;

            postRenderCmdBuf.Blit(PPRTs.Target, _foamSim.GetComponent<Camera>().targetTexture, _generateFoamFromSim);
        }
    }
}
