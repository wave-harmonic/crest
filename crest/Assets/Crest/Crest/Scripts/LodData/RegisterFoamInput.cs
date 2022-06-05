// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the foam simulation. Attach this GameObjects that you want to influence the foam simulation, such as depositing foam on the surface.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(MENU_PREFIX + "Foam Input")]
    [CrestHelpURL("user/ocean-simulation", "foam")]
    [FilterEnum("_mode", FilteredAttribute.Mode.Exclude, (int)Mode.Primitive)]
    public class RegisterFoamInput : RegisterLodDataInputWithSplineSupport<LodDataMgrFoam, SplinePointDataFoam>, IPaintable
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

        #region Painting
        [Header("Paint Mode Settings")]
        [Predicated("_mode", inverted: true, Mode.Painted), DecoratedField]
        public CPUTexture2DPaintable_R16_AddBlend _paintData;
        public IPaintedData PaintedData => _paintData;
        public Shader PaintedInputShader => Shader.Find("Hidden/Crest/Inputs/Foam/Painted Foam");

        protected override void PreparePaintInputMaterial(Material mat)
        {
            base.PreparePaintInputMaterial(mat);
            if (_paintData == null) return;

            _paintData.CenterPosition3 = transform.position;
            _paintData.PrepareMaterial(mat, CPUTexture2DHelpers.ColorConstructFnOneChannel);
        }

        protected override void UpdatePaintInputMaterial(Material mat)
        {
            base.UpdatePaintInputMaterial(mat);
            if (_paintData == null) return;

            _paintData.CenterPosition3 = transform.position;
            _paintData.UpdateMaterial(mat, CPUTexture2DHelpers.ColorConstructFnOneChannel);
        }

        public void ClearData() => _paintData.Clear(this, 0f);
        public void MakeDirty() => _paintData.MakeDirty();

        public bool Paint(Vector3 paintPosition3, Vector2 paintDir, float paintWeight, bool remove)
        {
            _paintData.CenterPosition3 = transform.position;

            return _paintData.PaintSmoothstep(this, paintPosition3, paintWeight, 0.03f, _paintData.BrushRadius, _paintData._brushStrength, CPUTexturePaintHelpers.PaintFnAdditiveBlendSaturateFloat, remove);
        }
        #endregion

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

#if UNITY_EDITOR
    // Ensure preview works (preview does not apply to derived classes so done per type)
    [CustomPreview(typeof(RegisterFoamInput))]
    public class RegisterFoamInputPreview : UserPaintedDataPreview
    {
    }
#endif // UNITY_EDITOR
}
