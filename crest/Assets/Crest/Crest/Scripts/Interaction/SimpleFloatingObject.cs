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
        [SerializeField]
        bool _debugDraw = false;
        [SerializeField] bool _debugValidateCollision = false;

        bool _inWater;
        public override bool InWater { get { return _inWater; } }

        Vector3 _displacementToObject = Vector3.zero;
        public override Vector3 CalculateDisplacementToObject() { return _displacementToObject; }

        public override Vector3 Velocity => _rb.velocity;

        Rigidbody _rb;

        SamplingData _samplingData = new SamplingData();
        SamplingData _samplingDataLengthWise = new SamplingData();
        SamplingData _samplingDataFlow = new SamplingData();

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
            UnityEngine.Profiling.Profiler.BeginSample("BoatAlignNormal.FixedUpdate");

            if (OceanRenderer.Instance == null)
            {
                return;
            }

            // Trigger processing of displacement textures that have come back this frame. This will be processed
            // anyway in Update(), but FixedUpdate() is earlier so make sure it's up to date now.
            if (OceanRenderer.Instance._simSettingsAnimatedWaves.CollisionSource == SimSettingsAnimatedWaves.CollisionSources.OceanDisplacementTexturesGPU && GPUReadbackDisps.Instance)
            {
                GPUReadbackDisps.Instance.ProcessRequests();
            }

            var collProvider = OceanRenderer.Instance.CollisionProvider;
            var position = transform.position;

            var thisRect = new Rect(transform.position.x, transform.position.z, 0f, 0f);
            if (!collProvider.GetSamplingData(ref thisRect, _objectWidth, _samplingData))
            {
                // No collision coverage for the sample area, in this case use the null provider.
                collProvider = CollProviderNull.Instance;
            }

            if (_debugValidateCollision)
            {
                var result = collProvider.CheckAvailability(ref position, _samplingData);
                if (result != AvailabilityResult.DataAvailable)
                {
                    Debug.LogWarning("Validation failed: " + result.ToString() + ". See comments on the AvailabilityResult enum.", this);
                }
            }

            Vector3 undispPos;
            if (!collProvider.ComputeUndisplacedPosition(ref position, _samplingData, out undispPos))
            {
                // If we couldn't get wave shape, assume flat water at sea level
                undispPos = position;
                undispPos.y = OceanRenderer.Instance.SeaLevel;
            }
            if (_debugDraw) DebugDrawCross(undispPos, 1f, Color.red);

            Vector3 waterSurfaceVel, displacement;
            bool dispValid, velValid;
            collProvider.SampleDisplacementVel(ref undispPos, _samplingData, out displacement, out dispValid, out waterSurfaceVel, out velValid);
            if (dispValid)
            {
                _displacementToObject = displacement;
            }

            if (GPUReadbackFlow.Instance)
            {
                GPUReadbackFlow.Instance.ProcessRequests();

                var flowRect = new Rect(position.x, position.z, 0f, 0f);
                GPUReadbackFlow.Instance.GetSamplingData(ref flowRect, _objectWidth, _samplingDataFlow);

                Vector2 surfaceFlow;
                GPUReadbackFlow.Instance.SampleFlow(ref position, _samplingDataFlow, out surfaceFlow);
                waterSurfaceVel += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);

                GPUReadbackFlow.Instance.ReturnSamplingData(_samplingDataFlow);
            }

            if (_debugDraw)
            {
                Debug.DrawLine(transform.position + 5f * Vector3.up, transform.position + 5f * Vector3.up + waterSurfaceVel,
                    new Color(1, 1, 1, 0.6f));
            }

            var velocityRelativeToWater = _rb.velocity - waterSurfaceVel;

            var dispPos = undispPos + _displacementToObject;
            if (_debugDraw) DebugDrawCross(dispPos, 4f, Color.white);

            float height = dispPos.y;

            float bottomDepth = height - transform.position.y + _raiseObject;

            _inWater = bottomDepth > 0f;
            if (!_inWater)
            {
                return;
            }

            var buoyancy = -Physics.gravity.normalized * _buoyancyCoeff * bottomDepth * bottomDepth * bottomDepth;
            _rb.AddForce(buoyancy, ForceMode.Acceleration);


            // apply drag relative to water
            var forcePosition = _rb.position + _forceHeightOffset * Vector3.up;
            _rb.AddForceAtPosition(Vector3.up * Vector3.Dot(Vector3.up, -velocityRelativeToWater) * _dragInWaterUp, forcePosition, ForceMode.Acceleration);
            _rb.AddForceAtPosition(transform.right * Vector3.Dot(transform.right, -velocityRelativeToWater) * _dragInWaterRight, forcePosition, ForceMode.Acceleration);
            _rb.AddForceAtPosition(transform.forward * Vector3.Dot(transform.forward, -velocityRelativeToWater) * _dragInWaterForward, forcePosition, ForceMode.Acceleration);

            FixedUpdateOrientation(collProvider, undispPos);

            collProvider.ReturnSamplingData(_samplingData);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// Align to water normal. One normal by default, but can use a separate normal based on boat length vs width. This gives
        /// varying rotations based on boat dimensions.
        /// </summary>
        void FixedUpdateOrientation(ICollProvider collProvider, Vector3 undisplacedPos)
        {
            Vector3 normal, normalLongitudinal = Vector3.up;
            if (!collProvider.SampleNormal(ref undisplacedPos, _samplingData, out normal))
            {
                normal = Vector3.up;
            }

            if (_debugDraw) Debug.DrawLine(transform.position, transform.position + 5f * normal, Color.green);

            var torqueWidth = Vector3.Cross(transform.up, normal);
            _rb.AddTorque(torqueWidth * _boyancyTorque, ForceMode.Acceleration);
            _rb.AddTorque(-_dragInWaterRotational * _rb.angularVelocity);
        }

#if UNITY_EDITOR
        //private void Update()
        //{
        //    UpdateDebugDrawSurroundingColl();
        //}

        private void UpdateDebugDrawSurroundingColl()
        {
            var r = 5f;
            var steps = 10;

            var collProvider = OceanRenderer.Instance.CollisionProvider;
            var thisRect = new Rect(transform.position.x - r * steps / 2f, transform.position.z - r * steps / 2f, r * steps / 2f, r * steps / 2f);
            if (!collProvider.GetSamplingData(ref thisRect, _objectWidth, _samplingData))
            {
                return;
            }

            for (float i = 0; i < steps; i++)
            {
                for (float j = 0; j < steps; j++)
                {
                    Vector3 pos = new Vector3(((i + 0.5f) - steps / 2f) * r, 0f, ((j + 0.5f) - steps / 2f) * r);
                    pos.x += transform.position.x;
                    pos.z += transform.position.z;

                    Vector3 disp;
                    if (collProvider.SampleDisplacement(ref pos, _samplingData, out disp))
                    {
                        DebugDrawCross(pos + disp, 1f, Color.green);
                    }
                    else
                    {
                        DebugDrawCross(pos, 0.25f, Color.red);
                    }
                }
            }

            collProvider.ReturnSamplingData(_samplingData);
        }
#endif

        void DebugDrawCross(Vector3 pos, float r, Color col)
        {
            Debug.DrawLine(pos - Vector3.up * r, pos + Vector3.up * r, col);
            Debug.DrawLine(pos - Vector3.right * r, pos + Vector3.right * r, col);
            Debug.DrawLine(pos - Vector3.forward * r, pos + Vector3.forward * r, col);
        }
    }
}
