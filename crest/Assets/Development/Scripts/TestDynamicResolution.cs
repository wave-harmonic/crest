// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

#if CREST_SRP
using UnityEngine.Rendering;
#endif

namespace Crest
{
    public class TestDynamicResolution : MonoBehaviour
    {
        [SerializeField, Range(0.01f, 1f)] float _resolutionScale = 1f;

#if CREST_SRP
        float SetDynamicResolutionScale() => _resolutionScale;

        void OnEnable()
        {
            // Binds the dynamic resolution policy defined above.
            DynamicResolutionHandler.SetDynamicResScaler(SetDynamicResolutionScale,
                DynamicResScalePolicyType.ReturnsMinMaxLerpFactor);
        }
#else
        float _currentResolutionScale = 1f;

        void OnDisable()
        {
            // Reset buffer scale. This persists enter/exit play mode.
            ScalableBufferManager.ResizeBuffers(1, 1);
        }

        void Update()
        {
            // Dynamic resolution
            if (_currentResolutionScale != _resolutionScale)
            {
                ScalableBufferManager.ResizeBuffers(_resolutionScale, _resolutionScale);
                _currentResolutionScale = _resolutionScale;
            }
        }
#endif
    }
}
