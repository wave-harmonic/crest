namespace Crest.EditorHelpers
{
    using UnityEngine;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(EmbeddedFieldAttribute))]
    public class EmbeddedFieldAttributeEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EmbeddedFieldAttribute embeddedAttribute = (EmbeddedFieldAttribute)attribute;
            embeddedAttribute.editor.DrawEditorCombo(this, property, "asset");
        }
    }
}
