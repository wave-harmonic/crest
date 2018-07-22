// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

[CreateAssetMenu(fileName = "SimSettingsFoam", menuName = "Crest/Foam Sim Settings", order = 10000)]
public class SimSettingsFoam : SimSettingsBase
{
    [Range(0f, 5f)]
    public float _foamFadeRate = 0.8f;
    [Range(0f, 5f)]
    public float _waveFoamStrength = 1.25f;
    [Range(0f, 1f)]
    public float _waveFoamCoverage = 0.8f;
    [Range(0f, 3f)]
    public float _shorelineFoamMaxDepth = 0.65f;
    [Range(0f, 1f)]
    public float _shorelineFoamStrength = 0.313f;
}
