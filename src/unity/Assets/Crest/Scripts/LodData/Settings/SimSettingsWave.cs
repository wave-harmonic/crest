// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsWaves", menuName = "Crest/Wave Sim Settings", order = 10000)]
    public class SimSettingsWave : SimSettingsBase
    {
        [Range(0f,32f), Tooltip("The wave sim will not run if the simulation grid is smaller in resolution than this size. Useful to limit sim range for performance.")]
        public float _minGridSize = 0f;
        [Range(0f, 32f), Tooltip("The wave sim will not run if the simulation grid is bigger in resolution than this size. Zero means no constraint/unlimited resolutions. Useful to limit sim range for performance.")]
        public float _maxGridSize = 0f;

        [Range(0f, 1f), Tooltip("How much energy is dissipated each frame.")]
        public float _damping = 0.173f;

        [Header("Displacement Generation")]
        [Range(0f, 3f), Tooltip("Induce horizontal displacements to sharpen simulated waves.")]
        public float _horizDisplace = 1.5f;
        [Range(0f, 1f), Tooltip("Clamp displacement to help prevent self-intersection in steep waves. Zero means unclamped.")]
        public float _displaceClamp = 0.3f;
    }
}
