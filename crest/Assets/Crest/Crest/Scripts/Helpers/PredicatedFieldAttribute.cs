// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using UnityEngine;

#if UNITY_EDITOR
using Crest.EditorHelpers;
using UnityEditor;
#endif

namespace Crest
{
    public class PredicatedAttribute : DecoratorAttribute
    {
        public readonly string _propertyName;
        public readonly Type _requiredComponentType;
        public readonly bool _inverted;
        public readonly object _disableIfValueIs;

        /// <summary>
        /// The field with this attribute will be drawn enabled/disabled based on another field. For example can be used
        /// to disable a field if a toggle is false.
        /// </summary>
        /// <param name="requiredComponentType">If a component of this type is not attached to this GameObject, disable the GUI (or enable if inverted is true).</param>
        /// <param name="inverted">Flip behaviour - for example disable if a bool field is set to true (instead of false).</param>
        public PredicatedAttribute(Type requiredComponentType, bool inverted = false)
        {
            _requiredComponentType = requiredComponentType;
            _inverted = inverted;
        }

        /// <summary>
        /// The field with this attribute will be drawn enabled/disabled based on another field. For example can be used
        /// to disable a field if a toggle is false.
        /// </summary>
        /// <param name="propertyName">The name of the other property whose value dictates whether this field is enabled or not.</param>
        /// <param name="inverted">Flip behaviour - for example disable if a bool field is set to true (instead of false).</param>
        /// <param name="disableIfValueIs">If the field has this value, disable the GUI (or enable if inverted is true).</param>
        public PredicatedAttribute(string propertyName, bool inverted = false, object disableIfValueIs = null)
        {
            _propertyName = propertyName;
            _inverted = inverted;
            _disableIfValueIs = disableIfValueIs;
        }

        /// <summary>
        /// The field with this attribute will be drawn enabled/disabled based on another field. For example can be used
        /// to disable a field if a toggle is false.
        /// </summary>
        /// <param name="propertyName">The name of the other property whose value dictates whether this field is enabled or not.</param>
        /// <param name="requiredComponentType">If a component of this type is not attached to this GameObject, disable the GUI (or enable if inverted is true).</param>
        /// <param name="inverted">Flip behaviour - for example disable if a bool field is set to true (instead of false).</param>
        /// <param name="disableIfValueIs">If the field has this value, disable the GUI (or enable if inverted is true).</param>
        public PredicatedAttribute(string propertyName, Type requiredComponentType, bool inverted = false, object disableIfValueIs = null)
        {
            _propertyName = propertyName;
            _requiredComponentType = requiredComponentType;
            _inverted = inverted;
            _disableIfValueIs = disableIfValueIs;
        }

#if UNITY_EDITOR
        public bool GUIEnabled(SerializedProperty prop)
        {
            bool result;

            if (prop.type == "int")
            {
                // Enable GUI if int value of field is not equal to 0, or whatever the disable-value is set to
                result = prop.intValue != ((int?)_disableIfValueIs ?? 0);
            }
            else if (prop.type == "bool")
            {
                // Enable GUI if disable-value is 0 and field is true, or disable-value is not 0 and field is false
                result = prop.boolValue ^ (((int?)_disableIfValueIs ?? 0) != 0);
            }
            else if (prop.type == "float")
            {
                result = prop.floatValue != ((float?)_disableIfValueIs ?? 0);
            }
            else if (prop.type == "string")
            {
                // It appears that a string value cannot be null.
                result = prop.stringValue != ((string)_disableIfValueIs ?? "");
            }
            else if (prop.type == "Enum")
            {
                result = prop.enumValueIndex != ((int?)_disableIfValueIs ?? 0);
            }
            else if (prop.type.StartsWith("PPtr"))
            {
                result = prop.objectReferenceValue != null;
            }
            else
            {
                Debug.LogError($"Crest: PredicatedAttribute: property type not implemented yet: {prop.type}.", prop.serializedObject.targetObject);
                return true;
            }

            return _inverted ? !result : result;
        }

        internal override void Decorate(Rect position, SerializedProperty property, GUIContent label, DecoratedDrawer drawer)
        {
            var enabled = true;

            if (_propertyName != null)
            {
                // Get the other property to be the predicate for the enabled/disabled state of this property.
                var otherProperty = property.serializedObject.FindProperty(_propertyName);
                if (otherProperty != null)
                {
                    enabled = GUIEnabled(otherProperty);
                }
            }

            if (_requiredComponentType != null && property.serializedObject.targetObject != null)
            {
                var comp = property.serializedObject.targetObject as Component;
                var enabledByComponent = comp.gameObject.TryGetComponent(_requiredComponentType, out _);
                if (_inverted) enabledByComponent = !enabledByComponent;
                enabled = enabledByComponent && enabled;
            }

            GUI.enabled = enabled;
        }
#endif
    }
}
