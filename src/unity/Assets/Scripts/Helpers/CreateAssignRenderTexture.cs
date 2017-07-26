// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

/// <summary>
/// This script creates a render texture and assigns it to the camera. We found the connection to the render textures kept breaking
/// when we saved the scene, so we create and assign it at runtime as a workaround.
/// </summary>
[RequireComponent(typeof(Camera))]
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

	void Start()
    {
        RenderTexture tex = new RenderTexture( _width, _height, _depthBits, _format );

        if( !string.IsNullOrEmpty( _targetName ) )
        {
            tex.name = _targetName;
        }

        tex.wrapMode = _wrapMode;
        tex.antiAliasing = _antiAliasing;
        tex.filterMode = _filterMode;
        tex.anisoLevel = _anisoLevel;
        tex.useMipMap = _useMipMap;

        GetComponent<Camera>().targetTexture = tex;
	}
}
