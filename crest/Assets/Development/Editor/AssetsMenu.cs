// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Development
{
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// A place for menu items under Assets menu.
    /// </summary>
    class AssetsMenu : MonoBehaviour
    {
        static void ForceReserializeAssets(ForceReserializeAssetsOptions options = ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata)
        {
            // Get selected assets from project window.
            var paths = Selection.GetFiltered<Object>(SelectionMode.Assets).Select(x => AssetDatabase.GetAssetPath(x));

            // If nothing selected, then get all assets.
            if (paths.Count() == 0)
            {
                paths = AssetDatabase.GetAllAssetPaths();
            }

            // Forces assets to reserialize which can upgrade the serialized version.
            // https://docs.unity3d.com/ScriptReference/AssetDatabase.ForceReserializeAssets.html
            AssetDatabase.ForceReserializeAssets(paths, options);
        }

        [MenuItem("Assets/Reserialize/Assets & Metadata")]
        static void ForceReserializeAssets()
        {
            ForceReserializeAssets(ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
        }

        [MenuItem("Assets/Reserialize/Assets")]
        static void ForceReserializeAssetsOnly()
        {
            ForceReserializeAssets(ForceReserializeAssetsOptions.ReserializeAssets);
        }

        [MenuItem("Assets/Reserialize/Metadata")]
        static void ForceReserializeMetadataOnly()
        {
            ForceReserializeAssets(ForceReserializeAssetsOptions.ReserializeMetadata);
        }
    }
}
