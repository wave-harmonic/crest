// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the dynamic wave simulation. Attach this GameObjects that you want to influence the sim to add ripples etc.
    /// </summary>
    public class RegisterDynWavesInput : RegisterLodDataInput<LodDataMgrDynWaves>
    {
        public override float Wavelength => 0f;

        public override bool Enabled => true;

        protected override Color GizmoColor => new Color(0f, 1f, 0f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Dynamic Waves";
    }
}
