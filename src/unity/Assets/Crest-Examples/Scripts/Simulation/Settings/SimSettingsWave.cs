// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

[CreateAssetMenu(fileName = "SimSettingsWaves", menuName = "Crest/Wave Sim Settings", order = 10000)]
public class SimSettingsWave : SimSettingsBase
{
    [Range(0f, 1f)]
    public float _damping = 0.173f;

    [Header("Foam Generation")]
    [Range(0f, 0.1f)]
    public float _foamMinAccel = 0f;
    [Range(0f, 0.1f)]
    public float _foamMaxAccel = 0.005f;
    [Range(0f, 5f)]
    public float _foamAmount = 0.5f;
}
