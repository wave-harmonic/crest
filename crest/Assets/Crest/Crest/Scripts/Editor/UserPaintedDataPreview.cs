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
            if (data != null)
            {
                if (data._dataRT != null)
                {
                    return $"{data._dataRT.width}x{data._dataRT.height} {data._dataRT.graphicsFormat}";
                }
                else if (data._dataTexture2D != null)
                {
                    return $"{data._dataTexture2D.width}x{data._dataTexture2D.height} {data._dataTexture2D.graphicsFormat}";
                }
            }
            return "";
        }

        /// <summary>
        /// Draws painted data.
        /// </summary>
        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);

            if (Mathf.Approximately(r.width, 1f)) return;

            var data = target as UserDataPainted;
            if (data != null)
            {
                if (data._dataRT != null)
                {
                    GUI.DrawTexture(r, data._dataRT, ScaleMode.ScaleToFit, false);
                }
                else if (data._dataTexture2D != null)
                {
                    GUI.DrawTexture(r, data._dataTexture2D, ScaleMode.ScaleToFit, false);
                }
            }
        }
    }
}
