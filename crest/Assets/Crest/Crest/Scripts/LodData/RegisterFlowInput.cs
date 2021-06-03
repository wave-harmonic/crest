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
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "ocean-simulation.html" + Internal.Constants.HELP_URL_RP + "#flow")]
    public class RegisterFlowInput : RegisterLodDataInputWithSplineSupport<LodDataMgrFlow, SplinePointDataFlow>
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

        protected override Color GizmoColor => new Color(0f, 0f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Flow";

        protected override bool FollowHorizontalMotion => _followHorizontalMotion;

        protected override string SplineShaderName => "Hidden/Crest/Inputs/Flow/Spline Geometry";
        protected override Vector2 DefaultCustomData => new Vector2(SplinePointDataFlow.k_defaultSpeed, 0f);

        [Header("Other Settings")]

        [SerializeField, Tooltip(k_displacementCorrectionTooltip)]
        bool _followHorizontalMotion = false;

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
