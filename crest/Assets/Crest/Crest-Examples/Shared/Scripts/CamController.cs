// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.XR;

public class CamController : MonoBehaviour
{
    public float linSpeed = 10f;
    public float rotSpeed = 70f;

    public bool simForwardInput = false;
    public bool _requireLMBToMove = false;

    Vector2 _lastMousePos = -Vector2.one;
    bool _dragging = false;

    public float _fixedDt = 1 / 60f;

    Transform _targetTransform;

    void Awake()
    {
        _targetTransform = transform;

        // We cannot change the Camera's transform when XR is enabled.
        if (XRSettings.enabled)
        {
            // Disable XR temporarily so we can change the transform of the camera.
            XRSettings.enabled = false;
            // The VR camera is moved in local space, so we can move the camera if we move its parent we create instead.
            var parent = new GameObject("VRCameraOffset");
            parent.transform.parent = _targetTransform.parent;
            // Copy the transform over to the parent.
            parent.transform.position = _targetTransform.position;
            parent.transform.rotation = _targetTransform.rotation;
            // Parent camera to offset and reset transform. Scale changes slightly in editor so we will reset that too.
            _targetTransform.parent = parent.transform;
            _targetTransform.localPosition = Vector3.zero;
            _targetTransform.localRotation = Quaternion.identity;
            _targetTransform.localScale = Vector3.one;
            // We want to manipulate this transform.
            _targetTransform = parent.transform;
            XRSettings.enabled = true;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (_fixedDt > 0f)
            dt = _fixedDt;

        UpdateMovement(dt);

        // These aren't useful and can break for XR hardware.
        if (!XRSettings.enabled || XRSettings.loadedDeviceName == "MockHMD")
        {
            UpdateDragging(dt);
            UpdateKillRoll();
        }
    }

    void UpdateMovement(float dt)
    {
        if (!Input.GetMouseButton(0) && _requireLMBToMove) return;

        float forward = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
        if (simForwardInput)
        {
            forward = 1f;
        }

        _targetTransform.position += linSpeed * _targetTransform.forward * forward * dt;
        var speed = linSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed *= 3f;
        }

        _targetTransform.position += speed * _targetTransform.forward * forward * dt;
        //_transform.position += linSpeed * _transform.right * Input.GetAxis( "Horizontal" ) * dt;
        _targetTransform.position += linSpeed * _targetTransform.up * (Input.GetKey(KeyCode.E) ? 1 : 0) * dt;
        _targetTransform.position -= linSpeed * _targetTransform.up * (Input.GetKey(KeyCode.Q) ? 1 : 0) * dt;
        _targetTransform.position -= linSpeed * _targetTransform.right * (Input.GetKey(KeyCode.A) ? 1 : 0) * dt;
        _targetTransform.position += linSpeed * _targetTransform.right * (Input.GetKey(KeyCode.D) ? 1 : 0) * dt;
        _targetTransform.position += speed * _targetTransform.up * (Input.GetKey(KeyCode.E) ? 1 : 0) * dt;
        _targetTransform.position -= speed * _targetTransform.up * (Input.GetKey(KeyCode.Q) ? 1 : 0) * dt;
        _targetTransform.position -= speed * _targetTransform.right * (Input.GetKey(KeyCode.A) ? 1 : 0) * dt;
        _targetTransform.position += speed * _targetTransform.right * (Input.GetKey(KeyCode.D) ? 1 : 0) * dt;
    }

    void UpdateDragging(float dt)
    {
        Vector2 mousePos;
        mousePos.x = Input.mousePosition.x;
        mousePos.y = Input.mousePosition.y;

        if( !_dragging && Input.GetMouseButtonDown( 0 ) && !Crest.OceanDebugGUI.OverGUI( mousePos ) )
        {
            _dragging = true;
            _lastMousePos = mousePos;
        }
        if( _dragging && Input.GetMouseButtonUp( 0 ) )
        {
            _dragging = false;
            _lastMousePos = -Vector2.one;
        }

        if( _dragging )
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
        Vector3 ea = _targetTransform.eulerAngles;
        ea.z = 0f;
        transform.eulerAngles = ea;
    }
}
