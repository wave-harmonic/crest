// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Thanks to @VizzzU for contributing this.

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Applies simple approximation of buoyancy force - force based on submerged depth and torque based on alignment
    /// to water normal.
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Simple Floating Object")]
    public class SimpleFloatingObject : FloatingObjectBase
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Header("Buoyancy Force")]
        [Tooltip("Offsets center of object to raise it (or lower it) in the water.")]
        public float _raiseObject = 1f;
        [Tooltip("Strength of buoyancy force per meter of submersion in water.")]
        public float _buoyancyCoeff = 3f;
        [Tooltip("Strength of torque applied to match boat orientation to water normal.")]
        public float _boyancyTorque = 8f;
        [Tooltip("Approximate hydrodynamics of 'surfing' down waves."), Range(0, 1)]
        public float _accelerateDownhill = 0f;

        [Header("Wave Response")]
        [Tooltip("Diameter of object, for physics purposes. The larger this value, the more filtered/smooth the wave response will be.")]
        public float _objectWidth = 3f;
        public override float ObjectWidth => _objectWidth;

        [Header("Drag")]
        [Tooltip("Vertical offset for where drag force should be applied.")]
        public float _forceHeightOffset = -0.3f;
        public float _dragInWaterUp = 3f;
        public float _dragInWaterRight = 2f;
        public float _dragInWaterForward = 1f;
        public float _dragInWaterRotational = 0.2f;

        [Header("Debug")]
        [SerializeField] bool _debugDraw = false;

        bool _inWater;
        public override bool InWater => _inWater;

        public override Vector3 Velocity => _rb.velocity;

        Rigidbody _rb;

        SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();
        SampleFlowHelper _sampleFlowHelper = new SampleFlowHelper();

        void Start()
        {
            _rb = GetComponent<Rigidbody>();

            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }
        }

        void FixedUpdate()
        {
            UnityEngine.Profiling.Profiler.BeginSample("SimpleFloatingObject.FixedUpdate");

            if (OceanRenderer.Instance == null)
            {
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            _sampleHeightHelper.Init(transform.position, _objectWidth, true);
            _sampleHeightHelper.Sample(out Vector3 disp, out var normal, out var waterSurfaceVel);

            {
                _sampleFlowHelper.Init(transform.position, ObjectWidth);

                _sampleFlowHelper.Sample(out var surfaceFlow);
                waterSurfaceVel += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);
            }

            if (_debugDraw)
            {
                Debug.DrawLine(transform.position + 5f * Vector3.up, transform.position + 5f * Vector3.up + waterSurfaceVel,
                    new Color(1, 1, 1, 0.6f));
            }

            var velocityRelativeToWater = _rb.velocity - waterSurfaceVel;

            float height = disp.y + OceanRenderer.Instance.SeaLevel;

            float bottomDepth = height - transform.position.y + _raiseObject;

            _inWater = bottomDepth > 0f;
            if (!_inWater)
            {
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            var buoyancy = -Physics.gravity.normalized * _buoyancyCoeff * bottomDepth * bottomDepth * bottomDepth;
            _rb.AddForce(buoyancy, ForceMode.Acceleration);

            // Approximate hydrodynamics of sliding along water
            if (_accelerateDownhill > 0f)
            {
                _rb.AddForce(new Vector3(normal.x, 0f, normal.z) * -Physics.gravity.y * _accelerateDownhill, ForceMode.Acceleration);
            }

            // Apply drag relative to water
            var forcePosition = _rb.position + _forceHeightOffset * Vector3.up;
            _rb.AddForceAtPosition(Vector3.up * Vector3.Dot(Vector3.up, -velocityRelativeToWater) * _dragInWaterUp, forcePosition, ForceMode.Acceleration);
            _rb.AddForceAtPosition(transform.right * Vector3.Dot(transform.right, -velocityRelativeToWater) * _dragInWaterRight, forcePosition, ForceMode.Acceleration);
            _rb.AddForceAtPosition(transform.forward * Vector3.Dot(transform.forward, -velocityRelativeToWater) * _dragInWaterForward, forcePosition, ForceMode.Acceleration);

            FixedUpdateOrientation(normal);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// Align to water normal. One normal by default, but can use a separate normal based on boat length vs width. This gives
        /// varying rotations based on boat dimensions.
        /// </summary>
        void FixedUpdateOrientation(Vector3 normal)
        {
            if (_debugDraw) Debug.DrawLine(transform.position, transform.position + 5f * normal, Color.green);

            var torqueWidth = Vector3.Cross(transform.up, normal);
            _rb.AddTorque(torqueWidth * _boyancyTorque, ForceMode.Acceleration);
            _rb.AddTorque(-_dragInWaterRotational * _rb.angularVelocity);
        }
    }
}
