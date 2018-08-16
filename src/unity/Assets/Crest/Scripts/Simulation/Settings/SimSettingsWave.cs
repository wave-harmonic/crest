// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsWaves", menuName = "Crest/Wave Sim Settings", order = 10000)]
    public class SimSettingsWave : SimSettingsBase
    {
        [Range(0f, 1f)]
        public float _damping = 0.173f;

        [Header("Displacement Generation")]
        [Range(0f, 3f), Tooltip("Induce horizontal displacements to sharpen simulated waves.")]
        public float _horizDisplace = 1.5f;
        [Range(0f, 1f), Tooltip("Clamp displacement to help prevent self-intersection in steep waves. Zero means unclamped.")]
        public float _displaceClamp = 0.3f;
    }
}
