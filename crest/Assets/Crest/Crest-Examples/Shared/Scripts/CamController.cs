// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if ENABLE_VR && ENABLE_VR_MODULE
using UnityEngine.XR;
#endif

/// <summary>
/// A simple and dumb camera script that can be controlled using WASD and the mouse.
/// </summary>
public class CamController : MonoBehaviour
{
    /// <summary>
    /// The version of this asset. Can be used to migrate across versions. This value should
    /// only be changed when the editor upgrades the version.
    /// </summary>
    [SerializeField, HideInInspector]
#pragma warning disable 414
    int _version = 0;
#pragma warning restore 414

    public float linSpeed = 10f;
    public float rotSpeed = 70f;

    public bool simForwardInput = false;
    public bool _requireLMBToMove = false;

    Vector2 _lastMousePos = -Vector2.one;
    bool _dragging = false;

    public float _fixedDt = 1 / 60f;

    Transform _targetTransform;

#pragma warning disable CS0108
    // In editor we need to use "new" to suppress warning but then gives warning when building so use pragma instead.
    Camera camera;
#pragma warning restore CS0108

    [Space(10)]

    [SerializeField]
    DebugFields _debug = new DebugFields();

    [System.Serializable]
    class DebugFields
    {
        [Tooltip("Allows the camera to roll (rotating on the z axis).")]
        public bool _enableCameraRoll = false;

        [Tooltip("Disables the XR occlusion mesh for debugging purposes. Only works with legacy XR.")]
        public bool _disableOcclusionMesh = false;

        [Tooltip("Sets the XR occlusion mesh scale. Useful for debugging refractions. Only works with legacy XR."), UnityEngine.Range(1f, 2f)]
        public float _occlusionMeshScale = 1f;
    }

    void Awake()
    {
        _targetTransform = transform;

        camera = GetComponent<Camera>();
        if (camera == null)
        {
            enabled = false;
            return;
        }

#if ENABLE_VR && ENABLE_VR_MODULE
        if (XRSettings.enabled)
        {
            // Seems like the best place to put this for now. Most XR debugging happens using this component.
            // @FixMe: useOcclusionMesh doesn't work anymore. Might be a Unity bug.
            XRSettings.useOcclusionMesh = !_debug._disableOcclusionMesh;
            XRSettings.occlusionMaskScale = _debug._occlusionMeshScale;
        }
#endif
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (_fixedDt > 0f)
            dt = _fixedDt;

        UpdateMovement(dt);

#if ENABLE_VR && ENABLE_VR_MODULE
        // These aren't useful and can break for XR hardware.
        if (!XRSettings.enabled || XRSettings.loadedDeviceName.Contains("MockHMD"))
#endif
        {
            UpdateDragging(dt);
            UpdateKillRoll();
        }

#if ENABLE_VR && ENABLE_VR_MODULE
        if (XRSettings.enabled)
        {
            // Check if property has changed.
            if (XRSettings.useOcclusionMesh == _debug._disableOcclusionMesh)
            {
                // @FixMe: useOcclusionMesh doesn't work anymore. Might be a Unity bug.
                XRSettings.useOcclusionMesh = !_debug._disableOcclusionMesh;
            }

            XRSettings.occlusionMaskScale = _debug._occlusionMeshScale;
        }
#endif
    }

    void UpdateMovement(float dt)
    {
        // New input system works even when game view is not focused.
        if (!Application.isFocused)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        if (!Mouse.current.leftButton.isPressed && _requireLMBToMove) return;
        float forward = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);
#else
        if (!Input.GetMouseButton(0) && _requireLMBToMove) return;
        float forward = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
#endif
        if (simForwardInput)
        {
            forward = 1f;
        }

        _targetTransform.position += linSpeed * _targetTransform.forward * forward * dt;
        var speed = linSpeed;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current.leftShiftKey.isPressed)
#else
        if (Input.GetKey(KeyCode.LeftShift))
#endif
        {
            speed *= 3f;
        }

        _targetTransform.position += speed * _targetTransform.forward * forward * dt;
        //_transform.position += linSpeed * _transform.right * Input.GetAxis( "Horizontal" ) * dt;
