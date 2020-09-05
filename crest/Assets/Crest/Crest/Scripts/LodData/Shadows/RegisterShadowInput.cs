// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input for shadow data. Attach this to GameObjects that you want use to override shadows.
    /// </summary>
    [ExecuteAlways]
    public class RegisterShadowInput : RegisterLodDataInput<LodDataMgrShadow>
    {
        public override bool Enabled => true;

        public override float Wavelength => 0f;

        protected override Color GizmoColor => new Color(0f, 0f, 0f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Shadows";

        protected override bool FollowHorizontalMotion => false;
    }
}
