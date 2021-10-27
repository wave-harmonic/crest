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
    public partial class SphereWaterInteraction : MonoBehaviour, ILodDataInput
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Range(0.01f, 50f), SerializeField]
        float _radius = 1f;

        [Range(-4f, 4f), SerializeField]
        float _weight = 1f;
        [Range(0f, 2f), SerializeField]
        float _weightUpDownMul = 0.5f;

        [Header("Limits")]
        [Tooltip("Teleport speed (km/h) - if the calculated speed is larger than this amount, the object is deemed to have teleported and the computed velocity is discarded."), SerializeField]
        float _teleportSpeed = 500f;
        [SerializeField]
        bool _warnOnTeleport = false;
        [Tooltip("Maximum speed clamp (km/h), useful for controlling/limiting wake."), SerializeField]
        float _maxSpeed = 100f;
        [SerializeField]
        bool _warnOnSpeedClamp = false;

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

            // Enforce upwards
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Velocity relative to water
            Vector3 relativeVelocity = LateUpdateComputeVelRelativeToWater(ocean);

            var dt = 1f / ocean._lodDataDynWaves.Settings._simulationFrequency;

            // Use weight from user with a multiplier to make interactions look plausible
            _weightThisFrame = 3.75f * _weight;

            var waterHeight = disp.y + ocean.SeaLevel;
            LateUpdateSphereWeight(waterHeight, ref _weightThisFrame);

            _mpb.SetVector(sp_velocity, relativeVelocity);
            _mpb.SetFloat(sp_simDeltaTime, dt);
            _mpb.SetFloat(sp_radius, _radius);
            _mpb.SetVector(RegisterLodDataInputBase.sp_DisplacementAtInputPosition, disp);

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
        Vector3 LateUpdateComputeVelRelativeToWater(OceanRenderer ocean)
        {
            Vector3 vel;

            // feed in water velocity
            vel = (transform.position - _posLast) / ocean.DeltaTimeDynamics;
            if (ocean.DeltaTimeDynamics < 0.0001f)
            {
                vel = Vector3.zero;
            }

            {
                _sampleFlowHelper.Init(transform.position, 2f * _radius);
                _sampleFlowHelper.Sample(out var surfaceFlow);
                vel -= new Vector3(surfaceFlow.x, 0, surfaceFlow.y);
            }
            vel.y *= _weightUpDownMul;

            var speedKmh = vel.magnitude * 3.6f;
            if (speedKmh > _teleportSpeed)
            {
                // teleport detected
                vel *= 0f;

                if (_warnOnTeleport)
                {
                    Debug.LogWarning("Crest: Teleport detected (speed = " + speedKmh.ToString() + "), velocity discarded.", this);
                }
            }
            else if (speedKmh > _maxSpeed)
            {
                // limit speed to max
                vel *= _maxSpeed / speedKmh;

                if (_warnOnSpeedClamp)
                {
                    Debug.LogWarning("Crest: Speed (" + speedKmh.ToString() + ") exceeded max limited, clamped.", this);
                }
            }

            return vel;
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
            var registered = RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrDynWaves));
            registered.Remove(this);
            registered.Add(0, this);
        }

        void OnDisable()
        {
            RegisterLodDataInputBase.GetRegistrar(typeof(LodDataMgrDynWaves)).Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }

        public void Draw(CommandBuffer buf, float weight, int isTransition, int lodIdx)
        {
            _mpb.SetFloat(sp_weight, weight * _weightThisFrame);
            buf.DrawMesh(RegisterLodDataInputBase.QuadMesh, _renderMatrix, _mat, 0, 0, _mpb);
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

        [CustomEditor(typeof(SphereWaterInteraction), true), CanEditMultipleObjects]
        class SphereWaterInteractionEditor : ValidatedEditor { }
    }
#endif
}
