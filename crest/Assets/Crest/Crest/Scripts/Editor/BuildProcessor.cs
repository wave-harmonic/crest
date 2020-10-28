// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Crest
{
    /// <summary>
    /// Optimises Crest for builds by stripping shader variants to reduce build times and size.
    /// </summary>
    class BuildProcessor : IPreprocessShaders, IProcessSceneWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;
        string UnderwaterShaderName => "Crest/Underwater Curtain";
        readonly List<Material> _oceanMaterials = new List<Material>();

#if CREST_DEBUG
        int shaderVariantCount = 0;
        int shaderVarientStrippedCount = 0;
#endif

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // OnProcessScene is called on scene start too. Limit to building.
            if (!BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            // Resources.FindObjectsOfTypeAll will get all materials that are used for this scene.
            foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (material.shader.name != "Crest/Ocean" && material.shader.name != "Crest/Ocean URP" && material.shader.name != "Crest/Framework")
                {
                    continue;
                }

                _oceanMaterials.Add(material);
            }
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
#if CREST_DEBUG
            if (shader.name.StartsWith("Crest"))
            {
                shaderVariantCount += data.Count;
            }
#endif

            if (shader.name == UnderwaterShaderName)
            {
                ProcessUnderwaterShader(shader, data);
            }
        }

        /// <summary>
        /// Strips shader variants from the underwater shader based on what features are enabled on the ocean material.
        /// </summary>
        public void ProcessUnderwaterShader(Shader shader, IList<ShaderCompilerData> data)
        {
            // This should not happen. There should always be at least one variant.
            if (data.Count == 0)
            {
                return;
            }

#if CREST_DEBUG
            var shaderVariantCount = data.Count;
            var shaderVarientStrippedCount = 0;
#endif

            // Collect all shader keywords.
            var unusedShaderKeywords = new HashSet<ShaderKeyword>();
            for (int i = 0; i < data.Count; i++)
            {
                // Each ShaderCompilerData is a variant which is a combination of keywords. Since each list will be
                // different, simply getting a list of all keywords is not possible. This also appears to be the only
                // way to get a list of keywords without trying to extract them from shader property names. Lastly,
                // shader_feature will be returned only if they are enabled.
                unusedShaderKeywords.UnionWith(data[i].shaderKeywordSet.GetShaderKeywords());
            }

            // Get used shader keywords so we can exclude them.
            var usedShaderKeywords = new List<ShaderKeyword>();
            foreach (var shaderKeyword in unusedShaderKeywords)
            {
                // Do not handle built-in shader keywords.
                if (ShaderKeyword.GetKeywordType(shader, shaderKeyword) != ShaderKeywordType.UserDefined)
                {
                    usedShaderKeywords.Add(shaderKeyword);
                    continue;
                }

                // GetKeywordName will work for both global and local keywords.
                var shaderKeywordName = ShaderKeyword.GetKeywordName(shader, shaderKeyword);

                // These keywords will not be on ocean material.
                if (shaderKeywordName.Contains("_MENISCUS") || shaderKeywordName.Contains("_FULL_SCREEN_EFFECT"))
                {
                    usedShaderKeywords.Add(shaderKeyword);
                    continue;
                }

                // TODO: Strip this once post-processing is more unified.
                if (shaderKeywordName.Contains("_DEBUG_VIEW_OCEAN_MASK"))
                {
                    usedShaderKeywords.Add(shaderKeyword);
                    continue;
                }

                foreach (var oceanMaterial in _oceanMaterials)
                {
                    if (oceanMaterial.IsKeywordEnabled(shaderKeywordName))
                    {
                        usedShaderKeywords.Add(shaderKeyword);
                        break;
                    }
                }
            }

            // Exclude used keywords to obtain list of unused keywords.
            unusedShaderKeywords.ExceptWith(usedShaderKeywords);

            for (int index = 0; index < data.Count; index++)
            {
                foreach (var unusedShaderKeyword in unusedShaderKeywords)
                {
                    // IsEnabled means this variant uses this keyword and we can strip it.
                    if (data[index].shaderKeywordSet.IsEnabled(unusedShaderKeyword))
                    {
                        data.RemoveAt(index--);
#if CREST_DEBUG
                        shaderVarientStrippedCount++;
#endif
                        break;
                    }
                }
            }

#if CREST_DEBUG
            this.shaderVarientStrippedCount += shaderVarientStrippedCount;
            Debug.Log($"Crest: {shaderVarientStrippedCount} shader variants stripped of {shaderVariantCount} from {shader.name}.");
#endif
        }

        public void OnPostprocessBuild(BuildReport report)
        {
#if CREST_DEBUG
            Debug.Log($"Crest: Stripped {shaderVarientStrippedCount} shader variants of {shaderVariantCount} from Crest.");
#endif
        }
    }
}
