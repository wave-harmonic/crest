// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsWaves", menuName = "Crest/Wave Sim Settings", order = 10000)]
    public class SimSettingsWave : SimSettingsBase
    {
        [Range(0f, 1f)]
        public float _damping = 0.173f;

        [Header("Displacement Generation (Experimental)")]
        [Range(0f, 3f), Tooltip("Induce horizontal displacements to sharpen simulated waves.")]
        public float _horizDisplace = 0f;
        [Range(0f, 1f), Tooltip("Clamp displacement to help prevent self-intersection in steep waves. Zero means unclamped.")]
        public float _displaceClamp = 0.3f;

        [Header("Foam Generation")]
        [Range(0f, 0.1f), Tooltip("Minimum downward accel in sim that will generate foam.")]
        public float _foamMinAccel = 0f;
        [Range(0f, 0.1f), Tooltip("Downward accel for which maximum foam is generated.")]
        public float _foamMaxAccel = 0.005f;
        [Range(0f, 5f), Tooltip("Scales how much foam is generated.")]
        public float _foamAmount = 0.5f;
    }
}
