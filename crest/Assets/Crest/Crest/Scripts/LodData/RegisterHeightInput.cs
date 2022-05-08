// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to affect the water height.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(MENU_PREFIX + "Height Input")]
    public partial class RegisterHeightInput : RegisterLodDataInputWithSplineSupport<LodDataMgrSeaFloorDepth>, IPaintable
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

        public readonly static Color s_gizmoColor = new Color(0f, 1f, 0f, 0.5f);
        protected override Color GizmoColor => s_gizmoColor;

        protected override string ShaderPrefix => "Crest/Inputs/Sea Floor Depth";

        protected override string SplineShaderName => "Crest/Inputs/Sea Floor Depth/Set Base Water Height Using Geometry";
        protected override Vector2 DefaultCustomData => Vector2.zero;

        protected override bool FollowHorizontalMotion => true;

        [Header("Height Input Settings")]
        [SerializeField, Tooltip("Inform ocean how much this input will displace the ocean surface vertically. This is used to set bounding box heights for the ocean tiles.")]
        float _maxDisplacementVertical = 0f;

        #region Painting
        [Header("Paint Settings")]
        public CPUTexture2DPaintable_R16_AddBlend _paintData;
        public IPaintedData PaintedData => _paintData;
        public Shader PaintedInputShader => Shader.Find("Hidden/Crest/Inputs/Animated Waves/Painted Height");

        protected override void PreparePaintInputMaterial(Material mat)
        {
            base.PreparePaintInputMaterial(mat);

            _paintData.CenterPosition3 = transform.position;
            _paintData.PrepareMaterial(mat, CPUTexture2DHelpers.ColorConstructFnOneChannel);
        }

        protected override void UpdatePaintInputMaterial(Material mat)
        {
            base.UpdatePaintInputMaterial(mat);

            _paintData.CenterPosition3 = transform.position;
            _paintData.UpdateMaterial(mat, CPUTexture2DHelpers.ColorConstructFnOneChannel);
        }

        public void ClearData()
        {
            _paintData.Clear(this, 0f);
        }

        public bool Paint(Vector3 paintPosition3, Vector2 paintDir, float paintWeight, bool remove)
        {
            _paintData.CenterPosition3 = transform.position;

            return _paintData.PaintSmoothstep(this, paintPosition3, paintWeight, remove ? 0.01f : 0.02f, _paintData.BrushRadius, _paintData._brushStrength, CPUTexturePaintHelpers.PaintFnAdditiveBlendFloat, remove);
        }
        #endregion

        protected override bool GetQueue(out int queue)
        {
            // TODO another symptom of murky input situation. What should happen here? Goal - make this apply strictly after
            // spline height, because spline height stomps (necessarily i think - needs to write its height not add it).
            if (!GetComponent<Spline.Spline>())
            {
                queue = -1000;
                return true;
            }
            return base.GetQueue(out queue);
        }

        protected override void Update()
        {
            base.Update();

            if (OceanRenderer.Instance == null)
            {
                return;
            }

            var maxDispVert = _maxDisplacementVertical;

            // let ocean system know how far from the sea level this shape may displace the surface
            if (_renderer != null)
            {
                var minY = _renderer.bounds.min.y;
                var maxY = _renderer.bounds.max.y;
                var seaLevel = OceanRenderer.Instance.SeaLevel;
                maxDispVert = Mathf.Max(maxDispVert, Mathf.Abs(seaLevel - minY), Mathf.Abs(seaLevel - maxY));
            }
            else if (_splineMaterial != null &&
                ShapeGerstnerSplineHandling.MinMaxHeightValid(_splinePointHeightMin, _splinePointHeightMax))
            {
                var seaLevel = OceanRenderer.Instance.SeaLevel;
                maxDispVert = Mathf.Max(maxDispVert,
                    Mathf.Abs(seaLevel - _splinePointHeightMin), Mathf.Abs(seaLevel - _splinePointHeightMax));
            }

            if (maxDispVert > 0f)
            {
                OceanRenderer.Instance.ReportMaxDisplacementFromShape(0f, maxDispVert, 0f);
            }
        }

#if UNITY_EDITOR
        // Animated waves are always enabled
        protected override bool FeatureEnabled(OceanRenderer ocean) => true;
#endif // UNITY_EDITOR
    }

#if UNITY_EDITOR
    // Ensure preview works (preview does not apply to derived classes so done per type)
    [CustomPreview(typeof(RegisterHeightInput))]
    public class RegisterHeightInputPreview : UserPaintedDataPreview
    {
    }
#endif // UNITY_EDITOR
}
