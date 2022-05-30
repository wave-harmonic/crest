// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Crest
{
    [CreateAssetMenu(fileName = "SimSettingsClipSurface", menuName = "Crest/Clip Surface Sim Settings", order = 10000)]
    [CrestHelpURL("user/ocean-simulation", "clip-surface-settings")]
    public class SimSettingsClipSurface : SimSettingsBase
    {
        /// <summary>
        /// The version of this asset. Can be used to migrate across versions. This value should
        /// only be changed when the editor upgrades the version.
        /// </summary>
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _version = 0;
#pragma warning restore 414

        // The clip values only really need 8bits (unless using signed distance).
        [Tooltip("The render texture format to use for the clip surface simulation. It should only be changed if you need more precision. See the documentation for information.")]
        public GraphicsFormat _renderTextureGraphicsFormat = GraphicsFormat.R8_UNorm;

        public override void AddToSettingsHash(ref int settingsHash)
        {
            base.AddToSettingsHash(ref settingsHash);
            Hashy.AddInt((int)_renderTextureGraphicsFormat, ref settingsHash);
        }
    }
}
