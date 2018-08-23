// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimShorelineFoam", menuName = "Crest/Sim/Shoreline Foam", order = 10000)]
    public class SimShorelineFoam : SimBase
    {
        [Range(0f, 20f), Tooltip("Speed at which foam fades/dissipates.")]
        public float _foamFadeRate = 0.8f;
        public float _shorelineFoamMaxDepth = 0.65f;
        [Range(0f, 5f), Tooltip("Scales intensity of foam generated in shallow water.")]
        public float _shorelineFoamStrength = 2f;
    }
}
