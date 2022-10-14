// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    /// <summary>
    /// This script and associated shader approximate the interaction between a sphere and the water. Multiple
    /// spheres can be used to model the interaction of a non-spherical shape.
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Sphere Water Interaction")]
    public partial class SphereWaterInteraction : CustomMonoBehaviour, ILodDataInput
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Range(0.01f, 50f), Tooltip("Radius of the sphere that is modelled.")]
        public float _radius = 1f;

        [Range(-40f, 40f), Tooltip("Intensity of the forces.")]
        public float _weight = 1f;
        [Range(0f, 2f), Tooltip("Intensity of the forces from vertical motion of the sphere.")]
        public float _weightUpDownMul = 0.5f;

        [Range(0f, 10f), Tooltip("Model parameter that can be used to modify the shape of the interaction.")]
        public float _innerSphereMultiplier = 1.55f;
        [Range(0f, 1f), Tooltip("Model parameter that can be used to modify the shape of the interaction.")]
        public float _innerSphereOffset = 0.109f;

        [Range(0f, 2f), Tooltip("Offset in direction of motion to help ripples appear in front of sphere.")]
        public float _velocityOffset = 0.04f;

        [Range(0f, 1f), Tooltip("Correct for wave displacement. Increasing this can fix issues where the dynamic wave input visibly drifts away from the boat in the presence of large waves. However in some cases enabling this option results in a feedback loop causing visible rings on the surface so a balance may need to be struck to minimize both issues.")]
        public float _compensateForWaveMotion = 0.45f;

        [Tooltip("If the dynamic waves are not visible far enough in the distance from the camera, this can be used to boost the output.")]
        public bool _boostLargeWaves = false;

        [Header("Limits")]
        [Tooltip("Teleport speed (km/h) - if the calculated speed is larger than this amount, the object is deemed to have teleported and the computed velocity is discarded."), SerializeField]
        float _teleportSpeed = 500f;
        [SerializeField]
        bool _warnOnTeleport = false;
        [Tooltip("Maximum speed clamp (km/h), useful for controlling/limiting wake."), SerializeField]
        float _maxSpeed = 100f;
        [SerializeField]
        bool _warnOnSpeedClamp = false;

#if UNITY_EDITOR
        [Header("Debug")]
        [Tooltip("Draws debug lines at each substep position. Editor only."), SerializeField]
        bool _debugSubsteps = false;
#endif

        Vector3 _velocity;
        Vector3 _velocityClamped;
        Vector3 _posLast;

        float _weightThisFrame;
        Matrix4x4 _renderMatrix;

        SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();
        SampleFlowHelper _sampleFlowHelper = new SampleFlowHelper();

        Material _mat;
        MaterialPropertyBlock _mpb;

        static int sp_velocity = Shader.PropertyToID("_Velocity");
        static int sp_weight = Shader.PropertyToID("_Weight");
        static int sp_simDeltaTime = Shader.PropertyToID("_SimDeltaTime");
        static int sp_radius = Shader.PropertyToID("_Radius");
        static int sp_innerSphereOffset = Shader.PropertyToID("_InnerSphereOffset");
        static int sp_innerSphereMultiplier = Shader.PropertyToID("_InnerSphereMultiplier");
        static int sp_largeWaveMultiplier = Shader.PropertyToID("_LargeWaveMultiplier");

        public float Wavelength => 2f * _radius;

        public bool Enabled => true;

        private void Start()
        {
            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }

#if UNITY_EDITOR
            if (EditorApplication.isPlaying && !Validate(OceanRenderer.Instance, ValidatedHelper.DebugLog))
            {
                enabled = false;
                return;
            }
