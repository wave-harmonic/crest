// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsDynamicWaves", menuName = "Crest/Dynamic Wave Sim Settings", order = 10000)]
    public class SimSettingsWave : SimSettingsBase
    {
        [Header("Simulation Settings")]
        [Range(0f,32f), Tooltip("The wave sim will not run if the simulation grid is smaller in resolution than this size. Useful to limit sim range for performance.")]
        public float _minGridSize = 0f;
        [Range(0f, 32f), Tooltip("The wave sim will not run if the simulation grid is bigger in resolution than this size. Zero means no constraint/unlimited resolutions. Useful to limit sim range for performance.")]
        public float _maxGridSize = 0f;
        [Tooltip("Max dt used for simulation.")]
        public float _maxSubstepDt = 1f / 60;

        [Range(0f, 1f), Tooltip("How much energy is dissipated each frame.")]
        public float _damping = 0.173f;

        [Header("Displacement Generation")]
        [Range(0f, 20f), Tooltip("Induce horizontal displacements to sharpen simulated waves.")]
        public float _horizDisplace = 10f;
        [Range(0f, 1f), Tooltip("Clamp displacement to help prevent self-intersection in steep waves. Zero means unclamped.")]
        public float _displaceClamp = 0.3f;
    }
}
