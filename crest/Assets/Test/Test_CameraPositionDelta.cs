using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_CameraPositionDelta : MonoBehaviour
{
    Vector3 m_OldPosition;

    void Awake()
    {
        m_OldPosition = transform.position;
    }

    void Start()
    {

    }

    void LateUpdate()
    {
        Shader.SetGlobalVector("_CameraPositionDelta", m_OldPosition - transform.position);
        // Debug.Log($"m_OldPosition - transform.position {m_OldPosition - transform.position}");
        m_OldPosition = transform.position;
    }
}
