// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Shout out to @holdingjason who posted a first version of this script here: https://github.com/huwb/crest-oceanrender/pull/100

using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Crest
{
    /// <summary>
    /// Boat physics by sampling at multiple probe points.
    /// </summary>
    public class BoatProbes : FloatingObjectBase
    {
        [Header("Forces")]
        [Tooltip("Override RB center of mass, in local space."), SerializeField]
        Vector3 _centerOfMass = Vector3.zero;
        [SerializeField, FormerlySerializedAs("ForcePoints")]
        FloaterForcePoints[] _forcePoints = new FloaterForcePoints[] { };
        [SerializeField]
        float _forceHeightOffset = 0f;
        [SerializeField]
        float _forceMultiplier = 10f;
        [SerializeField]
        float _minSpatialLength = 12f;
        [SerializeField, Range(0, 1)]
        float _turningHeel = 0.35f;

        [Header("Drag")]
        [SerializeField]
        float _dragInWaterUp = 3f;
        [SerializeField]
        float _dragInWaterRight = 2f;
        [SerializeField]
        float _dragInWaterForward = 1f;

        [Header("Control")]
        [SerializeField, FormerlySerializedAs("EnginePower")]
        float _enginePower = 7;
        [SerializeField, FormerlySerializedAs("TurnPower")]
        float _turnPower = 0.5f;
        [SerializeField]
        bool _playerControlled = true;
        [SerializeField]
        float _engineBias = 0f;
        [SerializeField]
        float _turnBias = 0f;


        private const float WATER_DENSITY = 1000;

        public override Vector3 Velocity => _rb.velocity;

        Rigidbody _rb;

        Vector3 _displacementToObject = Vector3.zero;
        public override Vector3 CalculateDisplacementToObject() { return _displacementToObject; }

        public override float ObjectWidth { get { return _minSpatialLength; } }
        public override bool InWater { get { return true; } }

        SamplingData _samplingData = new SamplingData();
        SamplingData _samplingDataFlow = new SamplingData();

        Rect _localSamplingAABB;
        float _totalWeight;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.centerOfMass = _centerOfMass;

            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }

            _localSamplingAABB = ComputeLocalSamplingAABB();

            CalcTotalWeight();
        }

        void CalcTotalWeight()
        {
            _totalWeight = 0f;
            foreach (var pt in _forcePoints)
            {
                _totalWeight += pt._weight;
            }
        }

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            // Sum weights every frame when running in editor in case weights are edited in the inspector.
            CalcTotalWeight();
