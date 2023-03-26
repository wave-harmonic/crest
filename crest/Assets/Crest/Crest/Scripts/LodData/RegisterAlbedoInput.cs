// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the albedo data. Attach this GameObjects that you want to influence the surface colour.
    /// </summary>
    [AddComponentMenu(MENU_PREFIX + "Albedo Input")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "water-appearance.html" + Internal.Constants.HELP_URL_RP + "#custom-albedo")]
    public class RegisterAlbedoInput : RegisterLodDataInput<LodDataMgrAlbedo>
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

        protected override Color GizmoColor => new Color(1f, 0f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Albedo";

        protected override bool FollowHorizontalMotion => false;

#if UNITY_EDITOR
        protected override string FeatureToggleName => "_createAlbedoData";
        protected override string FeatureToggleLabel => "Create Albedo Data";
        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateAlbedoData;

        protected override string RequiredShaderKeywordProperty => LodDataMgrAlbedo.MATERIAL_KEYWORD_PROPERTY;
        protected override string RequiredShaderKeyword => LodDataMgrAlbedo.MATERIAL_KEYWORD;

        protected override string MaterialFeatureDisabledError => LodDataMgrAlbedo.ERROR_MATERIAL_KEYWORD_MISSING;
        protected override string MaterialFeatureDisabledFix => LodDataMgrAlbedo.ERROR_MATERIAL_KEYWORD_MISSING_FIX;
#endif // UNITY_EDITOR
    }
}
