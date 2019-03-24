// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsShadow", menuName = "Crest/Shadow Sim Settings", order = 10000)]
    public class SimSettingsShadow : SimSettingsBase
    {
        [Range(0f, 32f), Tooltip("Jitter diameter for soft shadows, controls softness of this shadowing component.")]
        public float _jitterDiameterSoft = 15f;

        [Range(0f, 1f), Tooltip("Current frame weight for accumulation over frames for soft shadows. Roughly means 'responsiveness' for soft shadows.")]
        public float _currentFrameWeightSoft = 0.03f;

        [Range(0f, 32f), Tooltip("Jitter diameter for hard shadows, controls softness of this shadowing component.")]
        public float _jitterDiameterHard = 0.6f;

        [Range(0f, 1f), Tooltip("Current frame weight for accumulation over frames for hard shadows. Roughly means 'responsiveness' for hard shadows.")]
        public float _currentFrameWeightHard = 0.15f;

        [Tooltip("Whether to disable the null light warning, use this if you assign it dynamically and expect it to be null at points")]
        public bool _allowNullLight = false;
    }
}
