// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Previews painted input.
    /// </summary>
    [CustomPreview(typeof(UserDataPainted))]
    public class UserPaintedDataPreview : ObjectPreview
    {
        public override bool HasPreviewGUI() => true;

        /// <summary>
        /// Text displayed on top of preview.
        /// </summary>
        public override string GetInfoString()
        {
            var data = target as UserDataPainted;
            if (data == null || data._tex == null) return "";

            return $"{data._tex.Resolution.x}x{data._tex.Resolution.y}"; // {data._data.graphicsFormat}";
        }

        /// <summary>
        /// Draws painted data.
        /// </summary>
        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);

            if (Mathf.Approximately(r.width, 1f)) return;

            var data = target as UserDataPainted;
            if (data == null || data._tex == null) return;

            var tex = data._tex.GPUTexture(UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat, CPUTexture2DHelpers.ColorConstructFnOneChannel);

            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, false);
        }
    }
}
