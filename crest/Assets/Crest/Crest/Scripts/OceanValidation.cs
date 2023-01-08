﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// How to use:
// Create a custom editor that inherits from ValidatedEditor. Then implement IValidated on the component.

#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Crest.EditorHelpers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Crest
{
    using ValidationFixFunc = System.Action<SerializedObject>;

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
            public string _fixDescription;
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

        public delegate void ShowMessage(string message, string fixDescription, MessageType type, Object @object = null, ValidationFixFunc action = null);

        public static void DebugLog(string message, string fixDescription, MessageType type, Object @object = null, ValidationFixFunc action = null)
        {
            // Never log info validation to console.
            if (type == MessageType.Info)
            {
                return;
            }

            message = $"Crest Validation: {message} {fixDescription} Click this message to highlight the problem object.";

            switch (type)
            {
                case MessageType.Error: Debug.LogError(message, @object); break;
                case MessageType.Warning: Debug.LogWarning(message, @object); break;
                default: Debug.Log(message, @object); break;
            }
        }

        public static void HelpBox(string message, string fixDescription, MessageType type, Object @object = null, ValidationFixFunc action = null)
        {
            messages[(int)type].Add(new HelpBoxMessage { _message = message, _fixDescription = fixDescription, _object = @object, _action = action });
        }

        public static void Suppressed(string message, string fixDescription, MessageType type, Object @object = null, ValidationFixFunc action = null)
        {
        }

        internal static void FixAttachComponent<ComponentType>(SerializedObject componentOrGameObject)
            where ComponentType : Component
        {
            Undo.AddComponent<ComponentType>(EditorHelpers.EditorHelpers.GetGameObject(componentOrGameObject));
        }

        internal static void FixSetMaterialOptionEnabled(SerializedObject material, string keyword, string floatParam, bool enabled)
        {
            var mat = material.targetObject as Material;
            Undo.RecordObject(mat, $"Enable keyword {keyword}");
            mat.SetKeyword(keyword, enabled);
            mat.SetFloat(floatParam, enabled ? 1f : 0f);
        }

        internal static void FixSetMaterialIntProperty(SerializedObject material, string label, string intParam, int value)
        {
            var mat = material.targetObject as Material;
            Undo.RecordObject(mat, $"change {label}");
            mat.SetInt(intParam, value);
        }

        static void FixRemoveRenderer(SerializedObject componentOrGameObject)
        {
            // We will either get the component or the GameObject it is attached to.
            var gameObject = EditorHelpers.EditorHelpers.GetGameObject(componentOrGameObject);
            var renderer = gameObject.GetComponent<MeshRenderer>();
            Undo.DestroyObjectImmediate(renderer);
            EditorUtility.SetDirty(gameObject);
        }

        public static void FixAddMissingMathPackage(SerializedObject componentOrGameObject)
        {
            PackageManagerHelpers.AddMissingPackage("com.unity.mathematics");
        }

        public static void FixAddMissingBurstPackage(SerializedObject componentOrGameObject)
        {
            PackageManagerHelpers.AddMissingPackage("com.unity.burst");
        }

        public static bool ValidateNoScale(Object @object, Transform transform, ShowMessage showMessage)
        {
            if (transform.lossyScale != Vector3.one)
            {
                showMessage
                (
                    $"There must be no scale on the <i>{@object.GetType().Name}</i> Transform or any of its parents." +
                    $"The current scale is <i>{transform.lossyScale}</i>.",
                    "Reset the scale on this Transform and all parents to one.",
                    MessageType.Error, @object
                );

                return false;
            }

            return true;
        }

        public static bool ValidateNoRotation(Object @object, Transform transform, ShowMessage showMessage)
        {
            if (transform.eulerAngles.magnitude > 0.0001f)
            {
                showMessage
                (
                    $"There must be no rotation on the <i>{@object.GetType().Name}</i> Transform or any of its parents." +
                    $"The current rotation is <i>{transform.eulerAngles}.</i>",
                    "Reset the rotation on this Transform and all parents to zero.",
                    MessageType.Error, @object
                );

                return false;
            }

            return true;
        }

        public static bool ValidateRendererLayer(GameObject gameObject, ShowMessage showMessage, OceanRenderer ocean)
        {
            if (ocean != null && gameObject.layer != ocean.Layer)
            {
                var layerName = LayerMask.LayerToName(ocean.Layer);
                showMessage
                (
                    $"The layer is not the same as the <i>OceanRenderer.Layer ({layerName})</i> which can cause problems if the <i>{layerName}</i> layer is excluded from any culling masks.",
                    $"Set layer to <i>{layerName}</i>.",
                    MessageType.Warning, gameObject, x =>
                    {
                        Undo.RecordObject(gameObject, $"Change Layer to {layerName}");
                        gameObject.layer = ocean.Layer;
                    }
                );
            }

            // Is valid as not outright invalid but could be.
            return true;
        }

        public static bool ValidateRenderer<T>(GameObject gameObject, ShowMessage showMessage, string shaderPrefix) where T : Renderer
        {
            return ValidateRenderer<T>(gameObject, showMessage, isRendererRequired: true, isRendererOptional: false, shaderPrefix);
        }

        public static bool ValidateRenderer<T>(GameObject gameObject, ShowMessage showMessage, bool isRendererRequired, bool isRendererOptional, string shaderPrefix = null) where T : Renderer
        {
            gameObject.TryGetComponent<T>(out var renderer);

            if (isRendererRequired)
            {
                if (renderer == null)
                {
                    // If renderer is optional, then a different error message with all the optionals should be presented.
                    if (isRendererOptional)
                    {
                        return true;
                    }

                    var type = typeof(T);
                    var name = type.Name;

                    // Give users a hint as to what "Renderer" really means.
                    if (type == typeof(Renderer))
                    {
                        name += " (Mesh, Trail etc)";
                    }

                    showMessage
                    (
                        $"A <i>{name}</i> component is required but none is attached to ocean input.",
                        "Attach a <i>MeshRenderer</i> component.",
                        MessageType.Error, gameObject,
                        FixAttachComponent<MeshRenderer>
                    );

                    return false;
                }
                else if (!ValidateMaterial(gameObject, showMessage, renderer.sharedMaterial, shaderPrefix))
                {
                    return false;
                }
            }
            else
            {
                if (renderer)
                {
                    showMessage
                    (
                        "A <i>MeshRenderer</i> is present but is unused and should be removed.",
                        "Remove the <i>MeshRenderer</i> component.",
                        MessageType.Warning, gameObject,
                        FixRemoveRenderer
                    );

                    return false;
                }

                return true;
            }

            return true;
        }

        public static bool ValidateMaterial(GameObject gameObject, ShowMessage showMessage, Material material, string shaderPrefix)
        {
            if (shaderPrefix == null && material == null)
            {
                showMessage
                (
                    $"<i>Mesh Renderer</i> requires a material.",
                    "Assign a material.",
                    MessageType.Error, gameObject
                );

                return false;
            }

            if (!material || material.shader && (!material.shader.name.StartsWithNoAlloc(shaderPrefix) && !material.shader.name.Contains("/All/")))
            {
                showMessage
                (
                    $"Shader assigned to ocean input expected to be of type <i>{shaderPrefix}</i>.",
                    "Assign a material that uses a shader of this type.",
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
            if (!(this.target is IValidated))
            {
                return;
            }

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
            // component. Get instance using StageUtility so it searches only within the prefab in prefab mode instead
            // of the opened scene which is incorrect.
            target.Validate(StageUtility.GetCurrentStageHandle().FindComponentOfType<OceanRenderer>(), ValidatedHelper.HelpBox);

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

                            var fixDescription = message._fixDescription;
                            var originalGUIEnabled = GUI.enabled;

                            if (message._action != null)
                            {
                                fixDescription += " Click the fix/repair button on the right to fix.";

                                if ((message._action == ValidatedHelper.FixAddMissingMathPackage || message._action == ValidatedHelper.FixAddMissingBurstPackage) && PackageManagerHelpers.IsBusy)
                                {
                                    GUI.enabled = false;
                                }
                            }

                            EditorGUILayout.HelpBox($"{message._message} {fixDescription}", messageType);

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
                                if (s_fixButtonContent == null)
                                {
                                    s_fixButtonContent = new GUIContent(EditorGUIUtility.FindTexture("SceneViewTools@2x"));
                                }

                                if (message._fixDescription != null)
                                {
                                    var sanitisedFixDescr = Regex.Replace(message._fixDescription, @"<[^<>]*>", "'");
                                    s_fixButtonContent.tooltip = $"Apply fix: {sanitisedFixDescr}";
                                }
                                else
                                {
                                    s_fixButtonContent.tooltip = "Fix issue";
                                }

                                if (GUILayout.Button(s_fixButtonContent, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true)))
                                {
                                    // Run fix function
                                    var serializedObject = new SerializedObject(message._object);
                                    message._action.Invoke(serializedObject);
                                    if (serializedObject.ApplyModifiedProperties())
                                    {
                                        // SerializedObject does this for us, but gives the history item a nicer label.
                                        Undo.RecordObject(message._object, s_fixButtonContent.tooltip);
                                    }
                                }
                            }

                            GUI.enabled = originalGUIEnabled;

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
