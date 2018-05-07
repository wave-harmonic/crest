using UnityEngine;

namespace Crest
{
    public class OceanDepthCache : MonoBehaviour
    {
        public bool _populateOnStartup = true;
        public LayerMask _mask;
        public int _resolution = 512;

        RenderTexture _cache;
        GameObject _drawCacheQuad;

        void Start()
        {
            if (_populateOnStartup)
            {
                PopulateCache();
            }
        }

        public void PopulateCache()
        {
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
                _drawCacheQuad.AddComponent<RenderOceanDepth>();
                var qr = _drawCacheQuad.GetComponent<Renderer>();
                qr.material = new Material(Shader.Find("Unlit/Texture"));
                qr.material.mainTexture = _cache;
                qr.enabled = false;
            }

            var cam = new GameObject("DepthCacheCam").AddComponent<Camera>();
            cam.transform.position = transform.position + Vector3.up * 10f;
            cam.transform.parent = transform;
            cam.transform.localEulerAngles = 90f * Vector3.right;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(transform.lossyScale.x / 2f, transform.lossyScale.z / 2f);
            cam.targetTexture = _cache;
            cam.cullingMask = _mask;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.red * 10000f;
            cam.enabled = false;
            cam.allowMSAA = false;
            cam.RenderWithShader(Shader.Find("Ocean/Ocean Depth"), null);
            // I'd prefer to destroy the cam object, but sometimes (on first start of editor) it will fail to render.
            cam.gameObject.SetActive(false);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0f, 1f));
        }
    }
}
