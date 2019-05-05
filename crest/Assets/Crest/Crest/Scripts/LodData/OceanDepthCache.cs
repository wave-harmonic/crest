// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// This is the original version that uses an auxillary camera and works with Unity's GPU terrain - issue 152.

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Renders terrain height / ocean depth once into a render target to cache this off and avoid rendering it every frame.
    /// This should be used for static geometry, dynamic objects should be tagged with the Render Ocean Depth component.
    /// </summary>
    public class OceanDepthCache : MonoBehaviour
    {
        [Tooltip("Can be disabled to delay population of the cache."), SerializeField]
        bool _populateOnStartup = true;
        [Tooltip("The layers to render into the depth cache."), SerializeField]
        string[] _layerNames = null;

        [Tooltip("The resolution of the cached depth - lower will be more efficient."), SerializeField]
        int _resolution = 512;

        // A big hill will still want to write its height into the depth texture
        [Tooltip("The 'near plane' for the depth cache camera (top down)."), SerializeField]
        float _cameraMaxTerrainHeight = 100f;

        [Tooltip("Will render into the cache every frame. Intended for debugging, will generate garbage."), SerializeField]
#pragma warning disable 414
        bool _forceAlwaysUpdateDebug = false;
#pragma warning restore 414

        RenderTexture _cacheTexture;
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

            if (transform.lossyScale.magnitude < 5f)
            {
                Debug.LogWarning("Ocean depth cache transform scale is small and will capture a small area of the world. Is this intended?", this);
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

        RenderTexture MakeRT(bool depthStencilTarget)
        {
            var fmt = depthStencilTarget ? RenderTextureFormat.Depth : RenderTextureFormat.RHalf;
            Debug.Assert(SystemInfo.SupportsRenderTextureFormat(fmt), "The graphics device does not support the render texture format " + fmt.ToString());
            var result = new RenderTexture(_resolution, _resolution, depthStencilTarget ? 24 : 0);
            result.name = gameObject.name + "_oceanDepth_" + (depthStencilTarget ? "DepthOnly" : "Cache");
            result.format = fmt;
            result.useMipMap = false;
            result.anisoLevel = 0;
            return result;
        }

        void InitObjects()
        {
            if (_cacheTexture == null)
            {
                _cacheTexture = MakeRT(false);
            }

            if (_drawCacheQuad == null)
            {
                _drawCacheQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _drawCacheQuad.hideFlags = HideFlags.DontSave;
                Destroy(_drawCacheQuad.GetComponent<Collider>());
                _drawCacheQuad.name = "Draw_" + _cacheTexture.name + "_NOSAVE";
                _drawCacheQuad.transform.SetParent(transform, false);
                _drawCacheQuad.transform.localEulerAngles = 90f * Vector3.right;
                _drawCacheQuad.AddComponent<RegisterSeaFloorDepthInput>();
                var qr = _drawCacheQuad.GetComponent<Renderer>();
                qr.material = new Material(Shader.Find("Crest/Inputs/Depth/Cached Depths"));
                qr.material.mainTexture = _cacheTexture;
                qr.enabled = false;
            }

            if (_camDepthCache == null)
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
                    return;
                }

                _camDepthCache = new GameObject("DepthCacheCam").AddComponent<Camera>();
                _camDepthCache.transform.position = transform.position + Vector3.up * _cameraMaxTerrainHeight;
                _camDepthCache.transform.parent = transform;
                _camDepthCache.transform.localEulerAngles = 90f * Vector3.right;
                _camDepthCache.orthographic = true;
                _camDepthCache.orthographicSize = Mathf.Max(transform.lossyScale.x / 2f, transform.lossyScale.z / 2f);
                _camDepthCache.cullingMask = layerMask;
                _camDepthCache.clearFlags = CameraClearFlags.SolidColor;
                // 0 means '0m above very deep sea floor'
                _camDepthCache.backgroundColor = Color.black;
                _camDepthCache.enabled = false;
                _camDepthCache.allowMSAA = false;
                _camDepthCache.gameObject.SetActive(false);
            }

            if (_camDepthCache.targetTexture == null)
            {
                _camDepthCache.targetTexture = MakeRT(true);
            }
        }

        void PopulateCache()
        {
            // Make sure we have required objects
            InitObjects();

            // Render scene, saving depths in depth buffer
            _camDepthCache.Render();
            //LightweightRenderPipeline.RenderSingleCamera(context, _camDepthCache);

            Material copyDepthMaterial = new Material(Shader.Find("Crest/Copy Depth Buffer Into Cache"));

            copyDepthMaterial.SetTexture("_CamDepthBuffer", _camDepthCache.targetTexture);

            // Zbuffer params
            //float4 _ZBufferParams;            // x: 1-far/near,     y: far/near, z: x/far,     w: y/far
            float near = _camDepthCache.nearClipPlane, far = _camDepthCache.farClipPlane;
            copyDepthMaterial.SetVector("_CustomZBufferParams", new Vector4(1f - far / near, far / near, (1f - far / near) / far, (far / near) / far));

            // Altitudes for near and far planes
            float ymax = _camDepthCache.transform.position.y - near;
            float ymin = ymax - far;
            copyDepthMaterial.SetVector("_HeightNearHeightFar", new Vector2(ymax, ymin));

            // Copy from depth buffer into the cache
            Graphics.Blit(null, _cacheTexture, copyDepthMaterial);

            if (!_forceAlwaysUpdateDebug)
            {
                _camDepthCache.targetTexture = null;
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0f, 1f));
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            Gizmos.DrawCube(Vector3.up * _cameraMaxTerrainHeight / transform.lossyScale.y, new Vector3(1f, 0f, 1f));
        }
#endif
    }
}
