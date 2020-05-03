// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Thanks to @VizzzU for contributing this.

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Applies simple approximation of buoyancy force - force based on submerged depth and torque based on alignment
    /// to water normal.
    /// </summary>
    public class SimpleFloatingObject : FloatingObjectBase
    {
        [Header("Buoyancy Force")]
        [Tooltip("Offsets center of object to raise it (or lower it) in the water."), SerializeField]
        float _raiseObject = 1f;
        [Tooltip("Strength of buoyancy force per meter of submersion in water."), SerializeField]
        float _buoyancyCoeff = 3f;
        [Tooltip("Strength of torque applied to match boat orientation to water normal."), SerializeField]
        float _boyancyTorque = 8f;

        [Header("Wave Response")]
        [Tooltip("Diameter of object, for physics purposes. The larger this value, the more filtered/smooth the wave response will be."), SerializeField]
        float _objectWidth = 3f;
        public override float ObjectWidth { get { return _objectWidth; } }

        [Header("Drag")]
        [Tooltip("Vertical offset for where drag force should be applied."), SerializeField]
        float _forceHeightOffset = -0.3f;
        [SerializeField] float _dragInWaterUp = 3f;
        [SerializeField] float _dragInWaterRight = 2f;
        [SerializeField] float _dragInWaterForward = 1f;
        [SerializeField] float _dragInWaterRotational = 0.2f;

        [Header("Debug")]
        [SerializeField] bool _debugDraw = false;

        bool _inWater;
        public override bool InWater { get { return _inWater; } }

        Vector3 _displacementToObject = Vector3.zero;
        public override Vector3 CalculateDisplacementToObject() { return _displacementToObject; }

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

            var collProvider = OceanRenderer.Instance.CollisionProvider;
            var position = transform.position;

            var normal = Vector3.up; var waterSurfaceVel = Vector3.zero;
            _sampleHeightHelper.Init(transform.position, _objectWidth, true);
            _sampleHeightHelper.Sample(ref _displacementToObject, ref normal, ref waterSurfaceVel);

            var undispPos = transform.position - _displacementToObject;
            undispPos.y = OceanRenderer.Instance.SeaLevel;

            if (_debugDraw) VisualiseCollisionArea.DebugDrawCross(undispPos, 1f, Color.red);

            if (QueryFlow.Instance)
            {
                _sampleFlowHelper.Init(transform.position, ObjectWidth);

                Vector2 surfaceFlow = Vector2.zero;
                _sampleFlowHelper.Sample(ref surfaceFlow);
                waterSurfaceVel += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);
            }

            if (_debugDraw)
            {
                Debug.DrawLine(transform.position + 5f * Vector3.up, transform.position + 5f * Vector3.up + waterSurfaceVel,
                    new Color(1, 1, 1, 0.6f));
            }

            var velocityRelativeToWater = _rb.velocity - waterSurfaceVel;

            var dispPos = undispPos + _displacementToObject;
            if (_debugDraw) VisualiseCollisionArea.DebugDrawCross(dispPos, 4f, Color.white);

            float height = dispPos.y;

            float bottomDepth = height - transform.position.y + _raiseObject;

            _inWater = bottomDepth > 0f;
            if (!_inWater)
            {
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            var buoyancy = -Physics.gravity.normalized * _buoyancyCoeff * bottomDepth * bottomDepth * bottomDepth;
            _rb.AddForce(buoyancy, ForceMode.Acceleration);


            // apply drag relative to water
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
            Vector3 normalLongitudinal = Vector3.up;

            if (_debugDraw) Debug.DrawLine(transform.position, transform.position + 5f * normal, Color.green);

            var torqueWidth = Vector3.Cross(transform.up, normal);
            _rb.AddTorque(torqueWidth * _boyancyTorque, ForceMode.Acceleration);
            _rb.AddTorque(-_dragInWaterRotational * _rb.angularVelocity);
        }
    }
}
