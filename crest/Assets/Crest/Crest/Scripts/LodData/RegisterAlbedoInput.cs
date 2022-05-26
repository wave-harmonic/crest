// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the albedo data. Attach this GameObjects that you want to influence the surface colour.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(MENU_PREFIX + "Albedo Input")]
    [HelpURL(Internal.Constants.HELP_URL_BASE_USER + "ocean-simulation.html" + Internal.Constants.HELP_URL_RP + "#albedo")]
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
        // TODO:
        protected override ISimulation<LodDataMgr, SimSettingsBase> GetSimulation(OceanRenderer ocean) => null;
#endif // UNITY_EDITOR
    }
}
