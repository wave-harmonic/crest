// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// Optimises Crest for builds by stripping shader variants to reduce build times and size.
    /// </summary>
    class BuildProcessor : IPreprocessShaders, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        bool IsUnderwaterShader(string shaderName)
        {
            // According to the docs it's possible to change RP at runtime, so I guess all relevant
            // shaders should be built.
            return shaderName == "Crest/Underwater Curtain"
                || shaderName.StartsWithNoAlloc("Hidden/Crest/Underwater/Underwater Effect")
                || shaderName == "Hidden/Crest/Underwater/Post Process HDRP";
        }

#if CREST_DEBUG
        int shaderVariantCount = 0;
        int shaderVarientStrippedCount = 0;
#endif

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // This method will be called once per combination of shader, shader type (eg vertex or fragment), and
            // shader pass. For underwater, it will at minimum be called twice since it has vertex and fragment.

#if CREST_DEBUG
            if (shader.name.StartsWithNoAlloc("Crest") || shader.name.StartsWithNoAlloc("Hidden/Crest"))
            {
                shaderVariantCount += data.Count;
            }
#endif

            if (IsUnderwaterShader(shader.name))
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

            // Loop over and strip variants.
            for (int index = 0; index < data.Count; index++)
            {
                var isStripped = false;

                var keywords = data[index].shaderKeywordSet.GetShaderKeywords();

                // Strip any debug variants.
                if (!EditorUserBuildSettings.development)
                {
                    foreach (var keyword in keywords)
                    {
#if UNITY_2021_3_OR_NEWER
                        var name = keyword.name;
#else
                        var name = ShaderKeyword.GetKeywordName(shader, keyword);
#endif
                        if (!string.IsNullOrEmpty(name) && name.StartsWithNoAlloc("_DEBUG_"))
                        {
                            // Strip variant.
                            data.RemoveAt(index--);
#if CREST_DEBUG
                            shaderVarientStrippedCount++;
#endif
                            isStripped = true;

                            break;
                        }
                    }

                    if (isStripped)
                    {
                        continue;
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
