// Crest Ocean System

// Adapted from: https://forum.unity.com/threads/drawing-a-field-using-multiple-property-drawers.479377/

// This class draws all the attributes which inherit from MultiPropertyAttribute. This class may need to be extended to
// handle reseting GUI states as we need them.

#if UNITY_EDITOR

namespace Crest.EditorHelpers
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(MultiPropertyAttribute), true)]
    public class MultiPropertyDrawer : PropertyDrawer
    {
        List<object> _multiPropertyAttributes = new List<object>();
        List<object> MultiPropertyAttributes
        {
            get
            {
                // Populate list with attributes (MultiPropertyAttribute) if empty. There should at least be one since
                // MultiPropertyDrawer targets MultiPropertyAttribute.
                if (_multiPropertyAttributes == null || _multiPropertyAttributes.Count == 0)
                {
                    // TODO: Use something other than Linq.
                    _multiPropertyAttributes = fieldInfo.GetCustomAttributes(typeof(MultiPropertyAttribute), false)
                        .OrderBy(x => ((PropertyAttribute) x).order).ToList();

                }

                return _multiPropertyAttributes;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = base.GetPropertyHeight(property, label);

            // Go through the attributes, and try to get an altered height. If no altered height, then return the
            // default height.
            foreach (MultiPropertyAttribute attribute in MultiPropertyAttributes)
            {
                // TODO: Build label here too?
                var temporaryHeight = attribute.GetPropertyHeight(property, label);
                if (temporaryHeight.HasValue)
                {
                    height = temporaryHeight.Value;
                    break;
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Store the original GUI state so it can be reset later.
            var originalColor = GUI.color;
            var originalEnabled = GUI.enabled;

            for (var index = 0; index < MultiPropertyAttributes.Count; index++)
            {
                var attribute = (MultiPropertyAttribute)MultiPropertyAttributes[index];
                var isLast = index == MultiPropertyAttributes.Count - 1;
                attribute.OnGUI(position, property, attribute.BuildLabel(label), this, isLast);
            }

            // Handle resetting the GUI state.
            GUI.color = originalColor;
            GUI.enabled = originalEnabled;
        }
    }
}

#endif