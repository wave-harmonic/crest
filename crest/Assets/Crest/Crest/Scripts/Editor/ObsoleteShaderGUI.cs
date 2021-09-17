// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using UnityEditor;

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
            var message = "This shader is deprecated and will be removed in a future version.";

            {
                var property = FindProperty("_ObsoleteMessage", properties, propertyIsMandatory: false);
                if (property != null)
                {
                    message += " " + property.displayName;
                }
            }

            EditorGUILayout.HelpBox(message, MessageType.Warning);

            // Render the default GUI.
            base.OnGUI(editor, properties);
        }
    }
}
