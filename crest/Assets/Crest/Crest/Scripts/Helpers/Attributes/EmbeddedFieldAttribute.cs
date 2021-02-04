// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using Crest.EditorHelpers;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    public class EmbeddedFieldAttribute : PropertyAttribute
    {
        internal EmbeddedAssetEditor editor;

        public EmbeddedFieldAttribute()
        {
            editor = new EmbeddedAssetEditor();
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(EmbeddedFieldAttribute))]
    public class EmbeddedFieldAttributePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EmbeddedFieldAttribute embeddedAttribute = (EmbeddedFieldAttribute)attribute;
            embeddedAttribute.editor.DrawEditorCombo(this, property, "asset");
        }

        // Removes space above embedded editor so the embedded editor replaces this drawer.
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0f;
    }
#endif
}
