// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class CamController : MonoBehaviour
    {
    	public float linSpeed = 10f;
        public float rotSpeed = 70f;

        public bool simForwardInput = false;

        Vector2 _lastMousePos = -Vector2.one;
        bool _dragging = false;

        void Update()
        {
            float forward = Input.GetAxis("Vertical");
            if (simForwardInput)
                forward = 1f;

            transform.position += linSpeed * transform.forward * forward * Time.deltaTime;
            //transform.position += linSpeed * transform.right * Input.GetAxis( "Horizontal" ) * Time.deltaTime;
            transform.position += linSpeed * transform.up * (Input.GetKey( KeyCode.E ) ? 1 : 0) * Time.deltaTime;
            transform.position -= linSpeed * transform.up * (Input.GetKey( KeyCode.Q ) ? 1 : 0) * Time.deltaTime;
            transform.position -= linSpeed * transform.right * (Input.GetKey( KeyCode.A ) ? 1 : 0) * Time.deltaTime;
            transform.position += linSpeed * transform.right * (Input.GetKey( KeyCode.D ) ? 1 : 0) * Time.deltaTime;

            transform.rotation = transform.rotation * Quaternion.AngleAxis( rotSpeed * (Input.GetKey( KeyCode.LeftArrow ) ? -1 : 0) * Time.deltaTime, Vector3.up );
            transform.rotation = transform.rotation * Quaternion.AngleAxis( rotSpeed * (Input.GetKey( KeyCode.RightArrow ) ? 1 : 0) * Time.deltaTime, Vector3.up );

            UpdateDragging();

            UpdateKillRoll();
        }

        void UpdateDragging()
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
                ea.x += -0.1f * rotSpeed * delta.y * Time.deltaTime;
                ea.y += 0.1f * rotSpeed * delta.x * Time.deltaTime;
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
}
