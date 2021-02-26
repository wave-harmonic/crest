// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Editor
{
    using UnityEditor;
    using UnityEngine;

    public class MaterialUpgrader
    {
        static bool DisplayMaterialUpgradeDialogue() => EditorUtility.DisplayDialog
        (
            "Upgrade Crest Materials?",
            "Some property names have changed since the last version of Crest. They will need upgrading.",
            "Upgrade Crest Materials",
            "Leave Crest Materials Alone"
        );

        [InitializeOnLoadMethod]
        static void UpgradeMaterials()
        {
            if (MaterialHelper.RenamedKeywords.Count == 0)
            {
                return;
            }

            var wasUpgradeChosen = false;

            AssetDatabase.StartAssetEditing();
            try
            {
                // TODO: Might have to restrict to assets found in user editable locations (like Assets).
                // Process all materials.
                foreach (var guid in AssetDatabase.FindAssets("t:Material"))
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (material.shader.name == "Crest/Ocean")
                    {
                        var serializedObject = new SerializedObject(material);
                        var floatProperties = serializedObject.FindProperty("m_SavedProperties.m_Floats");
                        var keywordProperties = serializedObject.FindProperty("m_ShaderKeywords");

                        if (MaterialHelper.ContainsRenamedKeyword(floatProperties))
                        {
                            if (wasUpgradeChosen || DisplayMaterialUpgradeDialogue())
                            {
                                wasUpgradeChosen = true;
                                MaterialHelper.MigrateKeywords(material, serializedObject, floatProperties, keywordProperties);
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                }

                if (wasUpgradeChosen)
                {
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}

