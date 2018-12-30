// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Serialization;

namespace Crest
{
    /// <summary>
    /// Renders terrain height / ocean depth once into a render target to cache this off and avoid rendering it every frame.
    /// This should be used for static geometry, dynamic objects should be tagged with the Render Ocean Depth component.
    /// </summary>
    public class OceanDepthCache : MonoBehaviour
    {
        [Tooltip("Can be disabled to delay population of the cache.")]
        public bool _populateOnStartup = true;

        [Tooltip("The layers to render into the depth cache.")]
        public string[] _layerNames;

        [Tooltip("The resolution of the cached depth - lower will be more efficient.")]
        public int _resolution = 512;

        // a big hill will still want to write its height into the depth texture
        [Tooltip("The 'near plane' for the depth cache camera (top down).")]
        public float _cameraMaxTerrainHeight = 100f;

        [Tooltip("Will render into the cache every frame.")]
        public bool _forceAlwaysUpdateDebug = false;

        RenderTexture _cache;
        GameObject _drawCacheQuad;
        Camera _camDepthCache;

        void Start()
        {
            if (_layerNames == null || _layerNames.Length < 1)
            {
                Debug.LogError("At least one layer name to render into the cache must be provided.", this);
                enabled = false;
                return;
            }

            if (OceanRenderer.Instance == null)
            {
                enabled = false;
                return;
            }

            if (_populateOnStartup)
            {
                PopulateCache();
            }
        }

#if UNITY_EDITOR
        void Update()
        {
            if (_forceAlwaysUpdateDebug)
            {
                PopulateCache();
            }
        }
#endif

        public void PopulateCache()
        {
            var layerMask = 0;
            foreach (var layer in _layerNames)
            {
                int layerIdx = LayerMask.NameToLayer(layer);
                if (string.IsNullOrEmpty(layer) || layerIdx == -1)
                {
                    Debug.LogError("OceanDepthCache: Invalid layer specified: \"" + layer + 
                        "\". Please specify valid layers for objects/geometry that provide the ocean depth.", this);
                }
                else
                {
                    layerMask = layerMask | (1 << layerIdx);
                }
            }
            if (layerMask == 0)
            {
                Debug.LogError("No valid layers for populating depth cache, aborting.", this);
            }
            
            if (_cache == null)
            {
                _cache = new RenderTexture(_resolution, _resolution, 0);
                _cache.name = gameObject.name + "_oceanDepth";
                _cache.format = RenderTextureFormat.RHalf;
                _cache.useMipMap = false;
                _cache.anisoLevel = 0;
            }

            if (_drawCacheQuad == null)
            {
                _drawCacheQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(_drawCacheQuad.GetComponent<Collider>());
                _drawCacheQuad.name = "Draw_" + _cache.name;
                _drawCacheQuad.transform.SetParent(transform, false);
                _drawCacheQuad.transform.localEulerAngles = 90f * Vector3.right;
                _drawCacheQuad.AddComponent<RegisterSeaFloorDepthInput>();
                var qr = _drawCacheQuad.GetComponent<Renderer>();
                qr.material = new Material(Shader.Find("Ocean/Inputs/Depth/Cached Depths"));
                qr.material.mainTexture = _cache;
                qr.enabled = false;
            }

            if (_camDepthCache == null)
            {
                _camDepthCache = new GameObject("DepthCacheCam").AddComponent<Camera>();
                _camDepthCache.transform.position = transform.position + Vector3.up * _cameraMaxTerrainHeight;
                _camDepthCache.transform.parent = transform;
                _camDepthCache.transform.localEulerAngles = 90f * Vector3.right;
                _camDepthCache.orthographic = true;
                _camDepthCache.orthographicSize = Mathf.Max(transform.lossyScale.x / 2f, transform.lossyScale.z / 2f);
                _camDepthCache.targetTexture = _cache;
                _camDepthCache.cullingMask = layerMask;
                _camDepthCache.clearFlags = CameraClearFlags.SolidColor;
                // 0 means '0m above very deep sea floor'
                _camDepthCache.backgroundColor = Color.black;
                _camDepthCache.enabled = false;
                _camDepthCache.allowMSAA = false;
                // I'd prefer to destroy the cam object, but I found sometimes (on first start of editor) it will fail to render.
                _camDepthCache.gameObject.SetActive(false);
            }

            // Hackety-hack: this seems to be the only way to pass parameters to the shader when using RenderWithShader!
            Shader.SetGlobalVector("_OceanCenterPosWorld", OceanRenderer.Instance.transform.position);
            _camDepthCache.RenderWithShader(Shader.Find("Ocean/Inputs/Depth/Ocean Depth From Geometry"), null);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0f, 1f));
        }
    }
}
