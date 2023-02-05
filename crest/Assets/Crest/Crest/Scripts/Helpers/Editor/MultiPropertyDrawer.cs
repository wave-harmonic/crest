// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Adapted from:
// https://forum.unity.com/threads/drawing-a-field-using-multiple-property-drawers.479377/

// This class draws all the attributes which inherit from DecoratedPropertyAttribute. This class may need to be
// extended to handle reseting GUI states as we need them.

#if UNITY_EDITOR

namespace Crest.EditorHelpers
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(DecoratedPropertyAttribute), true)]
    public class DecoratedDrawer : PropertyDrawer
    {
        internal static bool s_HideInInspector = false;

        List<object> _decorators = null;
        List<object> Decorators
        {
            get
            {
                // Populate list with decorators.
                if (_decorators == null)
                {
                    // TODO: Use something other than Linq.
                    _decorators = fieldInfo
                        .GetCustomAttributes(typeof(DecoratorAttribute), false)
                        .OrderBy(x => ((DecoratorAttribute) x).order).ToList();
                }

                return _decorators;
            }
        }

        List<OnChangeAttribute> _onChangeAttributes = null;
        List<OnChangeAttribute> OnChangeAttributes
        {
            get
            {
                if (_onChangeAttributes == null)
                {
                    // TODO: Use something other than Linq.
                    _onChangeAttributes = fieldInfo
                        .GetCustomAttributes(typeof(OnChangeAttribute), false)
                        .Cast<OnChangeAttribute>()
                        .ToList();
                }

                return _onChangeAttributes;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Make original control rectangle be invisible because we always create our own.
            return 0;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Store the original GUI state so it can be reset later.
            var originalColor = GUI.color;
            var originalEnabled = GUI.enabled;

            for (var index = 0; index < Decorators.Count; index++)
            {
                var attribute = (DecoratorAttribute)Decorators[index];
                attribute.Decorate(position, property, attribute.BuildLabel(label), this);
            }

            if (!s_HideInInspector)
            {
                var a = (DecoratedPropertyAttribute) attribute;
                try
                {
                    EditorGUI.BeginChangeCheck();

                    a.OnGUI(a.NeedsControlRectangle ? EditorGUILayout.GetControlRect(true) : position, property, a.BuildLabel(label), this);

                    if (EditorGUI.EndChangeCheck())
                    {
                        OnChange(property);
                    }
                }
                catch (System.ArgumentException)
                {
                    Debug.LogError
                    (
                        $"Crest: Property <i>{property.displayName}</i> on <i>{property.serializedObject.targetObject.name}</i> " +
                        "has a multi-property attribute which requires a custom editor.",
                        property.serializedObject.targetObject
                    );
                }
            }

            // Handle resetting the GUI state.
            s_HideInInspector = false;
            GUI.color = originalColor;
            GUI.enabled = originalEnabled;
        }

        public void OnChange(SerializedProperty property)
        {
            if (OnChangeAttributes.Count == 0)
            {
                return;
            }

            // Apply any changes.
            property.serializedObject.ApplyModifiedProperties();

            var target = property.serializedObject.targetObject;
            var type = target.GetType();

            foreach (var attribute in OnChangeAttributes)
            {
                var method = type.GetMethod(attribute.Method, Helpers.s_AnyMethod);

#if CREST_DEBUG
                if (method == null)
                {
                    Debug.LogError($"Crest: <i>{attribute.GetType().Name}</i> could not find the method <i>{attribute.Method}</i>", target);
                    continue;
                }

                if (method.GetParameters().Length > 0)
                {
                    Debug.LogError($"Crest: <i>{attribute.GetType().Name}</i> could not invoke <i>{attribute.Method}</i> as it has more than zero parameters.", target);
                    continue;
                }
#endif

                method?.Invoke(target, new object[] {});
            }
        }
    }
}

#endif
