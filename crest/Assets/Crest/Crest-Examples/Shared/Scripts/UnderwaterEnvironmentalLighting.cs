// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class UnderwaterEnvironmentalLighting : MonoBehaviour
    {
        float lightIntensity;
        float ambientIntensity;
        float reflectionIntensity;
        float fogDensity;

        void OnEnable()
        {
            // Store lighting settings
            lightIntensity = OceanRenderer.Instance._primaryLight.intensity;
            ambientIntensity = RenderSettings.ambientIntensity;
            reflectionIntensity = RenderSettings.reflectionIntensity;
            fogDensity = RenderSettings.fogDensity;
        }

        void OnDisable()
        {
            // Restore lighting settings
            OceanRenderer.Instance._primaryLight.intensity = lightIntensity;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.fogDensity = fogDensity;
        }

        void LateUpdate()
        {
            // Darken light when viewer underwater
            OceanRenderer.Instance._primaryLight.intensity = Mathf.Lerp(0, lightIntensity, OceanRenderer.Instance.DepthMultiplier);
            RenderSettings.ambientIntensity = Mathf.Lerp(0, ambientIntensity, OceanRenderer.Instance.DepthMultiplier);
            RenderSettings.reflectionIntensity = Mathf.Lerp(0, reflectionIntensity, OceanRenderer.Instance.DepthMultiplier);
            RenderSettings.fogDensity = Mathf.Lerp(0, fogDensity, OceanRenderer.Instance.DepthMultiplier);
        }
    }
}
