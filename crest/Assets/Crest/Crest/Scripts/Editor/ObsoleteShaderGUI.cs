// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Adds a deprecated message to shaders.
    ///
    /// USAGE
    /// Add to bottom of Shader block:
    /// CustomEditor "Crest.ObsoleteShaderGUI"
    /// Optionally add to Properties block:
    /// [HideInInspector] _ObsoleteMessage("The additional message.", Float) = 0
    /// </summary>
    public class ObsoleteShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            // Enable rich text in help boxes. Store original so we can revert since this might be a "hack".
            var styleRichText = GUI.skin.GetStyle("HelpBox").richText;
            GUI.skin.GetStyle("HelpBox").richText = true;

            var message = "This shader is deprecated and will be removed in a future version.";

            {
                var property = FindProperty("_ObsoleteMessage", properties, propertyIsMandatory: false);
                if (property != null)
                {
                    message += " " + property.displayName;
                }
            }

            EditorGUILayout.HelpBox(message, MessageType.Warning);
            EditorGUILayout.Space(3f);

            // Revert skin since it persists.
            GUI.skin.GetStyle("HelpBox").richText = styleRichText;

            // Render the default GUI.
            base.OnGUI(editor, properties);
        }
    }
}
