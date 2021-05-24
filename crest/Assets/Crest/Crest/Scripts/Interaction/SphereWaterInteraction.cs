// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

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
    public partial class SphereWaterInteraction : MonoBehaviour
    {
        float Radius => 0.5f * transform.lossyScale.x;

        [Range(-1f, 1f), SerializeField]
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

        FloatingObjectBase _object;

        Vector3 _posLast;

        SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();
        SampleFlowHelper _sampleFlowHelper = new SampleFlowHelper();

        Renderer _renderer;
        MaterialPropertyBlock _mpb;

        static int sp_velocity = Shader.PropertyToID("_Velocity");
        static int sp_weight = Shader.PropertyToID("_Weight");
        static int sp_simDeltaTime = Shader.PropertyToID("_SimDeltaTime");
        static int sp_radius = Shader.PropertyToID("_Radius");

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

            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();

            _object = GetComponentInParent<FloatingObjectBase>();
            if (_object == null)
            {
                _object = transform.parent.gameObject.AddComponent<ObjectWaterInteractionAdaptor>();
            }
        }

        void LateUpdate()
        {
            var ocean = OceanRenderer.Instance;
            if (ocean == null) return;

            _sampleHeightHelper.Init(transform.position, 2f * Radius);
            _sampleHeightHelper.Sample(out Vector3 disp, out _, out _);

            // Enforce upwards
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Velocity relative to water
            Vector3 relativeVelocity = LateUpdateComputeVelRelativeToWater(ocean);

            var dt = 1f / ocean._lodDataDynWaves.Settings._simulationFrequency;
            var weight = _weight;

            var waterHeight = disp.y + ocean.SeaLevel;
            LateUpdateSphereWeight(waterHeight, ref weight);

            _renderer.GetPropertyBlock(_mpb);

            _mpb.SetVector(sp_velocity, relativeVelocity);
            _mpb.SetFloat(sp_simDeltaTime, dt);
            _mpb.SetFloat(sp_radius, Radius);

            // Weighting with this value helps keep ripples consistent for different gravity values
            var gravityMul = Mathf.Sqrt(ocean._lodDataDynWaves.Settings._gravityMultiplier / 25f);
            _mpb.SetFloat(sp_weight, weight * gravityMul);

            _renderer.SetPropertyBlock(_mpb);

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
                _sampleFlowHelper.Init(transform.position, _object.ObjectWidth);
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
                    Debug.LogWarning("Teleport detected (speed = " + speedKmh.ToString() + "), velocity discarded.", this);
                }
            }
            else if (speedKmh > _maxSpeed)
            {
                // limit speed to max
                vel *= _maxSpeed / speedKmh;

                if (_warnOnSpeedClamp)
                {
                    Debug.LogWarning("Speed (" + speedKmh.ToString() + ") exceeded max limited, clamped.", this);
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
                var prop = centerDepthInWater / Radius;
                prop *= 0.5f;
                weight *= Mathf.Exp(-prop * prop);
            }
            else
            {
                // Center out of water - ramp off with square root, weight goes to 0 when sphere is just touching water
                var height = -centerDepthInWater;
                var heightProp = 1f - Mathf.Clamp01(height / Radius);
                weight *= Mathf.Sqrt(heightProp);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            sp_velocity = Shader.PropertyToID("_Velocity");
            sp_weight = Shader.PropertyToID("_Weight");
            sp_simDeltaTime = Shader.PropertyToID("_SimDeltaTime");
            sp_radius = Shader.PropertyToID("_Radius");
        }
    }

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

            if (transform.parent == null)
            {
                showMessage
                (
                    "<i>SphereWaterInteraction</i> component requires a parent <i>GameObject</i>.",
                    "Create a primary GameObject for the object, and parent this underneath it.",
                    ValidatedHelper.MessageType.Error, this
                );

                isValid = false;
            }

            if (GetComponent<RegisterDynWavesInput>() == null)
            {
                showMessage
                (
                    "<i>SphereWaterInteraction</i> component requires <i>RegisterDynWavesInput</i> component to be present.",
                    "Attach a <i>RegisterDynWavesInput</i> component.",
                    ValidatedHelper.MessageType.Error, this
                );

                isValid = false;
            }

            if (GetComponent<Renderer>() == null)
            {
                showMessage
                (
                    "<i>SphereWaterInteraction</i> component requires a <i>MeshRenderer</i> component.",
                    "Attach a <i>MeshRenderer</i> component.",
                    ValidatedHelper.MessageType.Error, this,
                    ValidatedHelper.FixAttachComponent<MeshRenderer>
                );

                isValid = false;
            }

            return isValid;
        }

        [CustomEditor(typeof(SphereWaterInteraction), true), CanEditMultipleObjects]
        class SphereWaterInteractionEditor : ValidatedEditor { }
    }
#endif
}
