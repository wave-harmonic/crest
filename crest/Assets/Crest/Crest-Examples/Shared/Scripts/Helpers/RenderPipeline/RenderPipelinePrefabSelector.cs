// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Examples
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;

    [ExecuteAlways]
    public class RenderPipelinePrefabSelector : MonoBehaviour
    {
        [SerializeField]
        GameObject _prefabLegacy;

        [SerializeField]
        GameObject _prefabHighDefinition;

        [SerializeField]
        GameObject _prefabUniversal;

        GameObject _prefab;

        void OnEnable()
        {
            if (transform.childCount == 0)
            {
                LoadPrefab();
            }
            else if (_prefab == null)
            {
                _prefab = transform.GetChild(0).gameObject;
            }

            RenderPipelineManager.activeRenderPipelineTypeChanged -= LoadPrefab;
            RenderPipelineManager.activeRenderPipelineTypeChanged += LoadPrefab;
        }

        void OnDisable()
        {
            Helpers.Destroy(_prefab);
            RenderPipelineManager.activeRenderPipelineTypeChanged -= LoadPrefab;
        }

        void LoadPrefab()
        {
            switch (RenderPipelineHelper.CurrentRenderPipeline)
            {
                case Crest.RenderPipeline.Legacy:
                    LoadPrefab(_prefabLegacy);
                    break;
                case Crest.RenderPipeline.Universal:
                    LoadPrefab(_prefabUniversal);
                    break;
                case Crest.RenderPipeline.HighDefinition:
                    LoadPrefab(_prefabHighDefinition);
                    break;
                default:
                    throw new System.Exception("Crest: An unknown render pipeline is active.");
            }
        }

        void LoadPrefab(GameObject prefab)
        {
            if (_prefab != null)
            {
                Helpers.Destroy(_prefab);
            }

            if (prefab == null)
            {
                return;
            }

#if UNITY_EDITOR
            _prefab = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
#else
            _prefab = (GameObject)Instantiate(prefab, transform);
#endif

#if UNITY_EDITOR
            // Disable editing for the prefab and its hierarchy because using "Apply to Prefab" on a property in the
            // inspector will recreate the prefab and it could be serialised. Prefab editing can be done with context in
            // in the prefab stage so there is no need to edit in the scene stage.
            foreach (var transform in _prefab.GetComponentsInChildren<Transform>())
            {
                transform.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            }
#endif
        }
    }
}
