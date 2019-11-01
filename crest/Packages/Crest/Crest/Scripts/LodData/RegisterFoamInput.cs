// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the foam simulation. Attach this GameObjects that you want to influence the foam simulation, such as depositing foam on the surface.
    /// </summary>
    public class RegisterFoamInput : RegisterLodDataInput<LodDataMgrFoam>
    {
        public override float Wavelength => 0f;
    }
}
