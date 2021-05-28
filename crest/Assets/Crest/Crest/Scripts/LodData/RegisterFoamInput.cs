// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the foam simulation. Attach this GameObjects that you want to influence the foam simulation, such as depositing foam on the surface.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(MENU_PREFIX + "Foam Input")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "ocean-simulation.html" + Internal.Constants.HELP_URL_RP + "#foam")]
    public class RegisterFoamInput : RegisterLodDataInputWithSplineSupport<LodDataMgrFoam, SplinePointDataFoam>
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public override bool Enabled => true;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(1f, 1f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Foam";

        protected override bool FollowHorizontalMotion => _followHorizontalMotion;

        protected override string SplineShaderName => "Hidden/Crest/Inputs/Foam/Spline Geometry";
        protected override Vector2 DefaultCustomData => Vector2.right;

        [SerializeField, Tooltip(k_displacementCorrectionTooltip)]
        bool _followHorizontalMotion = false;

#if UNITY_EDITOR
        protected override string FeatureToggleName => "_createFoamSim";
        protected override string FeatureToggleLabel => "Create Foam Sim";
        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateFoamSim;

        protected override string RequiredShaderKeywordProperty => LodDataMgrFoam.MATERIAL_KEYWORD_PROPERTY;
        protected override string RequiredShaderKeyword => LodDataMgrFoam.MATERIAL_KEYWORD;

        protected override string MaterialFeatureDisabledError => LodDataMgrFoam.ERROR_MATERIAL_KEYWORD_MISSING;
        protected override string MaterialFeatureDisabledFix => LodDataMgrFoam.ERROR_MATERIAL_KEYWORD_MISSING_FIX;
#endif // UNITY_EDITOR
    }
}
