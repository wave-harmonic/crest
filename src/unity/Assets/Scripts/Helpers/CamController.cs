// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

public class CamController : MonoBehaviour
{
	public float linSpeed = 10f;
    public float rotSpeed = 70f;

    Vector2 _lastMousePos = -Vector2.one;

	void Update()
	{
		transform.position += linSpeed * transform.forward * Input.GetAxis("Vertical") * Time.deltaTime;
        //transform.position += linSpeed * transform.right * Input.GetAxis( "Horizontal" ) * Time.deltaTime;
        transform.position += linSpeed * transform.up * (Input.GetKey( KeyCode.E ) ? 1 : 0) * Time.deltaTime;
        transform.position -= linSpeed * transform.up * (Input.GetKey( KeyCode.Q ) ? 1 : 0) * Time.deltaTime;
        transform.position -= linSpeed * transform.right * (Input.GetKey( KeyCode.A ) ? 1 : 0) * Time.deltaTime;
        transform.position += linSpeed * transform.right * (Input.GetKey( KeyCode.D ) ? 1 : 0) * Time.deltaTime;

        transform.rotation = transform.rotation * Quaternion.AngleAxis( rotSpeed * (Input.GetKey( KeyCode.LeftArrow ) ? -1 : 0) * Time.deltaTime, Vector3.up );
        transform.rotation = transform.rotation * Quaternion.AngleAxis( rotSpeed * (Input.GetKey( KeyCode.RightArrow ) ? 1 : 0) * Time.deltaTime, Vector3.up );

        if( Input.GetMouseButton( 0 ) )
        {
            Vector2 mousePos;
            mousePos.x = Input.mousePosition.x;
            mousePos.y = Input.mousePosition.y;

            if( _lastMousePos != -Vector2.one )
            {
                Vector2 delta = mousePos - _lastMousePos;

                Vector3 ea = transform.eulerAngles;
                ea.x += -0.1f * rotSpeed * delta.y * Time.deltaTime;
                ea.y += 0.1f * rotSpeed * delta.x * Time.deltaTime;
                ea.z = 0f;
                transform.eulerAngles = ea;
            }

            if( _lastMousePos != -Vector2.one || !OceanResearch.OceanDebugGUI.Instance.OverGUI( mousePos ) )
            {
                _lastMousePos = mousePos;
            }
        }
        else
        {
            _lastMousePos = -Vector2.one;
        }
    }
}
