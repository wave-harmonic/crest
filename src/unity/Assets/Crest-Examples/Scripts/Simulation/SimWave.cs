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

        [Range(0f, 1f)]
        public float _damping = 0.173f;

        [Header("Foam Generation")]
        [Range(0f, 0.1f)]
        public float _foamMinAccel = 0f;
        [Range(0f, 0.1f)]
        public float _foamMaxAccel = 0.0134f;
        [Range(0f, 5f)]
        public float _foamAmount = 0.45f;

        public bool _rotateLaplacian = true;

        SimFoam _foamSim;
        Material _generateFoamFromSim;

        public override void AllSimsCreated()
        {
            base.AllSimsCreated();

            // look for a foam sim - take the first one that matches this resolution
            var sims = FindObjectsOfType<SimFoam>();
            foreach (var sim in sims)
            {
                var foamSim = sim as SimFoam;
                if (foamSim != null && foamSim._resolution == _resolution)
                {
                    _foamSim = foamSim;
                    break;
                }
            }
        }

        protected override void SetAdditionalSimParams(Material simMaterial)
        {
            base.SetAdditionalSimParams(simMaterial);

            simMaterial.SetFloat("_Damping", _damping);

            float laplacianKernelAngle = _rotateLaplacian ? Mathf.PI * 2f * Random.value : 0f;
            simMaterial.SetVector("_LaplacianAxisX", new Vector2(Mathf.Cos(laplacianKernelAngle), Mathf.Sin(laplacianKernelAngle)));
        }

        protected override void AddPostRenderCommands(CommandBuffer postRenderCmdBuf)
        {
            base.AddPostRenderCommands(postRenderCmdBuf);

            if (_foamSim == null)
            {
                return;
            }

            if (_generateFoamFromSim == null)
            {
                _generateFoamFromSim = new Material(Shader.Find("Ocean/Shape/Sim/Wave Generate Foam"));
            }

            _generateFoamFromSim.SetFloat("_MinAccel", _foamMinAccel);
            _generateFoamFromSim.SetFloat("_MaxAccel", _foamMaxAccel);
            _generateFoamFromSim.SetFloat("_Amount", _foamAmount);

            _generateFoamFromSim.mainTexture = PPRTs.Target;

            postRenderCmdBuf.Blit(PPRTs.Target, _foamSim.GetComponent<Camera>().targetTexture, _generateFoamFromSim);
        }
    }
}
