// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the flow data. Attach this GameObjects that you want to influence the horizontal flow of the water volume.
    /// </summary>
    public class RegisterFlowInput : RegisterLodDataInput<LodDataMgrFlow>
    {
        public override float Wavelength => 0f;
    }
}
