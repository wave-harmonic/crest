// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input for shadow data. Attach this to GameObjects that you want use to override shadows.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(MENU_PREFIX + "Shadow Input")]
    [HelpURL("https://crest.readthedocs.io/en/stable/user/ocean-simulation.html#shadows")]
    public class RegisterShadowInput : RegisterLodDataInput<LodDataMgrShadow>
    {
        public override bool Enabled => true;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(0f, 0f, 0f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Shadows";

        protected override bool FollowHorizontalMotion => false;

#if UNITY_EDITOR
        protected override string FeatureToggleName => "_createShadowData";
        protected override string FeatureToggleLabel => "Create Shadow Data";
        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateShadowData;

        protected override string RequiredShaderKeyword => LodDataMgrShadow.MATERIAL_KEYWORD;

        protected override string MaterialFeatureDisabledError => LodDataMgrShadow.ERROR_MATERIAL_KEYWORD_MISSING;
        protected override string MaterialFeatureDisabledFix => LodDataMgrShadow.ERROR_MATERIAL_KEYWORD_MISSING_FIX;
#endif // UNITY_EDITOR
    }
}
