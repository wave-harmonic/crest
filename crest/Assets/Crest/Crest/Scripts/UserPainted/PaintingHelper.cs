// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// TODO rename file and move to editor folder

using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

namespace Crest
{
#if UNITY_EDITOR
    [EditorTool("Crest Input Painting", typeof(IPaintable))]
    public class InputPaintingEditorTool : EditorTool
    {
        public override GUIContent toolbarIcon => _toolbarIcon ??
            (_toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PaintedWaves.png"), "Crest Input Painting"));

        GUIContent _toolbarIcon;

        public static bool CurrentlyPainting => ToolManager.activeToolType == typeof(InputPaintingEditorTool);
    }
#endif
}
