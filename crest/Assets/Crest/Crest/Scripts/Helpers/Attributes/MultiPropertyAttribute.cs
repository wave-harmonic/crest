// Crest Ocean System

// Adapted from: https://forum.unity.com/threads/drawing-a-field-using-multiple-property-drawers.479377/

// DecoratedPropertyAttribute renders the field and DecoratorAttribute decorates said field. The decorator changes the
// GUI state so that the decorated field receives that state. The DecoratedDrawer targets DecoratedPropertyAttribute,
// calls DecoratorAttribute.Decorate for each decorator and reverts GUI state.

namespace Crest
{
    using UnityEngine;
    using System;

#if UNITY_EDITOR
    using Crest.EditorHelpers;
    using UnityEditor;
#endif

    /// <summary>
    /// Renders a property field accommodating decorator properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public abstract class DecoratedPropertyAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        /// <summary>
        /// Override this method to customise the label.
        /// </summary>
        internal virtual GUIContent BuildLabel(GUIContent label) => label;

        /// <summary>
        /// Override this method to make your own IMGUI based GUI for the property.
        /// </summary>
        internal abstract void OnGUI(Rect position, SerializedProperty property, GUIContent label, DecoratedDrawer drawer);

        /// <summary>
        /// Override this method to specify how tall the GUI for this field is in pixels.
        /// </summary>
        internal virtual float? GetPropertyHeight(SerializedProperty property, GUIContent label) => null;
#endif
    }

    /// <summary>
    /// Decorates a decorator field by changing GUI state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public abstract class DecoratorAttribute : Attribute
    {
        public int order;

#if UNITY_EDITOR
        /// <summary>
        /// Override this method to customise the label.
        /// </summary>
        internal virtual GUIContent BuildLabel(GUIContent label) => label;

        /// <summary>
        /// Override this method to additively change the appearance of a decorated field.
        /// </summary>
        internal abstract void Decorate(Rect position, SerializedProperty property, GUIContent label, DecoratedDrawer drawer);
#endif
    }

    /// <summary>
    /// Renders the property using EditorGUI.PropertyField.
    /// </summary>
    public class DecoratedFieldAttribute : DecoratedPropertyAttribute
    {
#if UNITY_EDITOR
        internal override void OnGUI(Rect position, SerializedProperty property, GUIContent label, DecoratedDrawer drawer)
        {
            EditorGUI.PropertyField(position, property, label);
        }
#endif
    }

    /// <summary>
    /// Renders the property using EditorGUI.Delayed*.
    /// </summary>
    public class DelayedAttribute : DecoratedPropertyAttribute
    {
#if UNITY_EDITOR
        internal override void OnGUI(Rect position, SerializedProperty property, GUIContent label, DecoratedDrawer drawer)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    EditorGUI.DelayedFloatField(position, property, label);
                    break;
                case SerializedPropertyType.Integer:
                    EditorGUI.DelayedIntField(position, property, label);
                    break;
                case SerializedPropertyType.String:
                    EditorGUI.DelayedTextField(position, property, label);
                    break;
                default:
                    EditorGUI.LabelField(position, label.text, "Delayed: must be float, integer, or string.");
                    break;
            }
        }
#endif
    }

    /// <summary>
    /// Renders the property using EditorGUI.Slider.
    /// </summary>
    public class RangeAttribute : DecoratedPropertyAttribute
    {
        readonly float minimum;
        readonly float maximum;

        public RangeAttribute(float minimum, float maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
        }

#if UNITY_EDITOR
        internal override void OnGUI(Rect position, SerializedProperty property, GUIContent label, DecoratedDrawer drawer)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    EditorGUI.Slider(position, property, minimum, maximum, label);
                    break;
                case SerializedPropertyType.Integer:
                    EditorGUI.IntSlider(position, property, (int)minimum, (int)maximum, label);
                    break;
                default:
                    EditorGUI.LabelField(position, label.text, "Range: must be float or integer.");
                    break;
            }
        }
#endif
    }
}
