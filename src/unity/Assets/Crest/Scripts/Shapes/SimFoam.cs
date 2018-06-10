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
        protected override string ShaderTextureLastSimResult { get { return "_FoamLastFrame"; } }
        protected override string ShaderRenderResultsIntoDispTexture { get { return "Ocean/Shape/Sim/Foam Add To Disps"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RHalf; } }
    }
}
