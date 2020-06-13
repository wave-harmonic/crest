// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using UnityEditor;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// The field with this attribute will be drawn enabled/disabled based on another field. For example can be used
    /// to disable a field if a toggle is false. Limitation - conflicts with other property drawers such as Range().
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PredicatedFieldAttribute : PropertyAttribute
    {
        public readonly string _propertyName;
        public readonly bool _inverted;

        public PredicatedFieldAttribute(string propertyName, bool inverted = false)
        {
            _propertyName = propertyName;
            _inverted = inverted;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(PredicatedFieldAttribute))]
    public class PredicatedFieldAttributePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attrib = (attribute as PredicatedFieldAttribute);
            var propName = attrib._propertyName;
            var prop = property.serializedObject.FindProperty(propName);

            var before = GUI.enabled;
            if (prop != null)
            {
                GUI.enabled = attrib._inverted ? !Predicate(prop) : Predicate(prop);
            }
            else
            {
                Debug.LogError($"PredicatedFieldAttributePropertyDrawer - field '{propName}' not found.");
            }

            EditorGUI.PropertyField(position, property, label);

            GUI.enabled = before;
        }

        bool Predicate(SerializedProperty prop)
        {
            if (prop.type == "bool")
            {
                return prop.boolValue;
            }
            else if (prop.type == "int")
            {
                return prop.intValue > 0;
            }
            else if (prop.type == "float")
            {
                return prop.floatValue > 0f;
            }
            else if (prop.type.StartsWith("PPtr"))
            {
                return prop.objectReferenceValue != null;
            }
            else
            {
                Debug.LogError($"PredicatedFieldAttributePropertyDrawer - property type not implemented yet: {prop.type}.", prop.serializedObject.targetObject);
                return true;
            }
        }
    }
#endif
}
