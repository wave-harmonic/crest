// Crest Ocean System

// Lovingly adapted from Cinemachine - https://raw.githubusercontent.com/Unity-Technologies/upm-package-cinemachine/master/Editor/Utility/ScriptableObjectUtility.cs
// Unity Companion License: https://github.com/Unity-Technologies/upm-package-cinemachine/blob/master/LICENSE.md

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace Crest.EditorHelpers
{
    public class ScriptableObjectUtility : ScriptableObject
    {
        /// <summary>Create a scriptable object asset</summary>
        public static T CreateAt<T>(string assetPath) where T : ScriptableObject
        {
            return CreateAt(typeof(T), assetPath) as T;
        }

        /// <summary>Create a scriptable object asset</summary>
        public static ScriptableObject CreateAt(Type assetType, string assetPath)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(assetType);
            if (asset == null)
            {
                Debug.LogError("Crest: Failed to create instance of " + assetType.Name + " at " + assetPath);
                return null;
            }
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        public static void Create<T>(bool prependFolderName = false, bool trimName = true) where T : ScriptableObject
        {
            string className = typeof(T).Name;
            string assetName = className;
            string folder = GetSelectedAssetFolder();

            if (trimName)
            {
                string[] standardNames = new string[] { "Asset", "Attributes", "Container" };
                foreach (string standardName in standardNames)
                {
                    assetName = assetName.Replace(standardName, "");
                }
            }

            if (prependFolderName)
            {
                string folderName = Path.GetFileName(folder);
                assetName = (string.IsNullOrEmpty(assetName) ? folderName : string.Format("{0}_{1}", folderName, assetName));
            }

            Create(className, assetName, folder);
        }

        private static ScriptableObject Create(string className, string assetName, string folder)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(className);
            if (asset == null)
            {
                Debug.LogError("Crest: Failed to create instance of " + className);
                return null;
            }

            asset.name = assetName ?? className;

            string assetPath = GetUnusedAssetPath(folder, asset.name);
            AssetDatabase.CreateAsset(asset, assetPath);

            return asset;
        }

        private static string GetSelectedAssetFolder()
        {
            if ((Selection.activeObject != null) && AssetDatabase.Contains(Selection.activeObject))
            {
                string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                string assetPathAbsolute = string.Format("{0}/{1}", Path.GetDirectoryName(Application.dataPath), assetPath);

                if (Directory.Exists(assetPathAbsolute))
                {
                    return assetPath;
                }
                else
                {
                    return Path.GetDirectoryName(assetPath);
                }
            }

            return "Assets";
        }

        private static string GetUnusedAssetPath(string folder, string assetName)
        {
            for (int n = 0; n < 9999; n++)
            {
                string assetPath = string.Format("{0}/{1}{2}.asset", folder, assetName, (n == 0 ? "" : n.ToString()));
                string existingGUID = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(existingGUID))
                {
                    return assetPath;
                }
            }

            return null;
        }
    }
}

#endif
