// Crest Ocean System

// Adapted from: https://forum.unity.com/threads/drawing-a-field-using-multiple-property-drawers.479377/

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
                    a.OnGUI(a.NeedsControlRectangle ? EditorGUILayout.GetControlRect(true) : position, property, a.BuildLabel(label), this);
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
    }
}

#endif
