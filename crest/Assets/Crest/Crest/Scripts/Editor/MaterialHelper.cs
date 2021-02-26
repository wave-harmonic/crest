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
            { "_Foam", "CREST_FOAM" },
            { "_Shadows", "CREST_SHADOWS" },
            { "_Caustics", "CREST_CAUSTICS" },
            { "_Flow", "CREST_FLOW" },
        };

        internal static void MigrateKeywords(Material material, SerializedObject serializedObject,
            SerializedProperty floatProperties, SerializedProperty keywordProperties)
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

            if (ContainsRenamedKeyword(floatProperties) && GUILayout.Button("Upgrade Material"))
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

        internal static void RenameKeyword(string oldName, string newName, SerializedProperty floatProperties,
            SerializedProperty keywordProperties)
        {
            if (floatProperties == null || !floatProperties.isArray || keywordProperties == null)
            {
                // TODO: Error
                return;
            }

            for (int i = 0; i < floatProperties.arraySize; i++)
            {
                var oldProperty = floatProperties.GetArrayElementAtIndex(i);
                if (oldProperty.displayName != oldName)
                {
                    continue;
                }

                // If the material/shader has been loaded, it will already have created the new properties.
                var isFound = false;
                for (int ii = 0; ii < floatProperties.arraySize; ii++)
                {
                    SerializedProperty newProperty = floatProperties.GetArrayElementAtIndex(ii);

                    if (newProperty.displayName == newName)
                    {
                        RenameKeyword(oldName, newName, oldProperty, newProperty, keywordProperties);
                        isFound = true;
                        break;
                    }
                }

                if (!isFound)
                {
                    // Insert at the end of the array.
                    floatProperties.InsertArrayElementAtIndex(floatProperties.arraySize);
                    // Fetch newly inserted property. arraySize dynamically increases on insertion.
                    SerializedProperty newProperty = floatProperties.GetArrayElementAtIndex(floatProperties.arraySize - 1);
                    RenameKeyword(oldName, newName, oldProperty, newProperty, keywordProperties);
                }

                // Delete the old property.
                floatProperties.DeleteArrayElementAtIndex(i);
                return;
            }
        }

        internal static void RenameKeyword(string oldName, string newName, SerializedProperty oldProperty,
            SerializedProperty newProperty, SerializedProperty keywordProperties)
        {
            // A property is a pair so we need to navigate down a level.
            oldProperty.Next(true);
            // Skip the first value which is the label.
            oldProperty.Next(false);

            // Navigate to the label.
            newProperty.Next(true);
            // Set the label just in case this is newly inserted.
            newProperty.stringValue = newName;
            // Navigate to the value.
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
}

