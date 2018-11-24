// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using Crest;

// First experiment with object interacting with water. Assumes capsule is always touching water, does
// not take current water height into account yet
public class InteractCapsule : MonoBehaviour
{
    public Shader _shader;

    float _lastY = 0f;

    Material _mat;

    void Start()
    {
        _lastY = transform.position.y;

        _mat = new Material( _shader );

        GetComponent<Renderer>().material = _mat;
    }

    void Update()
    {
        float dy = _lastY - transform.position.y;

        float a = transform.lossyScale.x / 2f;
        float b = transform.lossyScale.z / 2f;
        float A = Mathf.PI * a * b;

        float V = dy * A;

        float VperLod = V / OceanRenderer.Instance.CurrentLodCount;

        _mat.SetFloat( "_displacedVPerLod", VperLod );

        _lastY = transform.position.y;
    }
}
