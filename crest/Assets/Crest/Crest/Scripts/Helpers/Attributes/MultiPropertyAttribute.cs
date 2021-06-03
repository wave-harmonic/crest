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
    using System.Reflection;
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
        /// A new control rectangle is required. Only override as false if the attribute needs to create it itself.
        /// See the embedded attribute as an example.
        /// </summary>
        internal virtual bool NeedsControlRectangle => true;
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
        readonly float power;

        public RangeAttribute(float minimum, float maximum, float power = 1f)
        {
            this.minimum = minimum;
            this.maximum = maximum;
            this.power = power;
        }

#if UNITY_EDITOR
        static MethodInfo _powerSliderMethod;

        static internal void PowerSlider(Rect position, SerializedProperty property, float minimum, float maximum, float power, GUIContent label)
        {
            if (_powerSliderMethod == null)
            {
                // Grab the internal PowerSlider method.
                _powerSliderMethod = typeof(EditorGUI).GetMethod
                (
                    name: "PowerSlider",
                    bindingAttr: BindingFlags.NonPublic | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(Rect), typeof(GUIContent), typeof(float), typeof(float), typeof(float), typeof(float) },
                    modifiers: null
                );
            }

            // Render slider and apply value to SerializedProperty.
            label = EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            float newValue = (float)_powerSliderMethod.Invoke(null, new object[] { position, label, property.floatValue, minimum, maximum, power });
            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = newValue;
            }
            EditorGUI.EndProperty();
        }

        internal override void OnGUI(Rect position, SerializedProperty property, GUIContent label, DecoratedDrawer drawer)
        {
            // Power provided so use PowerSlider.
            if (power != 1f)
            {
                if (property.propertyType != SerializedPropertyType.Float)
                {
                    // We could fallback to Slider, but better to raise an issue.
                    EditorGUI.LabelField(position, label.text, "Range: must be float if power is provided.");
                    return;
                }

                PowerSlider(position, property, minimum, maximum, power, label);
                return;
            }

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
