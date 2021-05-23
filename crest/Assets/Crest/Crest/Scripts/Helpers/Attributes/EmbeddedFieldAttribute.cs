// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using UnityEngine;

#if UNITY_EDITOR
    using Crest.EditorHelpers;
    using UnityEditor;
#endif

    public class EmbeddedAttribute : DecoratedPropertyAttribute
    {
#if UNITY_EDITOR
        internal EmbeddedAssetEditor editor;
#endif

        public EmbeddedAttribute()
        {
#if UNITY_EDITOR
            editor = new EmbeddedAssetEditor();
#endif
        }

#if UNITY_EDITOR
        internal override void OnGUI(Rect position, SerializedProperty property, GUIContent label, DecoratedDrawer drawer)
        {
            EmbeddedAttribute embeddedAttribute = this;
            embeddedAttribute.editor.DrawEditorCombo(label, drawer, property, "asset");
        }

        internal override bool NeedsControlRectangle => false;
#endif
    }
}
