// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

/// <summary>
/// Simple type of buoyancy - takes one sample and matches boat height and orientation to water height and normal.
/// </summary>
public class BoatAlignNormal : MonoBehaviour, IBoat
{
    [Header("Buoyancy Force")]
    [Tooltip("Height offset from transform center to bottom of boat (if any)."), SerializeField]
    float _bottomH = 0f;
    [Tooltip("Strength of buoyancy force per meter of submersion in water."), SerializeField]
    float _buoyancyCoeff = 1.5f;
    [Tooltip("Strength of torque applied to match boat orientation to water normal."), SerializeField]
    float _boyancyTorque = 8f;

    [Header("Engine Power")]
    [Tooltip("Vertical offset for where engine force should be applied."), SerializeField]
    float _forceHeightOffset = -0.3f;
    [SerializeField] float _enginePower = 11f;
    [SerializeField] float _turnPower = 1.3f;

    [Header("Wave Response")]
    [Tooltip("Width dimension of boat. The larger this value, the more filtered/smooth the wave response will be."), SerializeField]
    float _boatWidth = 3f;
    public float BoatWidth { get { return _boatWidth; } }

    [SerializeField, Tooltip("Computes a separate normal based on boat length to get more accurate orientations, at the cost of an extra collision sample.")]
    bool _useBoatLength = false;
    [Tooltip("Length dimension of boat. Only used if Use Boat Length is enabled."), SerializeField]
    float _boatLength = 3f;

    [Header("Drag")]
    [SerializeField] float _dragInWaterUp = 3f;
    [SerializeField] float _dragInWaterRight = 2f;
    [SerializeField] float _dragInWaterForward = 1f;

    [Header("Controls")]
    [SerializeField] bool _playerControlled = true;
    [Tooltip("Used to automatically add throttle input"), SerializeField]
    float _throttleBias = 0f;
    [Tooltip("Used to automatically add turning input"), SerializeField]
    float _steerBias = 0f;

    [Header("Debug")]
    [SerializeField] bool _debugDraw = false;
    [SerializeField] bool _debugValidateCollision = false;

    bool _inWater;
    public bool InWater { get { return _inWater; } }

    Vector3 _velocityRelativeToWater;
    public Vector3 VelocityRelativeToWater { get { return _velocityRelativeToWater; } }

    Vector3 _displacementToBoat;
    public Vector3 DisplacementToBoat { get { return _displacementToBoat; } }

    Rigidbody _rb;

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
        // Trigger processing of displacement textures that have come back this frame. This will be processed
        // anyway in Update(), but FixedUpdate() is earlier so make sure it's up to date now.
        if (OceanRenderer.Instance._simSettingsAnimatedWaves.CollisionSource == SimSettingsAnimatedWaves.CollisionSources.OceanDisplacementTexturesGPU && GPUReadbackDisps.Instance)
        {
            GPUReadbackDisps.Instance.ProcessRequests();
        }

        var collProvider = OceanRenderer.Instance.CollisionProvider;
        var position = transform.position;

        if (_debugValidateCollision)
        {
            var result = collProvider.CheckAvailability(ref position, _boatWidth);
            if (result != AvailabilityResult.DataAvailable)
            {
                Debug.LogWarning("Validation failed: " + result.ToString() + ". See comments on the AvailabilityResult enum.", this);
            }
        }

        Vector3 undispPos;
        if (!collProvider.ComputeUndisplacedPosition(ref position, out undispPos, _boatWidth))
        {
            // If we couldn't get wave shape, assume flat water at sea level
            undispPos = position;
            undispPos.y = OceanRenderer.Instance.SeaLevel;
        }
        if (_debugDraw) DebugDrawCross(undispPos, 1f, Color.red);

        var waterSurfaceVel = Vector3.zero;
        bool dispValid, velValid;
        collProvider.SampleDisplacementVel(ref undispPos, out _displacementToBoat, out dispValid, out waterSurfaceVel, out velValid, _boatWidth);

