// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Previews painted input.
    /// </summary>
    [CustomPreview(typeof(PaintingHelper))]
    public class UserPaintedDataPreview : ObjectPreview
    {
        public override bool HasPreviewGUI() => true;

        /// <summary>
        /// Text displayed on top of preview.
        /// </summary>
        public override string GetInfoString()
        {
            var paint = target as PaintingHelper;
            var client = paint?.GetComponent<IPaintedDataClient>();
            var data = client?.Texture;
            if (data == null) return "";

            return $"{data.Resolution.x}x{data.Resolution.y} {data.GraphicsFormat}";
        }

        /// <summary>
        /// Draws painted data.
        /// </summary>
        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);

            if (Mathf.Approximately(r.width, 1f)) return;

            var paint = target as PaintingHelper;
            var client = paint?.GetComponent<IPaintedDataClient>();
            var data = client?.Texture;
            var tex = data?.Texture;
            if (tex == null) return;

            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, false);
        }
    }
}
