// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Editor
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;

    public static class MaterialHelper
    {
        internal static readonly Dictionary<string, string> RenamedKeywords = new Dictionary<string, string>
        {
        };

        internal static void MigrateKeywords(Material material, SerializedObject serializedObject, SerializedProperty floatProperties, SerializedProperty keywordProperties)
        {
            foreach (var entry in RenamedKeywords)
            {
                RenameKeyword(entry.Key, entry.Value, floatProperties, keywordProperties);
            }

            // Order is important. This is what it takes to save our changes to the material.
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(material);
        }

        internal static void MigrateKeywordsGUI(Material material, SerializedObject serializedObject)
        {
            var floatProperties = serializedObject.FindProperty("m_SavedProperties.m_Floats");
            var keywordProperties = serializedObject.FindProperty("m_ShaderKeywords");

            if (ContainsRenamedKeyword(floatProperties) && GUILayout.Button("Migrate"))
            {
                foreach (var entry in RenamedKeywords)
                {
                    RenameKeyword(entry.Key, entry.Value, floatProperties, keywordProperties);
                }

                // Order is important. This is what it takes to save our changes to the material.
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
                GUIUtility.ExitGUI();
            }
        }

        internal static bool ContainsRenamedKeyword(SerializedProperty floatProperties)
        {
            if (floatProperties != null && floatProperties.isArray)
            {
                for (int index = 0; index < floatProperties.arraySize; index++)
                {
                    if (RenamedKeywords.ContainsKey(floatProperties.GetArrayElementAtIndex(index).displayName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static void RenameKeyword(string oldName, string newName, SerializedProperty floatProperties, SerializedProperty keywordProperties)
        {
            if (floatProperties != null && floatProperties.isArray)
            {
                for (int i = 0; i < floatProperties.arraySize; i++)
                {
                    var oldProperty = floatProperties.GetArrayElementAtIndex(i);
                    if (oldProperty.displayName == oldName)
                    {
                        for (int ii = 0; ii < floatProperties.arraySize; ii++)
                        {
                            SerializedProperty newProperty = floatProperties.GetArrayElementAtIndex(ii);
                            // Even if the property does not exist in the file, it will exist if it is defined in the shader.
                            if (newProperty.displayName == newName)
                            {
                                // A property is a pair so we need to navigate down a level.
                                oldProperty.Next(true);
                                // Skip the first value which is the name.
                                oldProperty.Next(false);

                                // A property is a pair so we need to navigate down a level.
                                newProperty.Next(true);
                                // Skip the first value which is the name.
                                newProperty.Next(false);

                                // Copy the value over.
                                newProperty.floatValue = oldProperty.floatValue;

                                var keywords = keywordProperties.stringValue.Split(' ').ToList();
                                keywords.Remove($"{oldName.ToUpper()}_ON");
                                if (newProperty.floatValue == 1)
                                {
                                    keywords.Add($"{newName.ToUpper()}_ON");
                                }
                                keywordProperties.stringValue = string.Join(" ", keywords);
                            }
                        }

                        // Delete the old property.
                        floatProperties.DeleteArrayElementAtIndex(i);
                        return;
                    }
                }
            }
        }
    }
}

