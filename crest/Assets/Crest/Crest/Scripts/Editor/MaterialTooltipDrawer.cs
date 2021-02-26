// Crest Ocean System

// Adapted from: https://raw.githubusercontent.com/supyrb/ConfigurableShaders/master/Scripts/Editor/MaterialTooltipDrawer.cs
// License: https://github.com/supyrb/ConfigurableShaders/blob/master/LICENSE.md

namespace Crest.Editor
{
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Draws a tooltip for material properties. <see cref="MaterialCrestTooltipDrawer"/>
    /// Usage: CrestTooltip(Write your tooltip here without quotes. Only special characters allowed are periods.)
    /// </summary>
    public class MaterialCrestTooltipDrawer : MaterialPropertyDrawer
    {
        protected string tooltip;
        protected GUIContent guiContent;
        MethodInfo DefaultShaderPropertyInternal;
        object[] methodArguments = new object[3];

        public MaterialCrestTooltipDrawer()
        {
        }

        public MaterialCrestTooltipDrawer(string tooltip)
        {
            // Use a prefix to determine parameter purpose.
            if (tooltip.StartsWith("_Tooltip") || !tooltip.StartsWith("_"))
            {
                this.tooltip = tooltip.Replace("_Tooltip ", "");
            }

            guiContent = new GUIContent(string.Empty, this.tooltip);

            // Use reflection to get the internal method.
            var methodArgumentTypes = new[] { typeof(Rect), typeof(MaterialProperty), typeof(GUIContent) };
            DefaultShaderPropertyInternal = typeof(MaterialEditor).GetMethod("DefaultShaderPropertyInternal",
                BindingFlags.Instance | BindingFlags.NonPublic, null, methodArgumentTypes, null);
        }

        public static string ExtractTooltip(string p1, string p2)
        {
            return p1.StartsWith("_Tooltip ") ? p1 : p2;
        }

        public override void OnGUI(Rect position, MaterialProperty property, string label, MaterialEditor editor)
        {
            guiContent.text = label;

            if (DefaultShaderPropertyInternal != null)
            {
                // I don't know why, but just calling this here will influence spacing. Must be setting internal GUI state.
                EditorGUILayout.GetControlRect(true, MaterialEditor.GetDefaultPropertyHeight(property) - 20, EditorStyles.layerMaskField);

                methodArguments[0] = position;
                methodArguments[1] = property;
                methodArguments[2] = guiContent;

                switch (property.type)
                {
                    case MaterialProperty.PropType.Texture:
                        editor.TextureProperty(position, property, label, tooltip, !property.flags.HasFlag(MaterialProperty.PropFlags.NoScaleOffset));
                        break;
                    case MaterialProperty.PropType.Vector:
                        VectorProperty(position, property, guiContent);
                        break;
                    default: // Range, Float, Color etc
                        DefaultShaderPropertyInternal.Invoke(editor, methodArguments);
                        break;
                }
            }
        }

        // The already defined VectorProperty method strips the tooltip away.
        Vector4 VectorProperty(Rect position, MaterialProperty prop, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            // We want to make room for the field in case it's drawn on the same line as the label
            // Set label width to default width (zero) temporarily
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;

            Vector4 newValue = EditorGUI.Vector4Field(position, label, prop.vectorValue);

            EditorGUIUtility.labelWidth = oldLabelWidth;

            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.vectorValue = newValue;

            return prop.vectorValue;
        }
    }

    // There can only be one property drawer per property. We have to redefine the built-in drawers to include the
    // tooltip parameter. The list of drawers and thier source code:
    // ToggleDrawer, EnumDrawer, KeywordEnumDrawer, PowerSliderDrawer, IntRangeDrawer
    // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/MaterialPropertyDrawer.cs

    // Adapted from: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/MaterialPropertyDrawer.cs#L323
    internal class MaterialCrestToggleDrawer : MaterialCrestTooltipDrawer
    {
        protected readonly string keyword;

        public MaterialCrestToggleDrawer()
        {
        }

        public MaterialCrestToggleDrawer(string p1) : base(p1)
        {
            // Use a prefix to determine parameter purpose.
            if (p1.StartsWith("_Keyword "))
            {
                keyword = p1.Replace("_Keyword ", "");
            }
        }

        public MaterialCrestToggleDrawer(string p1, string p2) : base(ExtractTooltip(p1, p2))
        {
            // Use a prefix to determine parameter purpose.
            keyword = p1.StartsWith("_Keyword ") ? p1 : p2;
            keyword = keyword.Replace("_Keyword ", "");
        }

        static bool IsPropertyTypeSuitable(MaterialProperty prop)
        {
            return prop.type == MaterialProperty.PropType.Float || prop.type == MaterialProperty.PropType.Range;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            label.tooltip = tooltip;

            EditorGUI.BeginChangeCheck();

            bool value = (Mathf.Abs(prop.floatValue) > 0.001f);
            EditorGUI.showMixedValue = prop.hasMixedValue;
            value = EditorGUI.Toggle(position, label, value);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = value ? 1.0f : 0.0f;
                SetKeyword(prop, value);
            }
        }

        public override void Apply(MaterialProperty prop)
        {
            base.Apply(prop);
            if (!IsPropertyTypeSuitable(prop))
                return;

            if (prop.hasMixedValue)
                return;

            SetKeyword(prop, (Mathf.Abs(prop.floatValue) > 0.001f));
        }

        void SetKeyword(MaterialProperty prop, bool on)
        {
            SetKeywordInternal(prop, on, "_ON");
        }

        protected void SetKeywordInternal(MaterialProperty prop, bool on, string defaultKeywordSuffix)
        {
            // if no keyword is provided, use <uppercase property name> + defaultKeywordSuffix
            string kw = string.IsNullOrEmpty(keyword) ? prop.name.ToUpperInvariant() + defaultKeywordSuffix : keyword;
            // set or clear the keyword
            foreach (Material material in prop.targets)
            {
                if (on)
                    material.EnableKeyword(kw);
                else
                    material.DisableKeyword(kw);
            }
        }
    }
}