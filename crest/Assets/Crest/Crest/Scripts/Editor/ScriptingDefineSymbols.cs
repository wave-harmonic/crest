// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Build;

namespace Crest
{
    /// <summary>
    /// Adds and removes Crest's scripting define symbols when this file is added or removed respectively.
    /// </summary>
    public class ScriptingDefineSymbols : UnityEditor.AssetModificationProcessor
    {
        /// <summary>
        /// Symbols that will be added to the editor.
        /// </summary>
        static readonly string[] Symbols = new[]
        {
            "CREST_OCEAN",
        };

#if UNITY_2022_3_OR_NEWER
        // Because there is no other way to get this…
        public static NamedBuildTarget CurrentNamedBuildTarget
        {
            get
            {
#if UNITY_SERVER
                return NamedBuildTarget.Server;
#else
                return NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif
            }
        }
#endif

        // NOTE: All of the above symbols must be checked here like so: !SYMBOL_1 || !SYMBOL_2
#if !CREST_OCEAN
        [InitializeOnLoadMethod]
        static void OnProjectLoadedInEditor()
        {
            AddDefineSymbols();
        }
#endif

        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            // Only remove symbols if this file is deleted.
            if (Path.GetFullPath(path) == GetCurrentFileName())
            {
                RemoveDefineSymbols();
            }

            return AssetDeleteResult.DidNotDelete;
        }

        /// <summary>
        /// Add scripting define symbols.
        /// </summary>
        static void AddDefineSymbols()
        {
            // We remove our symbols from the list first to prevent duplicates - just to be safe.
            SetScriptingDefineSymbols(GetDefineSymbolsList().Except(Symbols).Concat(Symbols).ToArray());
        }

        /// <summary>
        /// Remove scripting define symbols.
        /// </summary>
        static void RemoveDefineSymbols()
        {
            SetScriptingDefineSymbols(GetDefineSymbolsList().Except(Symbols).ToArray());
        }

        /// <summary>
        /// Get scripting define symbols as a list.
        /// </summary>
        static string[] GetDefineSymbolsList()
        {
#if UNITY_2022_3_OR_NEWER
            return PlayerSettings.GetScriptingDefineSymbols(CurrentNamedBuildTarget)
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
#endif
                .Split(';');
        }

        static void SetScriptingDefineSymbols(string[] symbols)
        {
            SetScriptingDefineSymbols(string.Join(";", symbols));
        }

        static void SetScriptingDefineSymbols(string symbols)
        {
#if UNITY_2022_3_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(CurrentNamedBuildTarget, symbols);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
#endif
        }

        /// <summary>
        /// Get the file name of the current script file.
        /// </summary>
        static string GetCurrentFileName([System.Runtime.CompilerServices.CallerFilePath] string fileName = null)
        {
            return fileName;
        }
    }
}
