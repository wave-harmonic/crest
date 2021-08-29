// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using UnityEngine;

    /// <summary>
    /// General purpose helpers which, at the moment, do not warrant a seperate file.
    /// </summary>
    public static class Helpers
    {
        public static bool IsMSAAEnabled(Camera camera)
        {
            return camera.allowMSAA && QualitySettings.antiAliasing > 0f;
        }
    }
}
