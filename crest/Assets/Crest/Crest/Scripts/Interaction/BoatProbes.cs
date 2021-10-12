// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Shout out to @holdingjason who posted a first version of this script here: https://github.com/huwb/crest-oceanrender/pull/100

using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.Serialization;

namespace Crest
{
    /// <summary>
    /// Boat physics by sampling at multiple probe points.
    /// </summary>
    [AddComponentMenu(Internal.Constants.MENU_PREFIX_SCRIPTS + "Boat Probes")]
    public class BoatProbes : FloatingObjectBase
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        [Header("Forces")]
        [Tooltip("Override RB center of mass, in local space."), SerializeField]
        Vector3 _centerOfMass = Vector3.zero;
        [SerializeField, FormerlySerializedAs("ForcePoints")]
        FloaterForcePoints[] _forcePoints = new FloaterForcePoints[] { };
        [Tooltip("Vertical offset for where engine force should be applied.")]
        public float _forceHeightOffset = 0f;
        public float _forceMultiplier = 10f;
        [Tooltip("Width dimension of boat. The larger this value, the more filtered/smooth the wave response will be.")]
        public float _minSpatialLength = 12f;
        [Range(0, 1)]
        public float _turningHeel = 0.35f;

        [Header("Drag")]
        public float _dragInWaterUp = 3f;
        public float _dragInWaterRight = 2f;
        public float _dragInWaterForward = 1f;

        [Header("Control")]
        [FormerlySerializedAs("EnginePower")]
        public float _enginePower = 7;
        [FormerlySerializedAs("TurnPower")]
        public float _turnPower = 0.5f;
        public bool _playerControlled = true;
        [Tooltip("Used to automatically add throttle input")]
        public float _engineBias = 0f;
        [Tooltip("Used to automatically add turning input")]
        public float _turnBias = 0f;

        private const float WATER_DENSITY = 1000;

        public override Vector3 Velocity => _rb.velocity;

        Rigidbody _rb;

        public override float ObjectWidth => _minSpatialLength;
        public override bool InWater => true;

        float _totalWeight;

        Vector3[] _queryPoints;
        Vector3[] _queryResultDisps;
        Vector3[] _queryResultVels;

        SampleFlowHelper _sampleFlowHelper = new SampleFlowHelper();

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.centerOfMass = _centerOfMass;

            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }

            CalcTotalWeight();

            _queryPoints = new Vector3[_forcePoints.Length + 1];
            _queryResultDisps = new Vector3[_forcePoints.Length + 1];
            _queryResultVels = new Vector3[_forcePoints.Length + 1];
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

            if (OceanRenderer.Instance == null)
            {
                return;
            }

            var collProvider = OceanRenderer.Instance.CollisionProvider;

            // Do queries
            UpdateWaterQueries(collProvider);

            var undispPos = transform.position - _queryResultDisps[_forcePoints.Length];
            undispPos.y = OceanRenderer.Instance.SeaLevel;

            var waterSurfaceVel = _queryResultVels[_forcePoints.Length];

            {
                _sampleFlowHelper.Init(transform.position, _minSpatialLength);
                _sampleFlowHelper.Sample(out var surfaceFlow);
                waterSurfaceVel += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);
            }

            // Buoyancy
            FixedUpdateBuoyancy();
            FixedUpdateDrag(waterSurfaceVel);
            FixedUpdateEngine();
        }

        void UpdateWaterQueries(ICollProvider collProvider)
        {
            // Update query points
            for (int i = 0; i < _forcePoints.Length; i++)
            {
                _queryPoints[i] = transform.TransformPoint(_forcePoints[i]._offsetPosition + new Vector3(0, _centerOfMass.y, 0));
            }
            _queryPoints[_forcePoints.Length] = transform.position;

            collProvider.Query(GetHashCode(), ObjectWidth, _queryPoints, _queryResultDisps, null, _queryResultVels);
        }

        void FixedUpdateEngine()
        {
            var forcePosition = _rb.position;

            var forward = _engineBias;
            if (_playerControlled) forward +=
#if ENABLE_INPUT_SYSTEM
                !Application.isFocused ? 0 :
                ((Keyboard.current.wKey.isPressed ? 1 : 0) + (Keyboard.current.sKey.isPressed ? -1 : 0));
#else
                Input.GetAxis("Vertical");
#endif
            _rb.AddForceAtPosition(transform.forward * _enginePower * forward, forcePosition, ForceMode.Acceleration);

            var sideways = _turnBias;
            if (_playerControlled) sideways +=
#if ENABLE_INPUT_SYSTEM
                !Application.isFocused ? 0 :
                ((Keyboard.current.aKey.isPressed ? -1f : 0f) +
                (Keyboard.current.dKey.isPressed ? 1f : 0f));
#else
                (Input.GetKey(KeyCode.A) ? -1f : 0f) +
                (Input.GetKey(KeyCode.D) ? 1f : 0f);
#endif
            var rotVec = transform.up + _turningHeel * transform.forward;
            _rb.AddTorque(rotVec * _turnPower * sideways, ForceMode.Acceleration);
        }

        void FixedUpdateBuoyancy()
        {
            var archimedesForceMagnitude = WATER_DENSITY * Mathf.Abs(Physics.gravity.y);

            for (int i = 0; i < _forcePoints.Length; i++)
            {
                var waterHeight = OceanRenderer.Instance.SeaLevel + _queryResultDisps[i].y;
                var heightDiff = waterHeight - _queryPoints[i].y;
                if (heightDiff > 0)
                {
                    _rb.AddForceAtPosition(archimedesForceMagnitude * heightDiff * Vector3.up * _forcePoints[i]._weight * _forceMultiplier / _totalWeight, _queryPoints[i]);
                }
            }
        }

        void FixedUpdateDrag(Vector3 waterSurfaceVel)
        {
            // Apply drag relative to water
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
