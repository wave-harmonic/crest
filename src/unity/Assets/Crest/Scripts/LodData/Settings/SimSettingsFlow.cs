// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsFlow", menuName = "Crest/Flow Sim Settings", order = 10000)]
    public class SimSettingsFlow : SimSettingsBase
    {
        [Range(0f, 20f), Tooltip("Speed at which flow fades/dissipates.")]
        public float _flowFadeRate = 0.8f;
        [Range(0f, 5f), Tooltip("Scales intensity of flow generated from waves.")]
        public float _waveFlowStrength = 1f;
        [Range(0f, 1f), Tooltip("How much of the waves generate flow.")]
        public float _waveFlowCoverage = 0.8f;
        [Range(0f, 3f), Tooltip("Flow will be generated in water shallower than this depth.")]
        public float _shorelineFlowMaxDepth = 0.65f;
        [Range(0f, 5f), Tooltip("Scales intensity of flow generated in shallow water.")]
        public float _shorelineFlowStrength = 2f;
    }
}
