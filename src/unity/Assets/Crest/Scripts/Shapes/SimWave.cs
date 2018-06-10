// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

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
    }
}
