// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsShadow", menuName = "Crest/Shadow Sim Settings", order = 10000)]
    public class SimSettingsShadow : SimSettingsBase
    {
        [Range(0f, 32f), Tooltip("Jitter diameter, controls softness.")]
        public float _jitterDiameter = 15f;

        [Range(0f, 1f), Tooltip("Current frame weight for accumulation over frames. Roughly means 'responsiveness'.")]
        public float _currentFrameWeight = 0.02f;
    }
}
