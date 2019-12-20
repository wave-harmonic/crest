// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;

/// <summary>
/// Simple type of buoyancy - takes one sample and matches boat height and orientation to water height and normal.
/// </summary>
public class BoatAlignNormal : FloatingObjectBase
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
    public override float ObjectWidth { get { return _boatWidth; } }

    [SerializeField, Tooltip("Computes a separate normal based on boat length to get more accurate orientations, at the cost of an extra collision sample.")]
    bool _useBoatLength = false;
    [Tooltip("Length dimension of boat. Only used if Use Boat Length is enabled."), SerializeField]
    float _boatLength = 3f;

    [Header("Drag")]
    [SerializeField] float _dragInWaterUp = 3f;
    [SerializeField] float _dragInWaterRight = 2f;
    [SerializeField] float _dragInWaterForward = 1f;

    [Header("Controls")]
    [SerializeField]
    bool _playerControlled = true;
    [Tooltip("Used to automatically add throttle input"), SerializeField]
    float _throttleBias = 0f;
    [Tooltip("Used to automatically add turning input"), SerializeField]
    float _steerBias = 0f;

    [Header("Debug")]
    [SerializeField]
    bool _debugDraw = false;

    bool _inWater;
    public override bool InWater { get { return _inWater; } }

    Vector3 _displacementToObject = Vector3.zero;
    public override Vector3 CalculateDisplacementToObject() { return _displacementToObject; }

    public override Vector3 Velocity => _rb.velocity;

    Rigidbody _rb;

    SamplingData _samplingDataFlow = new SamplingData();

    SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();
    SampleHeightHelper _sampleHeightHelperLengthwise = new SampleHeightHelper();
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
        if (OceanRenderer.Instance == null)
        {
            return;
        }

        UnityEngine.Profiling.Profiler.BeginSample("BoatAlignNormal.FixedUpdate");

        // Trigger processing of displacement textures that have come back this frame. This will be processed
        // anyway in Update(), but FixedUpdate() is earlier so make sure it's up to date now.
        if (OceanRenderer.Instance._simSettingsAnimatedWaves.CollisionSource == SimSettingsAnimatedWaves.CollisionSources.OceanDisplacementTexturesGPU && GPUReadbackDisps.Instance)
        {
            GPUReadbackDisps.Instance.ProcessRequests();
        }

        var collProvider = OceanRenderer.Instance.CollisionProvider;
        var position = transform.position;

        _sampleHeightHelper.Init(transform.position, _boatWidth);
        var height = OceanRenderer.Instance.SeaLevel;
        var normal = Vector3.up;
        var waterSurfaceVel = Vector3.zero;

        _sampleHeightHelper.Sample(ref _displacementToObject, ref normal, ref waterSurfaceVel);

        // height = base sea level + surface displacement y
        height += _displacementToObject.y;

        if (QueryFlow.Instance)
        {
            _sampleFlowHelper.Init(transform.position, _boatWidth);

            Vector2 surfaceFlow = Vector2.zero;
            _sampleFlowHelper.Sample(ref surfaceFlow);
            waterSurfaceVel += new Vector3(surfaceFlow.x, 0, surfaceFlow.y);
        }

        // I could filter the surface vel as the min of the last 2 frames. theres a hard case where a wavelength is turned on/off
        // which generates single frame vel spikes - because the surface legitimately moves very fast.

        if (_debugDraw)
        {
            Debug.DrawLine(transform.position + 5f * Vector3.up, transform.position + 5f * Vector3.up + waterSurfaceVel,
                new Color(1, 1, 1, 0.6f));
        }

        var velocityRelativeToWater = _rb.velocity - waterSurfaceVel;

        float bottomDepth = height - transform.position.y - _bottomH;

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

        float forward = _throttleBias;
        float rawForward = Input.GetAxis("Vertical");
        if (_playerControlled) forward += rawForward;
        _rb.AddForceAtPosition(transform.forward * _enginePower * forward, forcePosition, ForceMode.Acceleration);

        float reverseMultiplier = (rawForward < 0f ? -1f : 1f);
        float sideways = _steerBias;
        if (_playerControlled) sideways +=
                (Input.GetKey(KeyCode.A) ? reverseMultiplier * -1f : 0f) +
                (Input.GetKey(KeyCode.D) ? reverseMultiplier * 1f : 0f);
        _rb.AddTorque(transform.up * _turnPower * sideways, ForceMode.Acceleration);

        FixedUpdateOrientation(collProvider, normal);

        UnityEngine.Profiling.Profiler.EndSample();
    }

    /// <summary>
    /// Align to water normal. One normal by default, but can use a separate normal based on boat length vs width. This gives
    /// varying rotations based on boat dimensions.
    /// </summary>
    void FixedUpdateOrientation(ICollProvider collProvider, Vector3 normalSideways)
    {
        Vector3 normal = normalSideways, normalLongitudinal = Vector3.up;

        if (_useBoatLength)
        {
            _sampleHeightHelperLengthwise.Init(transform.position, _boatLength);
            var dummy = 0f;
            if (_sampleHeightHelperLengthwise.Sample(ref dummy, ref normalLongitudinal))
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
        }

        if (_debugDraw) Debug.DrawLine(transform.position, transform.position + 5f * normal, Color.green);
        if (_debugDraw && _useBoatLength) Debug.DrawLine(transform.position, transform.position + 5f * normalLongitudinal, Color.yellow);

        var torqueWidth = Vector3.Cross(transform.up, normal);
        _rb.AddTorque(torqueWidth * _boyancyTorque, ForceMode.Acceleration);
        if (_useBoatLength)
        {
            var torqueLength = Vector3.Cross(transform.up, normalLongitudinal);
            _rb.AddTorque(torqueLength * _boyancyTorque, ForceMode.Acceleration);
        }
    }
}
