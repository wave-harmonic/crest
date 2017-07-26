// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

/// <summary>
/// This is to ensure depth is available in case we use it in the ocean surface shader
/// </summary>
[ExecuteInEditMode]
public class SetCameraDepthTexture : MonoBehaviour {

	public DepthTextureMode _mode = DepthTextureMode.Depth;

	void Start()
	{
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
	}
}