        if (GPUReadbackFlow.Instance)
        {
            GPUReadbackFlow.Instance.ProcessRequests();

            Vector2 surfaceFlow;
            GPUReadbackFlow.Instance.SampleFlow(ref position, out surfaceFlow, _boatWidth);
            waterSurfaceVel += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);
        }

        // I could filter the surface vel as the min of the last 2 frames. theres a hard case where a wavelength is turned on/off
        // which generates single frame vel spikes - because the surface legitimately moves very fast. 

        if (_debugDraw)
        {
            Debug.DrawLine(transform.position + 5f * Vector3.up, transform.position + 5f * Vector3.up + waterSurfaceVel,
                new Color(1, 1, 1, 0.6f));
        }

        _velocityRelativeToWater = _rb.velocity - waterSurfaceVel;

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

        FixedUpdateOrientation(collProvider, undispPos);
    }

    /// <summary>
    /// Align to water normal. One normal by default, but can use a separate normal based on boat length vs width. This gives
    /// varying rotations based on boat dimensions.
    /// </summary>
    void FixedUpdateOrientation(ICollProvider collProvider, Vector3 undisplacedPos)
    {
        Vector3 normal, normalLongitudinal;
        if (!collProvider.SampleNormal(ref undisplacedPos, out normal, _boatWidth))
        {
            normal = Vector3.up;
        }

        if (_useBoatLength && collProvider.SampleNormal(ref undisplacedPos, out normalLongitudinal, _boatLength))
        {
            var F = transform.forward;
            F.y = 0f;
            F.Normalize();
            normal -= Vector3.Dot(F, normal) * F;

            var R = transform.right;
            R.y = 0f;
            R.Normalize();
            normalLongitudinal -= Vector3.Dot(R, normalLongitudinal) * R;
        }
        else
        {
            normalLongitudinal = Vector3.up;
        }

        if (_debugDraw) Debug.DrawLine(transform.position, transform.position + 5f * normal, Color.green);
        if (_debugDraw && _useBoatLength) Debug.DrawLine(transform.position, transform.position + 5f * normalLongitudinal, Color.green);

        var torqueWidth = Vector3.Cross(transform.up, normal);
        _rb.AddTorque(torqueWidth * _boyancyTorque, ForceMode.Acceleration);
        if (_useBoatLength)
        {
            var torqueLength = Vector3.Cross(transform.up, normalLongitudinal);
            _rb.AddTorque(torqueLength * _boyancyTorque, ForceMode.Acceleration);
        }
    }

#if UNITY_EDITOR
    //private void Update()
    //{
    //    UpdateDebugDrawSurroundingColl();
    //}

    private void UpdateDebugDrawSurroundingColl()
    {
        float r = 5f;
        float steps = 10;
        for (float i = 0; i < steps; i++)
        {
            for (float j = 0; j < steps; j++)
            {
                Vector3 pos = new Vector3(((i + 0.5f) - steps / 2f) * r, 0f, ((j + 0.5f) - steps / 2f) * r);
                pos.x += transform.position.x;
                pos.z += transform.position.z;

                Vector3 disp;
                if (OceanRenderer.Instance.CollisionProvider.SampleDisplacement(ref pos, out disp, _boatWidth))
                {
                    DebugDrawCross(pos + disp, 1f, Color.green);
                }
                else
                {
                    DebugDrawCross(pos, 0.25f, Color.red);
                }
            }
        }
    }
#endif

    void DebugDrawCross(Vector3 pos, float r, Color col)
    {
        Debug.DrawLine(pos - Vector3.up * r, pos + Vector3.up * r, col);
        Debug.DrawLine(pos - Vector3.right * r, pos + Vector3.right * r, col);
        Debug.DrawLine(pos - Vector3.forward * r, pos + Vector3.forward * r, col);
    }
}
