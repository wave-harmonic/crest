// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Unity serializes layers as bitmasks which causes havok when moving data to a new project. This script assigns layers
    /// by name instead of by index/mask, and throws an error if a named layer does not exist.
    /// </summary>
    public class ApplyLayers : MonoBehaviour
    {
        [Tooltip("If specified, this GameObject will be assigned to layer with this name.")]
        public string _layerName = "";

        [Tooltip("If a camera is attached, these layer names will be added to the cull mask (so that they will NOT be drawn).")]
        public string[] _cullExcludeLayers;
        [Tooltip("If a camera is attached, these layer names will be added to the cull mask (so that they will NOT be drawn).")]
        public string[] _cullIncludeLayers;

        void Start()
        {
            if(!string.IsNullOrEmpty(_layerName))
            {
                var layerIndex = LayerMask.NameToLayer(_layerName);

                if (layerIndex != -1)
                {
                    gameObject.layer = layerIndex;
                }
                else
                {
                    Debug.LogError("Layer named \"" + _layerName + "\" does not exist, please add this layer to the project.", this);
                }
            }

            var cam = GetComponent<Camera>();
            if( cam != null )
            {
                int mask = cam.cullingMask;

                if (_cullExcludeLayers != null && _cullExcludeLayers.Length > 0)
                {
                    foreach (var layer in _cullExcludeLayers)
                    {
                        if (string.IsNullOrEmpty(layer))
                            continue;

                        int idx = LayerMask.NameToLayer(layer);
                        if (idx == -1)
                        {
                            Debug.LogError("Layer \"" + layer + "\" does not exist in the project, please create it.", this);
                            continue;
                        }

                        mask &= ~(1 << idx);
                    }
                }
                if (_cullIncludeLayers != null && _cullIncludeLayers.Length > 0)
                {
                    foreach (var layer in _cullIncludeLayers)
                    {
                        if (string.IsNullOrEmpty(layer))
                            continue;

                        int idx = LayerMask.NameToLayer(layer);
                        if (idx == -1)
                        {
                            Debug.LogError("Layer \"" + layer + "\" does not exist in the project, please create it.", this);
                            continue;
                        }

                        mask |= 1 << idx;
                    }
                }

                cam.cullingMask = mask;
            }
        }
    }
}
