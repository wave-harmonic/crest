// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

public class RandomMotion : MonoBehaviour
{
    public Vector3 _axis = Vector3.up;
    [Range( 0, 5 )]
    public float _amplitude = 1f;
    [Range( 0, 5 )]
    public float _freq = 1f;

    Vector3 _origin;

    void Start()
    {
        _origin = transform.position;
    }

    void Update()
    {
        // do circles in perlin noise
        float rnd = 2f * (Mathf.PerlinNoise( 0.5f + 0.5f * Mathf.Cos( _freq * Time.time ), 0.5f + 0.5f * Mathf.Sin( _freq * Time.time ) ) - 0.5f);
        transform.position = _origin + _axis * _amplitude * rnd;
    }
}
