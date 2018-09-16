// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsShadow", menuName = "Crest/Shadow Sim Settings", order = 10000)]
    public class SimSettingsShadow : SimSettingsBase
    {
        [Range(0f, 32f), Tooltip("Jitter diameter soft, controls softness of soft shadowing.")]
        public float _jitterDiameterSoft = 15f;

        [Range(0f, 1f), Tooltip("Current frame weight for soft accumulation over frames. Roughly means 'responsiveness' for soft shadows.")]
        public float _currentFrameWeightSoft = 0.02f;

        [Range(0f, 32f), Tooltip("Jitter diameter sharp, controls softness of sharp shadowing.")]
        public float _jitterDiameterSharp = 0.6f;

        [Range(0f, 1f), Tooltip("Current frame weight for sharp accumulation over frames. Roughly means 'responsiveness' for sharp shadows.")]
        public float _currentFrameWeightSharp = 0.15f;
    }
}
