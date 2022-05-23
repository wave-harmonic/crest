// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Restores "Lighting > Environment" settings after switching from HDRP. "Lighting > Other Settings" do not need
// restoring. We only need to restore the skybox as we use the default values for everything else.

namespace Crest.Examples
{
    using UnityEngine;
    using UnityEngine.Rendering;

    [ExecuteAlways]
    public class RenderPipelineLightingSettingsUpdater : MonoBehaviour
    {
        [SerializeField]
        Material _skybox;

        void OnEnable()
        {
            UpdateEnvironmentSettings();
            RenderPipelineManager.activeRenderPipelineTypeChanged -= UpdateEnvironmentSettings;
            RenderPipelineManager.activeRenderPipelineTypeChanged += UpdateEnvironmentSettings;
        }

        void OnDisable()
        {
            RenderPipelineManager.activeRenderPipelineTypeChanged -= UpdateEnvironmentSettings;
        }

        void UpdateEnvironmentSettings()
        {
            if (RenderPipelineHelper.IsLegacy || RenderPipelineHelper.IsUniversal)
            {
                RenderSettings.skybox = _skybox;
            }
        }
    }
}
