// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if UNITY_EDITOR

namespace Crest.Examples
{
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Makes the hierarchy editable only when the desired RP is active.
    /// </summary>
    [ExecuteAlways]
    public class RenderPipelineEditLock : MonoBehaviour
    {
        [SerializeField]
        Crest.RenderPipeline _renderPipeline;

        HideFlags _oldHideFlags;

        void OnEnable()
        {
            _oldHideFlags = gameObject.hideFlags;
            UpdateHideFlags();
            RenderPipelineManager.activeRenderPipelineTypeChanged -= UpdateHideFlags;
            RenderPipelineManager.activeRenderPipelineTypeChanged += UpdateHideFlags;
        }

        void OnDisable()
        {
            gameObject.hideFlags = _oldHideFlags;
            RenderPipelineManager.activeRenderPipelineTypeChanged -= UpdateHideFlags;
        }

        void OnValidate()
        {
            UpdateHideFlags();
        }

        void UpdateHideFlags()
        {
            // This can be null.
            if (!this)
            {
                return;
            }

            if (_renderPipeline == Crest.RenderPipeline.None)
            {
                return;
            }

            if (RenderPipelineHelper.CurrentRenderPipeline != _renderPipeline)
            {
                // Make hierarchy not editable.
                foreach (var transform in GetComponentsInChildren<Transform>())
                {
                    transform.gameObject.hideFlags = HideFlags.NotEditable;
                }
            }
            else
            {
                // Make hierarchy editable.
                foreach (var transform in GetComponentsInChildren<Transform>())
                {
                    transform.gameObject.hideFlags = _oldHideFlags;
                }
            }
        }
    }
}

#endif // UNITY_EDITOR