#endif

            if (OceanRenderer.Instance._lodDataDynWaves == null)
            {
                // Don't run without a dyn wave sim
                enabled = false;
                return;
            }

            _mat = new Material(Shader.Find("Crest/Inputs/Dynamic Waves/Sphere-Water Interaction"));
            _mpb = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            var ocean = OceanRenderer.Instance;
            if (ocean == null) return;

            _sampleHeightHelper.Init(transform.position, 2f * _radius);
            _sampleHeightHelper.Sample(out Vector3 disp, out _, out _);

            LateUpdateComputeVel(ocean);

            // Velocity relative to water
            var relativeVelocity = _velocityClamped;
            {
                _sampleFlowHelper.Init(transform.position, 2f * _radius);
                _sampleFlowHelper.Sample(out var surfaceFlow);
                relativeVelocity -= new Vector3(surfaceFlow.x, 0, surfaceFlow.y);

                relativeVelocity.y *= _weightUpDownMul;
            }

            var dt = 1f / ocean._lodDataDynWaves.Settings._simulationFrequency;

            // Use weight from user with a multiplier to make interactions look plausible
            _weightThisFrame = 3.75f * _weight;

            var waterHeight = disp.y + ocean.SeaLevel;
            LateUpdateSphereWeight(waterHeight, ref _weightThisFrame);

            _mpb.SetVector(sp_velocity, relativeVelocity);
            _mpb.SetFloat(sp_simDeltaTime, dt);

            // Enlarge radius slightly - this tends to help waves 'wrap' the sphere slightly better
            float radiusScale = 1.1f;
            _mpb.SetFloat(sp_radius, _radius * radiusScale);

            _mpb.SetFloat(sp_innerSphereOffset, _innerSphereOffset);
            _mpb.SetFloat(sp_innerSphereMultiplier, _innerSphereMultiplier);
            _mpb.SetFloat(sp_largeWaveMultiplier, _boostLargeWaves ? 2f : 1f);
            _mpb.SetVector(RegisterLodDataInputBase.sp_DisplacementAtInputPosition, _compensateForWaveMotion * disp);

            // Weighting with this value helps keep ripples consistent for different gravity values
            var gravityMul = Mathf.Sqrt(ocean._lodDataDynWaves.Settings._gravityMultiplier) / 5f;
            _weightThisFrame *= gravityMul;

            // Matrix used for rendering this input
            {
                var position = transform.position;
                // Apply sea level to matrix so we can use it for rendering and gizmos.
                position.y = OceanRenderer.Instance.SeaLevel;
                var scale = Vector3.one * 2f * _radius;
                scale.z = 0f;
                _renderMatrix = Matrix4x4.TRS(position, Quaternion.Euler(90f, 0f, 0f), scale);
            }

            _posLast = transform.position;
        }

        // Velocity of the sphere, relative to the water. Computes on the fly, discards if teleport detected.
        void LateUpdateComputeVel(OceanRenderer ocean)
        {
            // Compue vel using finite difference
            _velocity = (transform.position - _posLast) / ocean.DeltaTimeDynamics;
            if (ocean.DeltaTimeDynamics < 0.0001f)
            {
                _velocity = Vector3.zero;
            }

            var speedKmh = _velocity.magnitude * 3.6f;
            if (speedKmh > _teleportSpeed)
            {
                // teleport detected
                _velocity *= 0f;

                if (_warnOnTeleport)
                {
                    Debug.LogWarning("Crest: Teleport detected (speed = " + speedKmh.ToString() + "), velocity discarded.", this);
                }

                speedKmh = _velocity.magnitude * 3.6f;
            }

            if (speedKmh > _maxSpeed)
            {
                // limit speed to max
                _velocityClamped = _velocity * _maxSpeed / speedKmh;

                if (_warnOnSpeedClamp)
                {
                    Debug.LogWarning("Crest: Speed (" + speedKmh.ToString() + ") exceeded max limited, clamped.", this);
                }
            }
            else
            {
                _velocityClamped = _velocity;
            }
        }

        // Weight based on submerged-amount of sphere
        void LateUpdateSphereWeight(float waterHeight, ref float weight)
        {
            var centerDepthInWater = waterHeight - transform.position.y;

            if (centerDepthInWater >= 0f)
            {
                // Center in water - exponential fall off of interaction influence as object gets deeper
                var prop = centerDepthInWater / _radius;
                prop *= 0.5f;
                weight *= Mathf.Exp(-prop * prop);
            }
            else
            {
                // Center out of water - ramp off with square root, weight goes to 0 when sphere is just touching water
                var height = -centerDepthInWater;
                var heightProp = 1f - Mathf.Clamp01(height / _radius);
                weight *= Mathf.Sqrt(heightProp);
            }
        }

        void OnEnable()
        {
            RegisterLodDataInput<LodDataMgrDynWaves>.RegisterInput(this, 0, transform.GetSiblingIndex());
        }

        void OnDisable()
        {
            RegisterLodDataInput<LodDataMgrDynWaves>.DeregisterInput(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position + _velocityOffset * _velocity, _radius);
        }

        public void Draw(LodDataMgr lodData, CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            var timeBeforeCurrentTime = (lodData as LodDataMgrDynWaves).TimeLeftToSimulate;

#if UNITY_EDITOR
            // Draw debug lines at each substep position. Alternate colours each frame so that substeps are clearly visible.
            if (_debugSubsteps)
            {
                var col = 0.7f * (Time.frameCount % 2 == 1 ? Color.green : Color.red);
                var pos = transform.position - _velocity * (timeBeforeCurrentTime - _velocityOffset);
                Debug.DrawLine(pos - transform.right + transform.up, pos + transform.right + transform.up, col, 0.5f);
            }
#endif

            // _renderMatrix is only updated at the frame update rate, whereas this input wants to apply
            // to substeps. Reconstruct the position of this input at the current substep time. This produces
            // much smoother interaction shapes for moving objects. Increasing sim freq helps further.
            var renderMatrix = _renderMatrix;
            var offset = _velocity * (timeBeforeCurrentTime - _velocityOffset);
            renderMatrix.m03 -= offset.x;
            renderMatrix.m13 -= offset.y;
            renderMatrix.m23 -= offset.z;

            _mpb.SetFloat(sp_weight, weight * _weightThisFrame);
            buf.DrawMesh(RegisterLodDataInputBase.QuadMesh, renderMatrix, _mat, 0, 0, _mpb);
        }
    }

    // Validation
#if UNITY_EDITOR
    public partial class SphereWaterInteraction : IValidated
    {
        public bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage)
        {
            var isValid = true;

            if (ocean != null && !ocean.CreateDynamicWaveSim && showMessage == ValidatedHelper.HelpBox)
            {
                showMessage
                (
                    "<i>SphereWaterInteraction</i> requires dynamic wave simulation to be enabled on <i>OceanRenderer</i>.",
                    $"Enable the <i>{LodDataMgrDynWaves.FEATURE_TOGGLE_LABEL}</i> option on the <i>OceanRenderer</i> component.",
                    ValidatedHelper.MessageType.Warning, ocean,
                    (so) => OceanRenderer.FixSetFeatureEnabled(so, LodDataMgrDynWaves.FEATURE_TOGGLE_NAME, true)
                );
            }

            return isValid;
        }
    }
#endif
}
