// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// This script creates a render texture and assigns it to the camera. We found the connection to the render textures kept breaking
    /// when we saved the scene, so we create and assign it at runtime as a workaround.
    /// </summary>
    public class CreateAssignRenderTexture : MonoBehaviour
    {
        public string _targetName = string.Empty;
        public int _width = 32;
        public int _height = 32;
        public int _depthBits = 0;
        public RenderTextureFormat _format = RenderTextureFormat.ARGBFloat;
        public TextureWrapMode _wrapMode = TextureWrapMode.Clamp;
        public int _antiAliasing = 1;
        public FilterMode _filterMode = FilterMode.Bilinear;
        public int _anisoLevel = 0;
        public bool _useMipMap = false;

        public bool _createPingPongTargets = false;
        bool _created = false;

        void Start()
        {
            if (!_created)
            {
	            if (!_createPingPongTargets)
	            {
	                GetComponent<Camera>().targetTexture = CreateRT();
	            }
	            else
	            {
	                CreatePingPongRts();
	            }
	        }
        }

        public RenderTexture CreateRT()
        {
            return CreateRT(_targetName);
        }

        public RenderTexture CreateRT(string targetName)
        {
            var tex = new RenderTexture( _width, _height, _depthBits, _format );

            if( !string.IsNullOrEmpty(targetName) )
            {
                tex.name = targetName;
            }

            tex.wrapMode = _wrapMode;
            tex.antiAliasing = _antiAliasing;
            tex.filterMode = _filterMode;
            tex.anisoLevel = _anisoLevel;
            tex.useMipMap = _useMipMap;

            _created = true;

            return tex;
        }

        void CreatePingPongRts()
        {
            PingPongRts ppr = GetComponent<PingPongRts>();
            if (ppr == null)
            {
                ppr = gameObject.AddComponent<PingPongRts>();
            }

            ppr.InitRTs(CreateRT(_targetName + "_A"), CreateRT(_targetName + "_B"));
        }
    }
}