#if ENABLE_INPUT_SYSTEM
        _targetTransform.position += linSpeed * _targetTransform.up * (Keyboard.current.eKey.isPressed ? 1 : 0) * dt;
        _targetTransform.position -= linSpeed * _targetTransform.up * (Keyboard.current.qKey.isPressed ? 1 : 0) * dt;
        _targetTransform.position -= linSpeed * _targetTransform.right * (Keyboard.current.aKey.isPressed ? 1 : 0) * dt;
        _targetTransform.position += linSpeed * _targetTransform.right * (Keyboard.current.dKey.isPressed ? 1 : 0) * dt;
        _targetTransform.position += speed * _targetTransform.up * (Keyboard.current.eKey.isPressed ? 1 : 0) * dt;
        _targetTransform.position -= speed * _targetTransform.up * (Keyboard.current.qKey.isPressed ? 1 : 0) * dt;
        _targetTransform.position -= speed * _targetTransform.right * (Keyboard.current.aKey.isPressed ? 1 : 0) * dt;
        _targetTransform.position += speed * _targetTransform.right * (Keyboard.current.dKey.isPressed ? 1 : 0) * dt;
#else
        _targetTransform.position += linSpeed * _targetTransform.up * (Input.GetKey(KeyCode.E) ? 1 : 0) * dt;
        _targetTransform.position -= linSpeed * _targetTransform.up * (Input.GetKey(KeyCode.Q) ? 1 : 0) * dt;
        _targetTransform.position -= linSpeed * _targetTransform.right * (Input.GetKey(KeyCode.A) ? 1 : 0) * dt;
        _targetTransform.position += linSpeed * _targetTransform.right * (Input.GetKey(KeyCode.D) ? 1 : 0) * dt;
        _targetTransform.position += speed * _targetTransform.up * (Input.GetKey(KeyCode.E) ? 1 : 0) * dt;
        _targetTransform.position -= speed * _targetTransform.up * (Input.GetKey(KeyCode.Q) ? 1 : 0) * dt;
        _targetTransform.position -= speed * _targetTransform.right * (Input.GetKey(KeyCode.A) ? 1 : 0) * dt;
        _targetTransform.position += speed * _targetTransform.right * (Input.GetKey(KeyCode.D) ? 1 : 0) * dt;
#endif
        {
            float rotate = 0f;
#if ENABLE_INPUT_SYSTEM
            rotate += (Keyboard.current.rightArrowKey.isPressed ? 1 : 0);
            rotate -= (Keyboard.current.leftArrowKey.isPressed ? 1 : 0);
#else
            rotate += (Input.GetKey(KeyCode.RightArrow) ? 1 : 0);
            rotate -= (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
#endif

            rotate *= 5f;
            Vector3 ea = _targetTransform.eulerAngles;
            ea.y += 0.1f * rotSpeed * rotate * dt;
            _targetTransform.eulerAngles = ea;
        }
    }

    void UpdateDragging(float dt)
    {
        // New input system works even when game view is not focused.
        if (!Application.isFocused)
        {
            return;
        }

        Vector2 mousePos =
#if ENABLE_INPUT_SYSTEM
            Mouse.current.position.ReadValue();
#else
            Input.mousePosition;
#endif

        var wasLeftMouseButtonPressed =
#if ENABLE_INPUT_SYSTEM
            Mouse.current.leftButton.wasPressedThisFrame;
#else
            Input.GetMouseButtonDown(0);
#endif

        if (!_dragging && wasLeftMouseButtonPressed && camera.rect.Contains(camera.ScreenToViewportPoint(mousePos)) &&
            !Crest.OceanDebugGUI.OverGUI(mousePos))
        {
            _dragging = true;
            _lastMousePos = mousePos;
        }
#if ENABLE_INPUT_SYSTEM
        if (_dragging && Mouse.current.leftButton.wasReleasedThisFrame)
#else
        if (_dragging && Input.GetMouseButtonUp(0))
#endif
        {
            _dragging = false;
            _lastMousePos = -Vector2.one;
        }

        if (_dragging)
        {
            Vector2 delta = mousePos - _lastMousePos;

            Vector3 ea = _targetTransform.eulerAngles;
            ea.x += -0.1f * rotSpeed * delta.y * dt;
            ea.y += 0.1f * rotSpeed * delta.x * dt;
            _targetTransform.eulerAngles = ea;

            _lastMousePos = mousePos;
        }
    }

    void UpdateKillRoll()
    {
        if (_debug._enableCameraRoll) return;
        Vector3 ea = _targetTransform.eulerAngles;
        ea.z = 0f;
        transform.eulerAngles = ea;
    }
}
