// Crest Ocean System

// Adapted from: https://forum.unity.com/threads/drawing-a-field-using-multiple-property-drawers.479377/

// This class draws all the attributes which inherit from DecoratedPropertyAttribute. This class may need to be
// extended to handle reseting GUI states as we need them.

// NOTE:
// If you come across the following exception:
// > ArgumentException: Getting control 1's position in a group with only 1 controls when doing repaint.
// Then the problem component needs a custom editor. It can be simlple as adding:
// > [CustomEditor(typeof(YourComponent), editorForChildClasses: true), CanEditMultipleObjects]
// > class YourComponentEditor : Editor { }

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

            var a = (DecoratedPropertyAttribute) attribute;
            // See note at top of file if exception is thrown here.
            a.OnGUI(a.NeedsControlRectangle ? EditorGUILayout.GetControlRect(true) : position, property, a.BuildLabel(label), this);

            // Handle resetting the GUI state.
            GUI.color = originalColor;
            GUI.enabled = originalEnabled;
        }
    }
}

#endif