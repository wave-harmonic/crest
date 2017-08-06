// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class CamController : MonoBehaviour
    {
        public float linSpeed = 10f;

        void Update()
        {
            transform.position += linSpeed * transform.forward * Input.GetAxis( "Vertical" ) * Time.deltaTime;
            transform.position += linSpeed * transform.up * (Input.GetKey( KeyCode.E ) ? 1 : 0) * Time.deltaTime;
            transform.position -= linSpeed * transform.up * (Input.GetKey( KeyCode.Q ) ? 1 : 0) * Time.deltaTime;

            float rotspeed = 50.0f;
            transform.rotation = transform.rotation * Quaternion.AngleAxis( rotspeed * Input.GetAxis( "Horizontal" ) * Time.deltaTime, Vector3.up );
        }
    }
}
