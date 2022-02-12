// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to affect the water height.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu(MENU_PREFIX + "Height Input")]
    public partial class RegisterHeightInput : RegisterLodDataInputWithSplineSupport<LodDataMgrSeaFloorDepth>
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
}
