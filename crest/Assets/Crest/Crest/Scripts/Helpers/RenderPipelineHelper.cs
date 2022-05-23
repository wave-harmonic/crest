// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if CREST_URP
using UnityEngine.Rendering.Universal;
#endif
#if CREST_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Crest
{
    public enum RenderPipeline
    {
        None,
        Legacy,
        HighDefinition,
        Universal,
    }

    public static class RenderPipelineHelper
    {
        public static RenderPipeline CurrentRenderPipeline { get; private set; }
        public static bool IsLegacy => CurrentRenderPipeline == RenderPipeline.Legacy;
        public static bool IsHighDefinition => CurrentRenderPipeline == RenderPipeline.HighDefinition;
        public static bool IsUniversal => CurrentRenderPipeline == RenderPipeline.Universal;

        // This will run once on load for both editor and standalone. If domain reload is enabled, it will also run
        // when entering play mode.
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void OnLoad()
        {
            UpdateRenderPipeline();

            // Delegates execute in order so the state will be ready for any other objects using the same delegate.
            RenderPipelineManager.activeRenderPipelineTypeChanged -= UpdateRenderPipeline;
            RenderPipelineManager.activeRenderPipelineTypeChanged += UpdateRenderPipeline;
        }

        static void UpdateRenderPipeline()
        {
            // GraphicsSettings.currentRenderPipeline handles both graphics setting and current quality level. There is
            // also RenderPipelineManager.currentPipeline, but:
            // > Unity updates this property only after rendering at least one frame with the active render pipeline,
            // > which can take up to four calls to Update. This means that this property is null on startup, and does not immediately reflect changes to the active render pipeline.
            // https://docs.unity3d.com/Manual/srp-setting-render-pipeline-asset.html
            // https://docs.unity3d.com/Documentation/ScriptReference/Rendering.RenderPipelineManager-currentPipeline.html

            if (GraphicsSettings.currentRenderPipeline == null)
            {
                CurrentRenderPipeline = RenderPipeline.Legacy;
                return;
            }

#if CREST_URP
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
            {
                CurrentRenderPipeline = RenderPipeline.Universal;
                return;
            }
#endif

#if CREST_HDRP
            if (GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset)
            {
                CurrentRenderPipeline = RenderPipeline.HighDefinition;
                return;
            }
#endif
        }

        public static string CurrentRenderPipelineShortName
        {
            get
            {
                return CurrentRenderPipeline switch
                {
                    RenderPipeline.Legacy => "BIRP",
                    RenderPipeline.HighDefinition => "HDRP",
                    RenderPipeline.Universal => "URP",
                    _ => throw new System.Exception("Crest: An unknown render pipeline is active."),
                };
            }
        }
    }
}
