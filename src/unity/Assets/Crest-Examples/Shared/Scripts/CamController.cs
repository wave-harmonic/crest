// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

public class CamController : MonoBehaviour
{
    public float linSpeed = 10f;
    public float rotSpeed = 70f;

    public bool simForwardInput = false;
    public bool _requireLMBToMove = false;

    Vector2 _lastMousePos = -Vector2.one;
    bool _dragging = false;

    public float _fixedDt = 1 / 60f;

    void Update()
    {
        float dt = Time.deltaTime;
        if (_fixedDt > 0f)
            dt = _fixedDt;

        UpdateMovement(dt);

        UpdateDragging(dt);

        UpdateKillRoll();
    }

    void UpdateMovement(float dt)
    {
        if (!Input.GetMouseButton(0) && _requireLMBToMove)
            return;

        float forward = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
        if (simForwardInput)
            forward = 1f;

        transform.position += linSpeed * transform.forward * forward * dt;
        //transform.position += linSpeed * transform.right * Input.GetAxis( "Horizontal" ) * dt;
        transform.position += linSpeed * transform.up * (Input.GetKey(KeyCode.E) ? 1 : 0) * dt;
        transform.position -= linSpeed * transform.up * (Input.GetKey(KeyCode.Q) ? 1 : 0) * dt;
        transform.position -= linSpeed * transform.right * (Input.GetKey(KeyCode.A) ? 1 : 0) * dt;
        transform.position += linSpeed * transform.right * (Input.GetKey(KeyCode.D) ? 1 : 0) * dt;

        transform.rotation = transform.rotation * Quaternion.AngleAxis(rotSpeed * (Input.GetKey(KeyCode.LeftArrow) ? -1 : 0) * dt, Vector3.up);
        transform.rotation = transform.rotation * Quaternion.AngleAxis(rotSpeed * (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) * dt, Vector3.up);
    }

    void UpdateDragging(float dt)
    {
        Vector2 mousePos;
        mousePos.x = Input.mousePosition.x;
        mousePos.y = Input.mousePosition.y;

        if( !_dragging && Input.GetMouseButtonDown( 0 ) && !OceanDebugGUI.OverGUI( mousePos ) )
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

            Vector3 ea = transform.eulerAngles;
            ea.x += -0.1f * rotSpeed * delta.y * dt;
            ea.y += 0.1f * rotSpeed * delta.x * dt;
            transform.eulerAngles = ea;

            _lastMousePos = mousePos;
        }
    }

    void UpdateKillRoll()
    {
        Vector3 ea = transform.eulerAngles;
        ea.z = 0f;
        transform.eulerAngles = ea;
    }
}
