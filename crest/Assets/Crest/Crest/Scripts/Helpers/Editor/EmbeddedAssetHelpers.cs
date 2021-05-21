// Crest Ocean System

// Lovingly adapted from Cinemachine - https://raw.githubusercontent.com/Unity-Technologies/upm-package-cinemachine/master/Editor/Utility/EmbeddedAssetHelpers.cs
// Unity Companion License: https://github.com/Unity-Technologies/upm-package-cinemachine/blob/master/LICENSE.md

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.VersionControl;

namespace Crest.EditorHelpers
{
    /// <summary>
    /// Helper for drawing embedded asset editors
    /// </summary>
    internal class EmbeddedAssetEditor
    {
        /// <summary>
        /// Create in OnEnable()
        /// </summary>
        public EmbeddedAssetEditor()
        {
            m_CreateButtonGUIContent = new GUIContent(
                    "Create Asset", "Create a new shared settings asset");
        }

        /// <summary>
        /// Called after the asset editor is created, in case it needs
        /// to be customized
        /// </summary>
        public OnCreateEditorDelegate OnCreateEditor;
        public delegate void OnCreateEditorDelegate(UnityEditor.Editor editor);

        /// <summary>
        /// Called when the asset being edited was changed by the user.
        /// </summary>
        public OnChangedDelegate OnChanged;
        public delegate void OnChangedDelegate(System.Type type, Object obj);

        /// <summary>
        /// Free the resources in OnDisable()
        /// </summary>
        public void OnDisable()
        {
            DestroyEditor();
        }

        /// <summary>
        /// Customize this after creation if you want
        /// </summary>
        public GUIContent m_CreateButtonGUIContent;

        private UnityEditor.Editor m_Editor = null;

        System.Type type;

        const int kIndentOffset = 3;

        public void DrawEditorCombo(GUIContent label, PropertyDrawer drawer, SerializedProperty property, string extension)
        {
            type = drawer.fieldInfo.FieldType;

            DrawEditorCombo(
                label,
                $"Create {property.displayName} Asset",
                $"{property.displayName.Replace(' ', '_')}",
                extension,
                string.Empty,
                false,
                property
            );
        }

        /// <summary>
        /// Call this from OnInspectorGUI.  Will draw the asset reference field, and
        /// the embedded editor, or a Create Asset button, if no asset is set.
        /// </summary>
        public void DrawEditorCombo(
            GUIContent label, string title, string defaultName, string extension, string message, bool indent, SerializedProperty property)
        {
            UpdateEditor(property);

            if (m_Editor == null)
                AssetFieldWithCreateButton(label, property, title, defaultName, extension, message, property.serializedObject);
            else
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                Rect rect = EditorGUILayout.GetControlRect(true);
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.BeginChangeCheck();
                // 2020 needs the label passed through.
                EditorGUI.PropertyField(rect, property, label);
                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                    UpdateEditor(property);
                }
                if (m_Editor != null)
                {
                    Rect foldoutRect = new Rect(
                        rect.x - kIndentOffset, rect.y, rect.width + kIndentOffset, rect.height);
                    property.isExpanded = EditorGUI.Foldout(
                        foldoutRect, property.isExpanded, GUIContent.none, true);

                    bool canEditAsset = AssetDatabase.IsOpenForEdit(m_Editor.target, StatusQueryOptions.UseCachedIfPossible);

                    // We take the current GUI state into account to support attribute stacking.
                    var guiEnabled = GUI.enabled;
                    GUI.enabled = guiEnabled && canEditAsset;

                    if (property.isExpanded)
                    {
                        EditorGUILayout.Separator();
                        EditorGUILayout.HelpBox(
                            "This is a shared asset.  Changes made here will apply to all users of this asset.",
                            MessageType.Info);
                        EditorGUI.BeginChangeCheck();
                        if (indent)
                            ++EditorGUI.indentLevel;
                        m_Editor.OnInspectorGUI();
                        if (indent)
                            --EditorGUI.indentLevel;
                        if (EditorGUI.EndChangeCheck() && (OnChanged != null))
                            OnChanged(type, property.objectReferenceValue);
                    }

                    // Enable GUI so the checkout button works.
                    GUI.enabled = true;

                    if (m_Editor.target != null)
                    {
                        if (!canEditAsset && GUILayout.Button("Check out"))
                        {
                            Task task = Provider.Checkout(AssetDatabase.GetAssetPath(m_Editor.target), CheckoutMode.Asset);
                            task.Wait();
                        }
                    }

                    // Restore stacked GUI enabled state.
                    GUI.enabled = guiEnabled;
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void AssetFieldWithCreateButton(
            GUIContent label,
            SerializedProperty property,
            string title, string defaultName, string extension, string message, SerializedObject serializedObject)
        {
            EditorGUI.BeginChangeCheck();

            float hSpace = 5;
            float buttonWidth = GUI.skin.button.CalcSize(m_CreateButtonGUIContent).x;
            Rect r = EditorGUILayout.GetControlRect(true);
            r.width -= buttonWidth + hSpace;
            // 2020 needs the label passed through.
            EditorGUI.PropertyField(r, property, label);
            r.x += r.width + hSpace; r.width = buttonWidth;
            if (GUI.Button(r, m_CreateButtonGUIContent))
            {
                string newAssetPath = EditorUtility.SaveFilePanelInProject(
                        title, defaultName, extension, message);
                if (!string.IsNullOrEmpty(newAssetPath))
                {
                    var asset = ScriptableObjectUtility.CreateAt(type, newAssetPath);
                    property.objectReferenceValue = asset;
                    serializedObject.ApplyModifiedProperties();
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateEditor(property);
            }
        }

        public void DestroyEditor()
        {
            if (m_Editor != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Editor);
                m_Editor = null;
            }
        }

        public void UpdateEditor(SerializedProperty property)
        {
            var target = property.objectReferenceValue;

            // Destroy the editor if target has changed.
            if (m_Editor != null && m_Editor.target != target)
            {
                DestroyEditor();
            }

            // NOTE: This is triggered twice on asset switch for some reason.
            // Create editor if need one.
            if (m_Editor == null && target != null)
            {
                m_Editor = Editor.CreateEditor(target);
                if (OnCreateEditor != null)
                {
                    OnCreateEditor(m_Editor);
                }
            }
        }
    }
}

#endif
