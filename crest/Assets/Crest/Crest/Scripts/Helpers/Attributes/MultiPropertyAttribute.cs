// Crest Ocean System

// Taken and modified from: https://forum.unity.com/threads/drawing-a-field-using-multiple-property-drawers.479377/

// This class allows multiple attributes to be used together providing they inherit from this class. It also eliminates
// the requirement to create a custom property drawer as the MultiPropertyDrawer class handles all of that for us. The
// implementation difference when creating a custom attribute is to place the property drawer code in the attribute
// instead. Also, any change to the GUI state must not be reset so it can be stacked. The MultiPropertyDrawer class
// resets the GUI state for us.

namespace Crest
{
    using UnityEngine;

#if UNITY_EDITOR
    using Crest.EditorHelpers;
    using UnityEditor;
#endif

    public abstract class MultiPropertyAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        /// <summary>
        /// Override this method to customise the label.
        /// </summary>
        internal virtual GUIContent BuildLabel(GUIContent label) => label;

        /// <summary>
        /// Override this method to make your own IMGUI based GUI for the property.
        /// </summary>
        internal abstract void OnGUI(Rect position, SerializedProperty property, GUIContent label, MultiPropertyDrawer drawer);

        /// <summary>
        /// Override this method to specify how tall the GUI for this field is in pixels.
        /// </summary>
        internal virtual float? GetPropertyHeight(SerializedProperty property, GUIContent label) => null;
#endif
    }
}
