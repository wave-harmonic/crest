// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

/// <summary>
/// Triggers the scene render to happen in wireframe. Unfortunately this currently affects the GUI elements as well.
/// </summary>
[RequireComponent(typeof(Camera))]
[AddComponentMenu(Crest.Internal.Constants.MENU_PREFIX_DEBUG + "Render Wire Frame")]
public class RenderWireFrame : MonoBehaviour
{
    public bool _gui = true;
    public static bool _wireFrame = false;

    Camera _cam;
    CameraClearFlags _defaultClearFlags;

    void Start()
    {
        _cam = GetComponent<Camera>();
        _defaultClearFlags = _cam.clearFlags;
    }

    void Update()
    {
        _cam.clearFlags = _wireFrame ? CameraClearFlags.SolidColor : _defaultClearFlags;
    }

    void OnPreRender()
    {
        if (enabled)
        {
            GL.wireframe = _wireFrame;
        }
    }
}
