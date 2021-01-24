// Crest Ocean System

// Lovingly copy pasted from Cinemachine - https://raw.githubusercontent.com/Unity-Technologies/upm-package-cinemachine/master/Editor/Utility/EmbeddedAssetHelpers.cs
// Unity Companion License: https://github.com/Unity-Technologies/upm-package-cinemachine/blob/master/LICENSE.md

using UnityEngine;
using UnityEditor;
using UnityEditor.VersionControl;

namespace Crest.EditorHelpers
{
    /// <summary>
    /// Helper for drawing embedded asset editors
    /// </summary>
    internal class EmbeddeAssetEditor<T> where T : ScriptableObject
    {
        /// <summary>
        /// Create in OnEnable()
        /// </summary>
        public EmbeddeAssetEditor(string propertyName, UnityEditor.Editor owner)
        {
            m_PropertyName = propertyName;
            m_Owner = owner;
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
        public delegate void OnChangedDelegate(T obj);

        /// <summary>
        /// Free the resources in OnDisable()
        /// </summary>
        public void OnDisable()
        {
            DestroyEditor();
            m_Owner = null;
        }

        /// <summary>
        /// Customize this after creation if you want
        /// </summary>
        public GUIContent m_CreateButtonGUIContent;

        private string m_PropertyName;
        private UnityEditor.Editor m_Editor = null;
        private UnityEditor.Editor m_Owner = null;

        const int kIndentOffset = 3;

        /// <summary>
        /// Call this from OnInspectorGUI.  Will draw the asset reference field, and
        /// the embedded editor, or a Create Asset button, if no asset is set.
        /// </summary>
        public void DrawEditorCombo(
            string title, string defaultName, string extension, string message,
            string showLabel, bool indent)
        {
            SerializedProperty property = m_Owner.serializedObject.FindProperty(m_PropertyName);
            UpdateEditor(property);
            if (m_Editor == null)
                AssetFieldWithCreateButton(property, title, defaultName, extension, message);
            else
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                Rect rect = EditorGUILayout.GetControlRect(true);
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, property);
                if (EditorGUI.EndChangeCheck())
                {
                    m_Owner.serializedObject.ApplyModifiedProperties();
                    UpdateEditor(property);
                }
                if (m_Editor != null)
                {
                    Rect foldoutRect = new Rect(
                        rect.x - kIndentOffset, rect.y, rect.width + kIndentOffset, rect.height);
                    property.isExpanded = EditorGUI.Foldout(
                        foldoutRect, property.isExpanded, GUIContent.none, true);

                    bool canEditAsset = AssetDatabase.IsOpenForEdit(m_Editor.target, StatusQueryOptions.UseCachedIfPossible);
                    GUI.enabled = canEditAsset;
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
                            OnChanged(property.objectReferenceValue as T);
                    }
                    GUI.enabled = true;
                    if(m_Editor.target != null)
                    {
			            if (!canEditAsset && GUILayout.Button("Check out"))
			            {
                            Task task = Provider.Checkout(AssetDatabase.GetAssetPath(m_Editor.target), CheckoutMode.Asset);
			                task.Wait();
			            }
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void AssetFieldWithCreateButton(
            SerializedProperty property,
            string title, string defaultName, string extension, string message)
        {
            EditorGUI.BeginChangeCheck();

            float hSpace = 5;
            float buttonWidth = GUI.skin.button.CalcSize(m_CreateButtonGUIContent).x;
            Rect r = EditorGUILayout.GetControlRect(true);
            r.width -= buttonWidth + hSpace;
            EditorGUI.PropertyField(r, property);
            r.x += r.width + hSpace; r.width = buttonWidth;
            if (GUI.Button(r, m_CreateButtonGUIContent))
            {
                string newAssetPath = EditorUtility.SaveFilePanelInProject(
                        title, defaultName, extension, message);
                if (!string.IsNullOrEmpty(newAssetPath))
                {
                    T asset = ScriptableObjectUtility.CreateAt<T>(newAssetPath);
                    property.objectReferenceValue = asset;
                    m_Owner.serializedObject.ApplyModifiedProperties();
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                m_Owner.serializedObject.ApplyModifiedProperties();
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
            if (m_Editor != null && m_Editor.target != target)
                DestroyEditor();
            if (target != null)
            {
                m_Editor = UnityEditor.Editor.CreateEditor(target);
                if (OnCreateEditor != null)
                    OnCreateEditor(m_Editor);
            }
        }
    }
}
