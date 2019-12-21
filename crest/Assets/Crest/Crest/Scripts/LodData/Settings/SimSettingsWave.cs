// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsDynamicWaves", menuName = "Crest/Dynamic Wave Sim Settings", order = 10000)]
    public class SimSettingsWave : SimSettingsBase
    {
        //[Header("Range")]
        [Range(0f, 32f), Tooltip("NOT CURRENTLY WORKING. The wave sim will not run if the simulation grid is smaller in resolution than this size. Useful to limit sim range for performance."),
            HideInInspector]
        public float _minGridSize = 0f;
        [Range(0f, 32f), Tooltip("NOT CURRENTLY WORKING. The wave sim will not run if the simulation grid is bigger in resolution than this size. Zero means no constraint/unlimited resolutions. Useful to limit sim range for performance."),
            HideInInspector]
        public float _maxGridSize = 0f;

        [Header("Stability")]
        [Range(0f, 1f), Tooltip("How much energy is dissipated each frame. Helps sim stability, but limits how far ripples will propagate. Set this as large as possible/acceptable.")]
        public float _damping = 0.25f;
        [Range(0.1f, 3f), Tooltip("Stability measurement. Lower values means more stable sim, at the cost of more computation. This value should be set as large as possible until sim instabilities/flickering begin to appear.")]
        public float _courantNumber = 1f;
        [Range(1, 8), Tooltip("How many simulation substeps are allowed per frame. Run at target framerate with the OceanDebugGUI visible to see how many substeps are being done when the camera is close to the water, and set the limit to this value. If the max substeps is set lower than this value, the detailed/high frequency waves will propagate slower than they would in reality. For many applications this may not be an issue.")]
        public int _maxSimStepsPerFrame = 3;

        [Header("Displacement Generation")]
        [Range(0f, 20f), Tooltip("Induce horizontal displacements to sharpen simulated waves.")]
        public float _horizDisplace = 3f;
        [Range(0f, 1f), Tooltip("Clamp displacement to help prevent self-intersection in steep waves. Zero means unclamped.")]
        public float _displaceClamp = 0.3f;

        [Range(0f, 64f), Tooltip("Multiplier for gravity. More gravity means dynamic waves will travel faster.")]
        public float _gravityMultiplier = 1f;
    }
}
