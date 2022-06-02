// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Previews painted input.
    /// </summary>
    public class UserPaintedDataPreview : ObjectPreview
    {
        public override bool HasPreviewGUI() => (target as IPaintable).ShowPaintedDataPreview;

        /// <summary>
        /// Text displayed on top of preview.
        /// </summary>
        public override string GetInfoString()
        {
            var tex = (target as IPaintable)?.PaintedData?.Texture;
            if (tex == null) return "";
            return $"{tex.width}x{tex.height} {tex.graphicsFormat}";
        }

        /// <summary>
        /// Draws painted data.
        /// </summary>
        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);

            if (Mathf.Approximately(r.width, 1f)) return;

            var tex = (target as IPaintable)?.PaintedData?.Texture;
            if (tex == null) return;

            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, false);
        }
    }
}
