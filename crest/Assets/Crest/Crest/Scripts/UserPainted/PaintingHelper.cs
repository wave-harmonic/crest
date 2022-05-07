// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// TODO rename file and move to editor folder

using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

namespace Crest
{
#if UNITY_EDITOR
    // This typeof means it gets activated for the above component
    [EditorTool("Crest Wave Painting", typeof(RegisterLodDataInputBase))]
    class WavePaintingEditorTool : EditorTool
    {
        public override GUIContent toolbarIcon => _toolbarIcon ??
            (_toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PaintedWaves.png"), "Crest Wave Painting"));

        public static bool CurrentlyPainting => ToolManager.activeToolType == typeof(WavePaintingEditorTool);

        GUIContent _toolbarIcon;
    }
#endif
}
