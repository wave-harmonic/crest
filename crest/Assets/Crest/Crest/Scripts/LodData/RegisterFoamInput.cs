// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the foam simulation. Attach this GameObjects that you want to influence the foam simulation, such as depositing foam on the surface.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(MENU_PREFIX + "Foam Input")]
    public class RegisterFoamInput : RegisterLodDataInputDisplacementCorrection<LodDataMgrFoam>
    {
        public override bool Enabled => true;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(1f, 1f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Foam";

#if UNITY_EDITOR
        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateFoamSim;
        protected override string FeatureDisabledErrorMessage => "<i>Create Foam Sim</i> must be enabled on the OceanRenderer component.";
        protected override string FixOceanFeatureDisabled(SerializedObject oceanComponent)
        {
            if (oceanComponent != null)
            {
                oceanComponent.FindProperty("_createFoamSim").boolValue = true;
            }
            return "Enable 'Create Foam Sim' option on OceanRenderer component.";
        }

        protected override string RequiredShaderKeywordProperty => LodDataMgrFoam.MATERIAL_KEYWORD_PROPERTY;
        protected override string RequiredShaderKeyword => LodDataMgrFoam.MATERIAL_KEYWORD;
        protected override string KeywordMissingErrorMessage => LodDataMgrFoam.ERROR_MATERIAL_KEYWORD_MISSING;
#endif // UNITY_EDITOR
    }
}
