// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to affect the water height.
    /// </summary>
    [AddComponentMenu(MENU_PREFIX + "Height Input")]
    public partial class RegisterHeightInput : RegisterLodDataInputWithSplineSupport<LodDataMgrSeaFloorDepth>, IReportsHeight
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

        // Debug
        [Space(10)]

        [SerializeField]
        DebugFields _debug = new DebugFields();

        [System.Serializable]
        class DebugFields
        {
            public bool _drawBounds;
        }


        Rect _rect;

        protected override void OnEnable()
        {
            base.OnEnable();

            OceanChunkRenderer.HeightReporters.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            OceanChunkRenderer.HeightReporters.Remove(this);
        }

        public bool ReportHeight(ref Rect bounds, ref float minimum, ref float maximum)
        {
            if (!Enabled)
            {
                return false;
            }

            if (_renderer != null)
            {
                _rect = new Rect(0, 0, _renderer.bounds.size.x, _renderer.bounds.size.z)
                {
                    center = new Vector2(_renderer.bounds.center.x, _renderer.bounds.center.z),
                };

                if (bounds.Overlaps(_rect, false))
                {
                    minimum = _renderer.bounds.min.y;
                    maximum = _renderer.bounds.max.y;
                    return true;
                }
            }
            else if (_splineMaterial != null && ShapeGerstnerSplineHandling.MinMaxHeightValid(_splinePointHeightMin, _splinePointHeightMax))
            {
                var splineBounds = GeometryUtility.CalculateBounds(_splineBoundingPoints, transform.localToWorldMatrix);
                _rect = Rect.MinMaxRect(splineBounds.min.x, splineBounds.min.z, splineBounds.max.x, splineBounds.max.z);

                if (bounds.Overlaps(_rect, false))
                {
                    minimum = _splinePointHeightMin;
                    maximum = _splinePointHeightMax;
                    return true;
                }
            }

            return false;
        }

        public void OnDrawGizmos()
        {
            if (_debug._drawBounds)
            {
                var ocean = OceanRenderer.Instance;
                if (ocean != null && _rect != null)
                {
                    Gizmos.DrawWireCube
                    (
                        new Vector3(_rect.center.x, ocean.SeaLevel, _rect.center.y),
                        new Vector3(_rect.size.x, 0, _rect.size.y)
                    );
                }
            }
        }

#if UNITY_EDITOR
        public override bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = base.Validate(ocean, showMessage);

            if (ocean != null && FeatureEnabled(ocean) && !ocean._lodDataSeaDepths.Settings._allowVaryingWaterLevel)
            {
                // NOTE: This is enabled by default so we will never trigger it for the default settings asset.
                showMessage
                (
                    $"<i>Allow Varing Water Level</i> must be enabled on the <i>Sim Settings Sea Floor Depth</i> asset.",
                    $"Enable the <i>Allow Varing Water Level</i> option on the <i>Sim Settings Sea Floor Depth</i> asset.",
                    ValidatedHelper.MessageType.Error, ocean._lodDataSeaDepths.Settings,
                    (so) => so.FindProperty(nameof(SimSettingsSeaFloorDepth._allowVaryingWaterLevel)).boolValue = true
                );

                isValid = false;
            }

            return isValid;
        }

        protected override string FeatureToggleName => LodDataMgrSeaFloorDepth.FEATURE_TOGGLE_NAME;
        protected override string FeatureToggleLabel => LodDataMgrSeaFloorDepth.FEATURE_TOGGLE_LABEL;
        protected override bool FeatureEnabled(OceanRenderer ocean) => ocean.CreateSeaFloorDepthData;
#endif // UNITY_EDITOR
    }
}
