// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

public class RandomMotion : MonoBehaviour
{
    public Vector3 _axis = Vector3.up;
    Vector3 _orthoAxis;
    [Range( 0, 15 )]
    public float _amplitude = 1f;
    [Range( 0, 5 )]
    public float _freq = 1f;

    [Range(0, 1)]
    public float _orthogonalMotion = 0f;

    Vector3 _origin;

    void Start()
    {
        _origin = transform.position;

        _orthoAxis = Quaternion.AngleAxis(90f, Vector3.up) * _axis;
    }

    void Update()
    {
        // do circles in perlin noise
        float rnd = 2f * (Mathf.PerlinNoise(0.5f + 0.5f * Mathf.Cos(_freq * Time.time), 0.5f + 0.5f * Mathf.Sin(_freq * Time.time)) - 0.5f);

        float orthoPhaseOff = Mathf.PI / 2f;
        float rndOrtho = 2f * (Mathf.PerlinNoise(0.5f + 0.5f * Mathf.Cos(_freq * Time.time + orthoPhaseOff), 0.5f + 0.5f * Mathf.Sin(_freq * Time.time + orthoPhaseOff)) - 0.5f);

        transform.position = _origin + (_axis * rnd + _orthoAxis * rndOrtho * _orthogonalMotion) * _amplitude;
    }
}