#endif

            // Trigger processing of displacement textures that have come back this frame. This will be processed
            // anyway in Update(), but FixedUpdate() is earlier so make sure it's up to date now.
            if (OceanRenderer.Instance._simSettingsAnimatedWaves.CollisionSource == SimSettingsAnimatedWaves.CollisionSources.OceanDisplacementTexturesGPU && GPUReadbackDisps.Instance)
            {
                GPUReadbackDisps.Instance.ProcessRequests();
            }

            var collProvider = OceanRenderer.Instance.CollisionProvider;
            var thisRect = GetWorldAABB();
            if (!collProvider.GetSamplingData(ref thisRect, _minSpatialLength, _samplingData))
            {
                // No collision coverage for the sample area, in this case use the null provider.
                collProvider = CollProviderNull.Instance;
            }

            var position = transform.position;
            Vector3 undispPos;
            if (!collProvider.ComputeUndisplacedPosition(ref position, _samplingData, out undispPos))
            {
                // If we couldn't get wave shape, assume flat water at sea level
                undispPos = position;
                undispPos.y = OceanRenderer.Instance.SeaLevel;
            }

            Vector3 displacement, waterSurfaceVel;
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
                if (GPUReadbackFlow.Instance.GetSamplingData(ref flowRect, _minSpatialLength, _samplingDataFlow))
                {
                    Vector2 surfaceFlow;
                    GPUReadbackFlow.Instance.SampleFlow(ref position, _samplingDataFlow, out surfaceFlow);
                    waterSurfaceVel += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);

                    GPUReadbackFlow.Instance.ReturnSamplingData(_samplingDataFlow);
                }
            }

            FixedUpdateBuoyancy(collProvider);
            FixedUpdateDrag(collProvider, waterSurfaceVel);
            FixedUpdateEngine();

            collProvider.ReturnSamplingData(_samplingData);
        }

        void FixedUpdateEngine()
        {
            var forcePosition = _rb.position;

            var forward = _engineBias;
            if (_playerControlled) forward += Input.GetAxis("Vertical");
            _rb.AddForceAtPosition(transform.forward * _enginePower * forward, forcePosition, ForceMode.Acceleration);

            var sideways = _turnBias;
            if (_playerControlled) sideways += (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f);
            var rotVec = transform.up + _turningHeel * transform.forward;
            _rb.AddTorque(rotVec * _turnPower * sideways, ForceMode.Acceleration);
        }

        void FixedUpdateBuoyancy(ICollProvider collProvider)
        {
            float archimedesForceMagnitude = WATER_DENSITY * Mathf.Abs(Physics.gravity.y);

            for (int i = 0; i < _forcePoints.Length; i++)
            {
                FloaterForcePoints point = _forcePoints[i];
                var transformedPoint = transform.TransformPoint(point._offsetPosition + new Vector3(0, _centerOfMass.y, 0));

                Vector3 undispPos;
                if (!collProvider.ComputeUndisplacedPosition(ref transformedPoint, _samplingData, out undispPos))
                {
                    // If we couldn't get wave shape, assume flat water at sea level
                    undispPos = transformedPoint;
                    undispPos.y = OceanRenderer.Instance.SeaLevel;
                }

                Vector3 displaced;
                collProvider.SampleDisplacement(ref undispPos, _samplingData, out displaced);

                var dispPos = undispPos + displaced;
                var heightDiff = dispPos.y - transformedPoint.y;
                if (heightDiff > 0)
                {
                    _rb.AddForceAtPosition(archimedesForceMagnitude * heightDiff * Vector3.up * point._weight * _forceMultiplier / _totalWeight, transformedPoint);
                }
            }
        }

        void FixedUpdateDrag(ICollProvider collProvider, Vector3 waterSurfaceVel)
        {
            // Apply drag relative to water
            var pos = _rb.position;
            Vector3 undispPos;
            if (!collProvider.ComputeUndisplacedPosition(ref pos, _samplingData, out undispPos))
            {
                // If we couldn't get wave shape, assume flat water at sea level
                undispPos = pos;
                undispPos.y = OceanRenderer.Instance.SeaLevel;
            }

            var _velocityRelativeToWater = _rb.velocity - waterSurfaceVel;

            var forcePosition = _rb.position + _forceHeightOffset * Vector3.up;
            _rb.AddForceAtPosition(Vector3.up * Vector3.Dot(Vector3.up, -_velocityRelativeToWater) * _dragInWaterUp, forcePosition, ForceMode.Acceleration);
            _rb.AddForceAtPosition(transform.right * Vector3.Dot(transform.right, -_velocityRelativeToWater) * _dragInWaterRight, forcePosition, ForceMode.Acceleration);
            _rb.AddForceAtPosition(transform.forward * Vector3.Dot(transform.forward, -_velocityRelativeToWater) * _dragInWaterForward, forcePosition, ForceMode.Acceleration);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(transform.TransformPoint(_centerOfMass), Vector3.one * 0.25f);

            for (int i = 0; i < _forcePoints.Length; i++)
            {
                var point = _forcePoints[i];

                var transformedPoint = transform.TransformPoint(point._offsetPosition + new Vector3(0, _centerOfMass.y, 0));

                Gizmos.color = Color.red;
                Gizmos.DrawCube(transformedPoint, Vector3.one * 0.5f);
            }

            var worldAABB = GetWorldAABB();
            new Bounds(new Vector3(worldAABB.center.x, 0f, worldAABB.center.y), Vector3.right * worldAABB.width + Vector3.forward * worldAABB.height).DebugDraw();
        }

        Rect ComputeLocalSamplingAABB()
        {
            if (_forcePoints.Length == 0) return new Rect();

            float xmin = _forcePoints[0]._offsetPosition.x;
            float zmin = _forcePoints[0]._offsetPosition.z;
            float xmax = xmin, zmax = zmin;
            for (int i = 1; i < _forcePoints.Length; i++)
            {
                float x = _forcePoints[i]._offsetPosition.x, z = _forcePoints[i]._offsetPosition.z;
                xmin = Mathf.Min(xmin, x); xmax = Mathf.Max(xmax, x);
                zmin = Mathf.Min(zmin, z); zmax = Mathf.Max(zmax, z);
            }

            return Rect.MinMaxRect(xmin, zmin, xmax, zmax);
        }

        Rect GetWorldAABB()
        {
            Bounds b = new Bounds(transform.position, Vector3.one);
            b.Encapsulate(transform.TransformPoint(new Vector3(_localSamplingAABB.xMin, 0f, _localSamplingAABB.yMin)));
            b.Encapsulate(transform.TransformPoint(new Vector3(_localSamplingAABB.xMin, 0f, _localSamplingAABB.yMax)));
            b.Encapsulate(transform.TransformPoint(new Vector3(_localSamplingAABB.xMax, 0f, _localSamplingAABB.yMin)));
            b.Encapsulate(transform.TransformPoint(new Vector3(_localSamplingAABB.xMax, 0f, _localSamplingAABB.yMax)));
            return Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);
        }
    }

    [Serializable]
    public class FloaterForcePoints
    {
        [FormerlySerializedAs("_factor")]
        public float _weight = 1f;

        public Vector3 _offsetPosition;
    }
}
