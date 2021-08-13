// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
#if UNITY_EDITOR
    using Crest.EditorHelpers;
    using UnityEditor;
#endif
    using UnityEngine;

    public class HelpBoxAttribute : DecoratorAttribute
    {
        // Define our own as Unity's won't be available in builds.
        public enum MessageType
        {
            Info,
            Warning,
            Error,
        }

        public string message;
        public MessageType messageType;
        public Visibility visibility;

        public enum Visibility
        {
            Always,
            PropertyEnabled,
            PropertyDisabled,
        }

#if UNITY_EDITOR
        GUIContent guiContent;
#endif

        public HelpBoxAttribute(string message, MessageType messageType = MessageType.Info, Visibility visibility = Visibility.Always)
        {
            this.message = message;
            this.messageType = messageType;
            this.visibility = visibility;
#if UNITY_EDITOR
            guiContent = new GUIContent(message);
#endif
        }

#if UNITY_EDITOR
        internal override void Decorate(Rect position, SerializedProperty property, GUIContent label, DecoratedDrawer drawer)
        {
            if (visibility == Visibility.PropertyEnabled && !GUI.enabled || visibility == Visibility.PropertyDisabled && GUI.enabled)
            {
                return;
            }

            // Enable rich text in help boxes. Store original so we can revert since this might be a "hack".
            var style = GUI.skin.GetStyle("HelpBox");
            var styleRichText = style.richText;
            style.richText = true;

            var height = style.CalcHeight(guiContent, EditorGUIUtility.currentViewWidth);
            if (height <= EditorGUIUtility.singleLineHeight)
            {
                // This gets internal layout of the help box right but breaks down if multiline.
                height += style.padding.horizontal + style.lineHeight;
            }

            // Always get a new control rect so we don't have to deal with positions and offsets.
            position = EditorGUILayout.GetControlRect(true, height, style);
            // + 1 maps our MessageType to Unity's.
            EditorGUI.HelpBox(position, message, (UnityEditor.MessageType)messageType + 1);

            // Revert skin since it persists.
            style.richText = styleRichText;
        }
#endif
    }
}
