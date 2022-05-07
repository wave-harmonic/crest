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
        public override bool HasPreviewGUI() => true;

        /// <summary>
        /// Text displayed on top of preview.
        /// </summary>
        public override string GetInfoString()
        {
            var target = this.target as RegisterLodDataInputBase;
            if (!target /*|| !(target is IPaintedDataClient)*/) return "";
            var tex = target.PaintedTexture;
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

            var target = this.target as RegisterLodDataInputBase;
            if (!target /*|| !(target is IPaintedDataClient)*/) return;
            var tex = target.PaintedTexture;
            if (tex == null) return;

            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, false);
        }
    }
}
