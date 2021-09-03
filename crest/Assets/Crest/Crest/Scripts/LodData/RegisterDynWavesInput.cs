// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the dynamic wave simulation. Attach this GameObjects that you want to influence the sim to add ripples etc.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(MENU_PREFIX + "Dynamic Waves Input")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "ocean-simulation.html" + Internal.Constants.HELP_URL_RP + "#dynamic-waves")]
    public class RegisterDynWavesInput : RegisterLodDataInput<LodDataMgrDynWaves>
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        public override float Wavelength => 0f;

        public override bool Enabled => true;

        protected override Color GizmoColor => new Color(0f, 1f, 0f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Dynamic Waves";

#if UNITY_EDITOR
        protected override string FeatureToggleName => LodDataMgrDynWaves.FEATURE_TOGGLE_NAME;
        protected override string FeatureToggleLabel => LodDataMgrDynWaves.FEATURE_TOGGLE_LABEL;
        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateDynamicWaveSim;
#endif
    }
}
