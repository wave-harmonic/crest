// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

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

        [Tooltip("Renderers in scene to render into this depth cache. When provided this saves the code from doing an expensive FindObjectsOfType() call. If one or more renderers are specified, the layer setting is ignored."),  SerializeField]
        Renderer[] _geometryToRenderIntoCache = new Renderer[0];

        [Tooltip("The layers to render into the depth cache. This is ignored if geometry instances are specified in the Geometry To Render Into Cache field."), SerializeField]
        string[] _layerNames;

        [Tooltip("The resolution of the cached depth - lower will be more efficient."), SerializeField]
        int _resolution = 512;

        // A big hill will still want to write its height into the depth texture
        [Tooltip("The 'near plane' for the depth cache camera (top down)."), SerializeField]
        float _cameraMaxTerrainHeight = 100f;

        [Tooltip("Will render into the cache every frame. Intended for debugging, will generate garbage."), SerializeField]
        bool _forceAlwaysUpdateDebug = false;

        RenderTexture _cacheTexture;
        GameObject _drawCacheQuad;

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

        void InitObjects()
        {
            if (_cacheTexture == null)
            {
                _cacheTexture = new RenderTexture(_resolution, _resolution, 0);
                _cacheTexture.name = gameObject.name + "_oceanDepth";
                _cacheTexture.format = RenderTextureFormat.RHalf;
                _cacheTexture.useMipMap = false;
                _cacheTexture.anisoLevel = 0;
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
                qr.material = new Material(Shader.Find("Ocean/Inputs/Depth/Cached Depths"));
                qr.material.mainTexture = _cacheTexture;
                qr.enabled = false;
            }
        }

        public void PopulateCache()
        {
            // Make sure we have required objectss
            InitObjects();

            // Render into the cache
            {
                var buf = new CommandBuffer();
                buf.name = "Populate " + gameObject.name;

                buf.SetRenderTarget(_cacheTexture);
                buf.ClearRenderTarget(true, true, Color.black);

                var worldToCameraMatrix =
                    LodTransform.CalculateWorldToCameraMatrixRHS(transform.position + Vector3.up * _cameraMaxTerrainHeight, transform.rotation * Quaternion.AngleAxis(90f, Vector3.right));
                var projectionMatrix =
                    Matrix4x4.Ortho(-0.5f * transform.lossyScale.x, 0.5f * transform.lossyScale.x, -0.5f * transform.lossyScale.z, 0.5f * transform.lossyScale.z, 0.5f, 500f);
                buf.SetViewProjectionMatrices(worldToCameraMatrix, projectionMatrix);

                var mat = new Material(Shader.Find("Ocean/Inputs/Depth/Ocean Depth From Geometry"));
                mat.SetVector("_OceanCenterPosWorld", OceanRenderer.Instance.transform.position);

                if (_geometryToRenderIntoCache != null && _geometryToRenderIntoCache.Length > 0)
                {
                    PopulateUsingGeometryList(mat, buf);
                }
                else
                {
                    PopulateUsingLayers(mat, buf);
                }

                Graphics.ExecuteCommandBuffer(buf);
            }
        }

        void PopulateUsingGeometryList(Material mat, CommandBuffer buf)
        {
            Debug.Assert(_geometryToRenderIntoCache != null && _geometryToRenderIntoCache.Length > 0);

            foreach (var rend in _geometryToRenderIntoCache)
            {
                buf.DrawRenderer(rend, mat);
            }
        }

        void PopulateUsingLayers(Material mat, CommandBuffer buf)
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

            var gos = FindObjectsOfType<GameObject>();
            foreach (var go in gos)
            {
                if (0 == ((1 << go.layer) & layerMask)) continue;
                var rend = go.GetComponent<Renderer>();
                if (rend == null) continue;
                buf.DrawRenderer(rend, mat);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0f, 1f));
        }
#endif
    }
}
