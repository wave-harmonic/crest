// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the foam simulation. Attach this GameObjects that you want to influence the foam simulation, such as depositing foam on the surface.
    /// </summary>
    [ExecuteAlways]
    public class RegisterFoamInput : RegisterLodDataInputDisplacementCorrection<LodDataMgrFoam>
    {
        public override bool Enabled => true;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(1f, 1f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Foam";

        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateFoamSim;
        protected override string FeatureDisabledErrorMessage => "<i>Create Foam Sim</i> must be enabled on the OceanRenderer component.";

        protected override string RequiredShaderKeyword => "_FOAM_ON";
        protected override string KeywordMissingErrorMessage => "Foam must be enabled on the ocean material. Tick the <i>Enable</i> option in the <i>Foam</i> parameter section on the material currently assigned to the OceanRenderer component.";
    }
}
