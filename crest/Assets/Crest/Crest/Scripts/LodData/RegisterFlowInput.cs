// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the flow data. Attach this GameObjects that you want to influence the horizontal flow of the water volume.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(MENU_PREFIX + "Flow Input")]
    [HelpURL("https://crest.readthedocs.io/en/stable/user/ocean-simulation.html#flow")]
    public class RegisterFlowInput : RegisterLodDataInputDisplacementCorrection<LodDataMgrFlow>
    {
        public override bool Enabled => true;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(0f, 0f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Flow";

#if UNITY_EDITOR
        protected override string FeatureToggleName => "_createFlowSim";
        protected override string FeatureToggleLabel => "Create Flow Sim";
        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateFlowSim;

        protected override string RequiredShaderKeywordProperty => LodDataMgrFlow.MATERIAL_KEYWORD_PROPERTY;
        protected override string RequiredShaderKeyword => LodDataMgrFlow.MATERIAL_KEYWORD;

        protected override string MaterialFeatureDisabledError => LodDataMgrFlow.ERROR_MATERIAL_KEYWORD_MISSING;
        protected override string MaterialFeatureDisabledFix => LodDataMgrFlow.ERROR_MATERIAL_KEYWORD_MISSING_FIX;
#endif // UNITY_EDITOR
    }
}
