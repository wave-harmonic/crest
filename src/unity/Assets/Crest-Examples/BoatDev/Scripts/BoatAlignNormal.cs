// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

public class BoatAlignNormal : MonoBehaviour
{
    public float _bottomH = -1f;
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

    [SerializeField] bool _computeWaterVel = false;

    bool _inWater;
    public bool InWater { get { return _inWater; } }

    Vector3 _velocityRelativeToWater;
    public Vector3 VelocityRelativeToWater { get { return _velocityRelativeToWater; } }

    Vector3 _displacementToBoat;
    public Vector3 DisplacementToBoat { get { return _displacementToBoat; } }

    public bool _playerControlled = true;
    public float _throttleBias = 0f;
    public float _steerBias = 0f;

    [SerializeField] bool _debugDraw = false;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Manually trigger request update - this is because FixedUpdate executes before Update, so process the requests
        // early so that latest and greatest data is available. This should remove a frame of latency from physics!
        foreach(var aw in OceanRenderer.Instance._lodDataAnimWaves)
        {
            if (aw && aw.DataReadback)
            {
                aw.DataReadback.UpdateProcessRequests();
            }
        }

        var colProvider = OceanRenderer.Instance.CollisionProvider;
        var position = transform.position;

        Vector3 undispPos;
        if (!colProvider.ComputeUndisplacedPosition(ref position, out undispPos)) return;
        if (_debugDraw) DebugDrawCross(undispPos, 1f, Color.red);

        var waterSurfaceVel = Vector3.zero;
        bool dispValid, velValid;
        colProvider.SampleDisplacementVel(ref undispPos, out _displacementToBoat, out dispValid, out waterSurfaceVel, out velValid, _boatWidth);
        if (!dispValid) return;

        if (!_computeWaterVel)
        {
            waterSurfaceVel = Vector3.zero;
        }

        if (OceanRenderer.Instance._createFlowSim)
        {
            Vector2 surfaceFlow;
            int lod  = LodDataFlow.SuggestDataLOD(new Rect(position.x, position.z, 0f, 0f), _boatWidth);
            if(lod != -1) {
                if(OceanRenderer.Instance._lodDataAnimWaves[lod].LDFlow.SampleFlow(ref position, out surfaceFlow)) {
                    waterSurfaceVel += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);
                }
            }
        }
        if (_debugDraw)
        {
            Debug.DrawLine(transform.position + 5f * Vector3.up, transform.position + 5f * Vector3.up + waterSurfaceVel,
                new Color(1, 1, 1, 0.6f));
        }

        Vector3 normal;
        if (!colProvider.SampleNormal(ref undispPos, out normal, _boatWidth)) return;
        if(_debugDraw) Debug.DrawLine(transform.position, transform.position + 5f * normal, Color.green);

        if (velValid)
        {
            _velocityRelativeToWater = _rb.velocity - waterSurfaceVel;
        }

        var dispPos = undispPos + _displacementToBoat;
        if (_debugDraw) DebugDrawCross(dispPos, 4f, Color.white);

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

        float forward = _throttleBias;
        if(_playerControlled) forward += Input.GetAxis("Vertical");
        _rb.AddForceAtPosition(transform.forward * _enginePower * forward, forcePosition, ForceMode.Acceleration);

        float sideways = _steerBias;
        if(_playerControlled ) sideways += (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f);
        _rb.AddTorque(transform.up * _turnPower * sideways, ForceMode.Acceleration);

        // align to normal
        var current = transform.up;
        var target = normal;
        var torque = Vector3.Cross(current, target);
        _rb.AddTorque(torque * _boyancyTorque, ForceMode.Acceleration);
    }

    void DebugDrawCross(Vector3 pos, float r, Color col)
    {
        Debug.DrawLine(pos - Vector3.up * r, pos + Vector3.up * r, col);
        Debug.DrawLine(pos - Vector3.right * r, pos + Vector3.right * r, col);
        Debug.DrawLine(pos - Vector3.forward * r, pos + Vector3.forward * r, col);
    }
}
