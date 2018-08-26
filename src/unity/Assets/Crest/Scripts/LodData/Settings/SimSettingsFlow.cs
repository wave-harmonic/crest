// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsFlow", menuName = "Crest/Flow Sim Settings", order = 10000)]
    public class SimSettingsFlow : SimSettingsBase
    {
        [Range(0f, 500f), Tooltip("Speed of the flow")]
        public float _flowSpeed = 100.0f;
    }
}
