// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

#if CREST_UNITY_INPUT && ENABLE_INPUT_SYSTEM
#define INPUT_SYSTEM_ENABLED
#endif

using UnityEngine;

namespace Crest.Examples
{
    sealed class InputModulePatcher : MonoBehaviour
    {
#if INPUT_SYSTEM_ENABLED
        void OnEnable()
        {
            GetComponent<UnityEngine.EventSystems.StandaloneInputModule>().enabled = false;
        }
#endif
    }
}
