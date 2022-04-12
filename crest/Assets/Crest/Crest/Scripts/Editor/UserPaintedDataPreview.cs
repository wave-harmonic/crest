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
            if (data == null || data._data == null) return "";

            return $"{data._data.width}x{data._data.height} {data._data.graphicsFormat}";
        }

        /// <summary>
        /// Draws painted data.
        /// </summary>
        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);

            var data = target as UserDataPainted;
            if (data == null) return;

            if (Mathf.Approximately(r.width, 1f)) return;

            GUI.DrawTexture(r, data._data, ScaleMode.ScaleToFit, false);
        }
    }
}
