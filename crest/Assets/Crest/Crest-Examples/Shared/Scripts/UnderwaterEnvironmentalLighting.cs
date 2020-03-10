// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Provides out-scattering based on the camera's underwater depth. It scales down environmental lighting
    /// (directional light, reflections, ambient etc) with the underwater depth. This works with vanilla lighting, but 
    /// uncommon or custom lighting will require a custom solution (use this for reference).
    /// </summary>
    public class UnderwaterEnvironmentalLighting : MonoBehaviour
    {
        float _lightIntensity;
        float _ambientIntensity;
        float _reflectionIntensity;
        float _fogDensity;

        float _averageDensity = 0f;

        void Start()
        {
            Color density = OceanRenderer.Instance.OceanMaterial.GetColor("_DepthFogDensity");
            _averageDensity = (density.r + density.g + density.b) / 3f;

            // Store lighting settings
            _lightIntensity = OceanRenderer.Instance._primaryLight.intensity;
            _ambientIntensity = RenderSettings.ambientIntensity;
            _reflectionIntensity = RenderSettings.reflectionIntensity;
            _fogDensity = RenderSettings.fogDensity;
        }

        void OnDisable()
        {
            // Restore lighting settings
            OceanRenderer.Instance._primaryLight.intensity = _lightIntensity;
            RenderSettings.ambientIntensity = _ambientIntensity;
            RenderSettings.reflectionIntensity = _reflectionIntensity;
            RenderSettings.fogDensity = _fogDensity;
        }

        void LateUpdate()
        {
            float depthMultiplier = Mathf.Exp(_averageDensity * 
                Mathf.Min(OceanRenderer.Instance.ViewerHeightAboveWater, 0f));

            // Darken environmental lighting when viewer underwater
            OceanRenderer.Instance._primaryLight.intensity = Mathf.Lerp(0, _lightIntensity, depthMultiplier);
            RenderSettings.ambientIntensity = Mathf.Lerp(0, _ambientIntensity, depthMultiplier);
            RenderSettings.reflectionIntensity = Mathf.Lerp(0, _reflectionIntensity, depthMultiplier);
            RenderSettings.fogDensity = Mathf.Lerp(0, _fogDensity, depthMultiplier);
        }
    }
}
