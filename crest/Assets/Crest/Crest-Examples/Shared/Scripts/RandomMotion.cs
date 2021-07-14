// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

/// <summary>
/// Shoves the gameobject around random amounts, occasionally useful for debugging where some motion is required to reproduce an issue.
/// </summary>
public class RandomMotion : MonoBehaviour
{
    /// <summary>
    /// The version of this asset. Can be used to migrate across versions. This value should
    /// only be changed when the editor upgrades the version.
    /// </summary>
    [SerializeField, HideInInspector]
#pragma warning disable 414
    int _version = 0;
#pragma warning restore 414

    [Header("Translation")]
    public Vector3 _axis = Vector3.up;
    Vector3 _orthoAxis;
    [Range(0, 15)]
    public float _amplitude = 1f;
    [Range(0, 5)]
    public float _freq = 1f;

    [Range(0, 1)]
    public float _orthogonalMotion = 0f;

    [Header("Rotation")]
    [Range(0, 5)]
    public float _rotationFreq = 1f;
    public float _rotationVel = 0f;

    Vector3 _origin;

    void Start()
    {
        _origin = transform.position;

        _orthoAxis = Quaternion.AngleAxis(90f, Vector3.up) * _axis;
    }

    void Update()
    {
        // Translation
        {
            // Do circles in perlin noise
            float rnd = 2f * (Mathf.PerlinNoise(0.5f + 0.5f * Mathf.Cos(_freq * Time.time), 0.5f + 0.5f * Mathf.Sin(_freq * Time.time)) - 0.5f);

            float orthoPhaseOff = Mathf.PI / 2f;
            float rndOrtho = 2f * (Mathf.PerlinNoise(0.5f + 0.5f * Mathf.Cos(_freq * Time.time + orthoPhaseOff), 0.5f + 0.5f * Mathf.Sin(_freq * Time.time + orthoPhaseOff)) - 0.5f);

            transform.position = _origin + (_axis * rnd + _orthoAxis * rndOrtho * _orthogonalMotion) * _amplitude;
        }

        // Rotation
        {
            var f1 = Mathf.Sin(Time.time * _rotationFreq * 1.0f);
            var f2 = Mathf.Sin(Time.time * _rotationFreq * 0.83f);
            var f3 = Mathf.Sin(Time.time * _rotationFreq * 1.14f);
            transform.rotation *= Quaternion.Euler(
                f1 * _rotationVel * Time.deltaTime,
                f2 * _rotationVel * Time.deltaTime,
                f3 * _rotationVel * Time.deltaTime);
        }
    }
}
