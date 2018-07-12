// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

public class BoatAlignNormal : MonoBehaviour
{
    public float _bottomH = -1f;
    public bool _debugDraw = false;
    public float _overrideProbeRadius = -1f;
    public float _buoyancyCoeff = 40000f;
    public float _boyancyTorque = 2f;

    public float _forceHeightOffset = -1f;
    public float _enginePower = 10000f;
    public float _turnPower = 100f;

    public float _boatWidth = 2f;

    Rigidbody _rb;

    public float _dragInWaterUp = 20000f;
    public float _dragInWaterRight = 20000f;
    public float _dragInWaterForward = 20000f;

    bool _inWater;
    public bool InWater { get { return _inWater; } }

    Vector3 _velocityRelativeToWater;
    public Vector3 VelocityRelativeToWater { get { return _velocityRelativeToWater; } }

    Vector3 _displacementToBoat, _displacementToBoatLastFrame;
    bool _displacementToBoatInitd = false;
    public Vector3 DisplacementToBoat { get { return _displacementToBoat; } }

    public bool _holdThrottle = false;
    public float _steerBias = 0f;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        var colProvider = OceanRenderer.Instance.CollisionProvider;
        var position = transform.position;

        var undispPos = Vector3.zero;
        if (!colProvider.ComputeUndisplacedPosition(ref position, ref undispPos)) return;

        if (!colProvider.SampleDisplacement(ref undispPos, ref _displacementToBoat)) return;
        if (!_displacementToBoatInitd)
        {
            _displacementToBoatLastFrame = _displacementToBoat;
            _displacementToBoatInitd = true;
        }

        // estimate water velocity
        Vector3 velWater = (_displacementToBoat - _displacementToBoatLastFrame) / Time.deltaTime;
        _displacementToBoatLastFrame = _displacementToBoat;

        var normal = Vector3.zero;
        if (!colProvider.SampleNormal(ref undispPos, ref normal, _boatWidth)) return;
        Debug.DrawLine(transform.position, transform.position + 5f * normal);

        _velocityRelativeToWater = _rb.velocity - velWater;

        var dispPos = undispPos + _displacementToBoat;
        float height = dispPos.y;

        float bottomDepth = height - transform.position.y - _bottomH;

        _inWater = bottomDepth > 0f;
        if (!_inWater)
        {
            return;
        }

        var buoyancy = -Physics.gravity.normalized * _buoyancyCoeff * bottomDepth * bottomDepth * bottomDepth;
        _rb.AddForce(buoyancy, ForceMode.Acceleration);


        // apply drag relative to water
        var forcePosition = _rb.position + _forceHeightOffset * Vector3.up;
        _rb.AddForceAtPosition(Vector3.up * Vector3.Dot(Vector3.up, -_velocityRelativeToWater) * _dragInWaterUp, forcePosition, ForceMode.Acceleration);
        _rb.AddForceAtPosition(transform.right * Vector3.Dot(transform.right, -_velocityRelativeToWater) * _dragInWaterRight, forcePosition, ForceMode.Acceleration);
        _rb.AddForceAtPosition(transform.forward * Vector3.Dot(transform.forward, -_velocityRelativeToWater) * _dragInWaterForward, forcePosition, ForceMode.Acceleration);

        float forward = _holdThrottle ? 1f : Input.GetAxis("Vertical");
        _rb.AddForceAtPosition(transform.forward * _enginePower * forward, forcePosition, ForceMode.Acceleration);
        //Debug.DrawLine(transform.position + Vector3.up * 5f, transform.position + 5f * (Vector3.up + transform.forward));
        float sideways = _steerBias + (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f);
        _rb.AddTorque(transform.up * _turnPower * sideways, ForceMode.Acceleration);

        // align to normal
        var current = transform.up;
        var target = normal;
        var torque = Vector3.Cross(current, target);
        _rb.AddTorque(torque * _boyancyTorque, ForceMode.Acceleration);
    }
}
