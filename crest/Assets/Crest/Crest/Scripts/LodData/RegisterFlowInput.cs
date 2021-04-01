// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the flow data. Attach this GameObjects that you want to influence the horizontal flow of the water volume.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(MENU_PREFIX + "Flow Input")]
    public class RegisterFlowInput : RegisterLodDataInputWithSplineSupport<LodDataMgrFlow, SplinePointDataFlow>
    {
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
        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateFlowSim;
        protected override string FeatureDisabledErrorMessage => "<i>Create Flow Sim</i> must be enabled on the OceanRenderer component to enable flow on the water surface.";
        protected override void FixOceanFeatureDisabled(SerializedObject oceanComponent)
        {
            oceanComponent.FindProperty("_createFlowSim").boolValue = true;
        }

        protected override string RequiredShaderKeywordProperty => LodDataMgrFlow.MATERIAL_KEYWORD_PROPERTY;
        protected override string RequiredShaderKeyword => LodDataMgrFlow.MATERIAL_KEYWORD;
        protected override string KeywordMissingErrorMessage => LodDataMgrFlow.ERROR_MATERIAL_KEYWORD_MISSING;
#endif // UNITY_EDITOR
    }
}
