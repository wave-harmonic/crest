// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the flow data. Attach this GameObjects that you want to influence the horizontal flow of the water volume.
    /// </summary>
    [ExecuteAlways]
    public class RegisterFlowInput : RegisterLodDataInputDisplacementCorrection<LodDataMgrFlow>
    {
        public override bool Enabled => true;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(0f, 0f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Flow";

        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateFlowSim;
        protected override string FeatureDisabledErrorMessage => "<i>Create Clip Surface Data</i> must be enabled on the OceanRenderer component to enable clipping holes in the water surface.";

        protected override string RequiredShaderKeyword => "_FLOW_ON";
        protected override string KeywordMissingErrorMessage => "Flow must be enabled on the ocean material. Tick the <i>Enable</i> option in the <i>Flow</i> parameter section on the material currently assigned to the OceanRenderer component.";
    }
}
