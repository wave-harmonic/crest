// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest.Examples
{
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Makes prefab files with naming convention saveable only when the desired RP is active.
    /// </summary>
    public class RenderPipelinePrefabDontSave : AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            var rp = RenderPipelineHelper.CurrentRenderPipelineShortName;
            var toSave = new List<string>();

            foreach (var path in paths)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var extension = Path.GetExtension(path);
                // Must start with "Crest" for safety. Must end with "RP" otherwise all non RP prefabs will trigger it.
                var noSave = extension == ".prefab" && name.StartsWith("Crest") && name.EndsWith("RP") && !name.EndsWith(rp);

                if (noSave)
                {
                    Debug.Log($"Crest: Cannot save prefab (<i>{path}</i>) as it can only be saved when {rp} is active.");
                }
                else
                {
                    toSave.Add(path);
                }
            }

            return toSave.ToArray();
        }
    }
}
