// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Simple type of buoyancy - takes one sample and matches boat height and orientation to water height and normal.
/// </summary>
[AddComponentMenu(Crest.Internal.Constants.MENU_PREFIX_EXAMPLE + "Boat Align Normal")]
public class BoatAlignNormal : FloatingObjectBase
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
    [Tooltip("Height offset from transform center to bottom of boat (if any).")]
    public float _bottomH = 0f;
    [Tooltip("Strength of buoyancy force per meter of submersion in water.")]
    public float _buoyancyCoeff = 1.5f;
    [Tooltip("Strength of torque applied to match boat orientation to water normal.")]
    public float _boyancyTorque = 8f;
    [Tooltip("Approximate hydrodynamics of 'surfing' down waves."), Crest.Range(0, 1)]
    public float _accelerateDownhill = 0f;

    [Header("Engine Power")]
    [Tooltip("Vertical offset for where engine force should be applied.")]
    public float _forceHeightOffset = -0.3f;
    public float _enginePower = 11f;
    public float _turnPower = 1.3f;

    [Header("Wave Response")]
    [Tooltip("Width dimension of boat. The larger this value, the more filtered/smooth the wave response will be.")]
    public float _boatWidth = 3f;
    public override float ObjectWidth => _boatWidth;

    [Tooltip("Computes a separate normal based on boat length to get more accurate orientations, at the cost of an extra collision sample.")]
    public bool _useBoatLength = false;
    [Tooltip("Length dimension of boat. Only used if Use Boat Length is enabled."), Predicated("_useBoatLength"), DecoratedField]
    public float _boatLength = 3f;

    [Header("Drag")]
    public float _dragInWaterUp = 3f;
    public float _dragInWaterRight = 2f;
    public float _dragInWaterForward = 1f;

    [Header("Controls")]
    public bool _playerControlled = true;
    [Tooltip("Used to automatically add throttle input")]
    public float _throttleBias = 0f;
    [Tooltip("Used to automatically add turning input")]
    public float _steerBias = 0f;

    [Header("Debug")]
    [SerializeField]
    bool _debugDraw = false;

    bool _inWater;
    public override bool InWater => _inWater;

    public override Vector3 Velocity => _rb.velocity;

    Rigidbody _rb;

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

        _sampleHeightHelper.Init(transform.position, _boatWidth, true);
        var height = OceanRenderer.Instance.SeaLevel;

        _sampleHeightHelper.Sample(out Vector3 disp, out var normal, out var waterSurfaceVel);

        // height = base sea level + surface displacement y
        height += disp.y;

        {
            _sampleFlowHelper.Init(transform.position, _boatWidth);

            _sampleFlowHelper.Sample(out var surfaceFlow);
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

        // Approximate hydrodynamics of sliding along water
        if (_accelerateDownhill > 0f)
        {
            _rb.AddForce(new Vector3(normal.x, 0f, normal.z) * -Physics.gravity.y * _accelerateDownhill, ForceMode.Acceleration);
        }

        // apply drag relative to water
        var forcePosition = _rb.position + _forceHeightOffset * Vector3.up;
        _rb.AddForceAtPosition(Vector3.up * Vector3.Dot(Vector3.up, -velocityRelativeToWater) * _dragInWaterUp, forcePosition, ForceMode.Acceleration);
        _rb.AddForceAtPosition(transform.right * Vector3.Dot(transform.right, -velocityRelativeToWater) * _dragInWaterRight, forcePosition, ForceMode.Acceleration);
        _rb.AddForceAtPosition(transform.forward * Vector3.Dot(transform.forward, -velocityRelativeToWater) * _dragInWaterForward, forcePosition, ForceMode.Acceleration);

        float forward = _throttleBias;
#if ENABLE_INPUT_SYSTEM
        float rawForward = !Application.isFocused ? 0 : ((Keyboard.current.wKey.isPressed ? 1 : 0) + (Keyboard.current.sKey.isPressed ? -1 : 0));
#else
        float rawForward = Input.GetAxis("Vertical");
#endif
        if (_playerControlled) forward += rawForward;
        _rb.AddForceAtPosition(transform.forward * _enginePower * forward, forcePosition, ForceMode.Acceleration);

        float reverseMultiplier = (rawForward < 0f ? -1f : 1f);
        float sideways = _steerBias;
        if (_playerControlled) sideways +=
#if ENABLE_INPUT_SYSTEM
                !Application.isFocused ? 0 :
                ((Keyboard.current.aKey.isPressed ? reverseMultiplier * -1f : 0f) +
                (Keyboard.current.dKey.isPressed ? reverseMultiplier * 1f : 0f));
#else
                (Input.GetKey(KeyCode.A) ? reverseMultiplier * -1f : 0f) +
                (Input.GetKey(KeyCode.D) ? reverseMultiplier * 1f : 0f);
#endif
        _rb.AddTorque(transform.up * _turnPower * sideways, ForceMode.Acceleration);

        FixedUpdateOrientation(normal);

        UnityEngine.Profiling.Profiler.EndSample();
    }

    /// <summary>
    /// Align to water normal. One normal by default, but can use a separate normal based on boat length vs width. This gives
    /// varying rotations based on boat dimensions.
    /// </summary>
    void FixedUpdateOrientation(Vector3 normalSideways)
    {
        Vector3 normal = normalSideways, normalLongitudinal = Vector3.up;

        if (_useBoatLength)
        {
            _sampleHeightHelperLengthwise.Init(transform.position, _boatLength, true);
            if (_sampleHeightHelperLengthwise.Sample(out _, out normalLongitudinal))
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
