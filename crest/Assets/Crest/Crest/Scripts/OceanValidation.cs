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
    using ValidationFixFunc = System.Func<SerializedObject, string>;

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

        public struct HelpBoxMessage
        {
            public string _message;
            public Object _object;
            public ValidationFixFunc _action;
        }

        // This is a shared resource. It will be cleared before use. It is only used by the HelpBox delegate since we
        // want to group them by severity (MessageType). Make sure length matches MessageType length.
        public static readonly List<HelpBoxMessage>[] messages = new[]
        {
            new List<HelpBoxMessage>(),
            new List<HelpBoxMessage>(),
            new List<HelpBoxMessage>(),
        };

        public delegate void ShowMessage(string message, MessageType type, Object @object = null, ValidationFixFunc action = null);

        public static void DebugLog(string message, MessageType type, Object @object = null, ValidationFixFunc action = null)
        {
            message = $"Validation: {message} Click this message to highlight the problem object.";

            switch (type)
            {
                case MessageType.Error: Debug.LogError(message, @object); break;
                case MessageType.Warning: Debug.LogWarning(message, @object); break;
                default: Debug.Log(message, @object); break;
            }
        }

        public static void HelpBox(string message, MessageType type, Object @object = null, ValidationFixFunc action = null)
        {
            messages[(int)type].Add(new HelpBoxMessage { _message = message, _object = @object, _action = action });
        }

        public static void Suppressed(string message, MessageType type, Object @object = null, ValidationFixFunc action = null)
        {
        }

        static string FixAttachRenderer(SerializedObject lodInputComponent)
        {
            if (lodInputComponent != null)
            {
                var gameObject = lodInputComponent.targetObject as GameObject;
                gameObject.AddComponent<MeshRenderer>();
                EditorUtility.SetDirty(gameObject);
            }
            return "Attach MeshRenderer component";
        }

        public static bool ValidateRenderer(GameObject gameObject, string shaderPrefix, ShowMessage showMessage)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (!renderer)
            {
                showMessage
                (
                    "No renderer has been attached to ocean input. A renderer is required.",
                    MessageType.Error, gameObject, FixAttachRenderer
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
        static GUIContent s_jumpButtonContent = null;
        static GUIContent s_fixButtonContent = null;

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
                        var joinedMessage = messages[0]._message;
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
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.HelpBox(message._message, messageType);

                            // Jump to object button.
                            if (message._object != null)
                            {
                                // Selection.activeObject can be message._object.gameObject instead of the component
                                // itself. We soft cast to MonoBehaviour to get the gameObject for comparison.
                                // Alternatively, we could always pass gameObject instead of "this".
                                var casted = message._object as MonoBehaviour;

                                if (Selection.activeObject != message._object && (casted == null || casted.gameObject != Selection.activeObject))
                                {
                                    if (s_jumpButtonContent == null)
                                    {
                                        s_jumpButtonContent = new GUIContent(EditorGUIUtility.FindTexture("scenepicking_pickable_hover@2x"), "Jump to object to resolve issue");
                                    }

                                    if (GUILayout.Button(s_jumpButtonContent, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
                                    {
                                        Selection.activeObject = message._object;
                                    }
                                }
                            }

                            // Fix the issue button.
                            if (message._action != null)
                            {
                                // Call fix function with null argument to retrieve the resolution info
                                string resolution = message._action.Invoke(null);
                                if (s_fixButtonContent == null)
                                {
                                    s_fixButtonContent = new GUIContent(EditorGUIUtility.FindTexture("SceneViewTools@2x"));
                                }
                                s_fixButtonContent.tooltip = $"Fix: {resolution}";

                                if (GUILayout.Button(s_fixButtonContent, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
                                {
                                    // Fix for real
                                    var serializedObject = new SerializedObject(message._object);
                                    message._action.Invoke(serializedObject);
                                    if (serializedObject.ApplyModifiedProperties())
                                    {
                                        // SerializedObject does this for us, but gives the history item a nicer label.
                                        Undo.RecordObject(message._object, resolution);
                                    }
                                }
                            }

                            EditorGUILayout.EndHorizontal();
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
