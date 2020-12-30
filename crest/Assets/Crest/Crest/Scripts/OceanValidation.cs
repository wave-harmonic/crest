// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// How to use:
// Create a custom editor that inherits from ValidatedEditor. Then implement IValidated on the component.

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Crest
{
    public interface IValidated
    {
        bool Validate(OceanRenderer ocean, ValidatedHelper.ShowMessage showMessage);
    }

    // Holds the shared list for messages
    public static class ValidatedHelper
    {
        public enum MessageType
        {
            Error,
            Warning,
            Info,
        }

        // This is a shared resource. It will be cleared before use. It is only used by the HelpBox delegate since we 
        // want to group them by severity (MessageType). Make sure length matches MessageType length.
        public static readonly List<string>[] messages = new[]
        {
            new List<string>(),
            new List<string>(),
            new List<string>(),
        };

        public delegate void ShowMessage(string message, MessageType type, Object @object = null);

        public static void DebugLog(string message, MessageType type, Object @object = null)
        {
            message = $"Validation: {message} Click this message to highlight the problem object.";

            switch (type)
            {
                case MessageType.Error: Debug.LogError(message, @object); break;
                case MessageType.Warning: Debug.LogWarning(message, @object); break;
                default: Debug.Log(message, @object); break;
            }
        }

        public static void HelpBox(string message, MessageType type, Object @object = null)
        {
            messages[(int)type].Add(message);
        }

        public static bool ValidateRenderer(GameObject gameObject, string shaderPrefix, ShowMessage showMessage)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (!renderer)
            {
                showMessage
                (
                    "No renderer has been attached to ocean input. A renderer is required.",
                    MessageType.Error, gameObject
                );

                return false;
            }

            if (!renderer.sharedMaterial || renderer.sharedMaterial.shader && !renderer.sharedMaterial.shader.name.StartsWith(shaderPrefix))
            {
                showMessage
                (
                    $"Shader assigned to ocean input expected to be of type <i>{shaderPrefix}</i>.",
                    MessageType.Error, gameObject
                );

                return false;
            }

            return true;
        }
    }

    public abstract class ValidatedEditor : Editor
    {
        static readonly bool _groupMessages = false;

        public void ShowValidationMessages()
        {
            IValidated target = (IValidated)this.target;

            // Enable rich text in help boxes. Store original so we can revert since this might be a "hack".
            var styleRichText = GUI.skin.GetStyle("HelpBox").richText;
            GUI.skin.GetStyle("HelpBox").richText = true;

            // This is a static list so we need to clear it before use. Not sure if this will ever be a threaded
            // operation which would be an issue.
            foreach (var messages in ValidatedHelper.messages)
            {
                messages.Clear();
            }

            // OceanRenderer isn't a hard requirement for validation to work. Null needs to be handled in each
            // component.
            target.Validate(FindObjectOfType<OceanRenderer>(), ValidatedHelper.HelpBox);

            // We only want space before and after the list of help boxes. We don't want space between.
            var needsSpaceAbove = true;
            var needsSpaceBelow = false;

            // We loop through in reverse order so errors appears at the top.
            for (var messageTypeIndex = 0; messageTypeIndex < ValidatedHelper.messages.Length; messageTypeIndex++)
            {
                var messages = ValidatedHelper.messages[messageTypeIndex];

                if (messages.Count > 0)
                {
                    if (needsSpaceAbove)
                    {
                        // Double space looks good at top.
                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        needsSpaceAbove = false;
                    }

                    needsSpaceBelow = true;

                    // Map Validated.MessageType to HelpBox.MessageType.
                    var messageType = (MessageType)ValidatedHelper.messages.Length - messageTypeIndex;

                    if (_groupMessages)
                    {
                        // We join the messages together to reduce vertical space since HelpBox has padding, borders etc.
                        var joinedMessage = messages[0];
                        // Format as list if we have more than one message.
                        if (messages.Count > 1) joinedMessage = $"- {joinedMessage}";

                        for (var messageIndex = 1; messageIndex < messages.Count; messageIndex++)
                        {
                            joinedMessage += $"\n- {messages[messageIndex]}";
                        }

                        EditorGUILayout.HelpBox(joinedMessage, messageType);
                    }
                    else
                    {
                        foreach (var message in messages)
                        {
                            EditorGUILayout.HelpBox(message, messageType);
                        }
                    }
                }
            }

            if (needsSpaceBelow)
            {
                EditorGUILayout.Space();
            }

            // Revert skin since it persists.
            GUI.skin.GetStyle("HelpBox").richText = styleRichText;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // We want to show messages at the bottom or it will disturb input focus.
            ShowValidationMessages();
        }
    }
}

#endif
