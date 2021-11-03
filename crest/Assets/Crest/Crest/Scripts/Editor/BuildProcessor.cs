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
using System.Linq;

namespace Crest
{
    /// <summary>
    /// Optimises Crest for builds by stripping shader variants to reduce build times and size.
    ///
    /// Candidates for stripping:
    /// - multi_compile keywords in the underwater effect shader which mirror the ocean shader are candidates for
    ///   stripping (eg _CAUSTICS_ON). We determine this by checking the keywords used in the ocean material.
    /// - the meniscus keyword (CREST_MENISCUS) which is set on the underwater renderer.
    /// </summary>
    class BuildProcessor : IPreprocessShaders, IProcessSceneWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;
        readonly List<Material> _oceanMaterials = new List<Material>();
        readonly List<UnderwaterRenderer> _underwaterRenderers = new List<UnderwaterRenderer>();

        static readonly string[] s_ShaderKeywordsToIgnoreStripping = new string[]
        {
            "_FULL_SCREEN_EFFECT",
            "_DEBUG_VIEW_OCEAN_MASK",
        };

        bool IsUnderwaterShader(string shaderName)
        {
            // According to the docs it's possible to change RP at runtime, so I guess all relevant
            // shaders should be built.
            return shaderName == "Crest/Underwater Curtain"
                || shaderName.StartsWith("Hidden/Crest/Underwater/Underwater Effect")
                || shaderName == "Hidden/Crest/Underwater/Post Process HDRP";
        }

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

                if (_oceanMaterials.Contains(material))
                {
                    continue;
                }

                _oceanMaterials.Add(material);
            }

            // Finds them in scenes and prefabs. Instances found is higher than expected.
            foreach (var underwaterRenderer in Resources.FindObjectsOfTypeAll<UnderwaterRenderer>())
            {
                if (!_underwaterRenderers.Contains(underwaterRenderer))
                {
                    _underwaterRenderers.Add(underwaterRenderer);
                }
            }
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // This method will be called once per combination of shader, shader type (eg vertex or fragment), and
            // shader pass. For underwater, it will at minimum be called twice since it has vertex and fragment.

#if CREST_DEBUG
            if (shader.name.StartsWith("Crest") || shader.name.StartsWith("Hidden/Crest"))
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

            // Collect all shader keywords.
            var shaderKeywords = new HashSet<ShaderKeyword>();
            for (int i = 0; i < data.Count; i++)
            {
                // Each ShaderCompilerData is a variant which is a combination of keywords. Since each list will be
                // different, simply getting a list of all keywords is not possible. This also appears to be the only
                // way to get a list of keywords without trying to extract them from shader property names. Lastly,
                // shader_feature will be returned only if they are enabled.
                var skipped = data[i].shaderKeywordSet.GetShaderKeywords()
                    // Ignore Unity keywords.
                    .Where(x => ShaderKeyword.GetKeywordType(shader, x) == ShaderKeywordType.UserDefined)
                    // Ignore keywords from our list above.
                    .Where(x => !s_ShaderKeywordsToIgnoreStripping.Contains(ShaderKeyword.GetKeywordName(shader, x)));
                shaderKeywords.UnionWith(skipped);
            }

            // Get a list of active shader keywords.
            var usedShaderKeywords = new HashSet<ShaderKeyword>();
            foreach (var shaderKeyword in shaderKeywords)
            {
                // GetKeywordName will work for both global and local keywords.
                var shaderKeywordName = ShaderKeyword.GetKeywordName(shader, shaderKeyword);

                // Mensicus is set on the UnderwaterRenderer component.
                if (shaderKeywordName.Contains("CREST_MENISCUS"))
                {
                    foreach (var underwaterRenderer in _underwaterRenderers)
                    {
                        if (underwaterRenderer.IsMeniscusEnabled)
                        {
                            usedShaderKeywords.Add(shaderKeyword);
                            break;
                        }
                    }

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

            // Get a list of inactive keywords. Also, remove active keywords from list that are also inactive as this
            // means it could either active or inactive in a build so we must skip stripping.
            var unusedShaderKeyowrds = new HashSet<ShaderKeyword>();
            foreach (var shaderKeyword in shaderKeywords)
            {
                var shaderKeywordName = ShaderKeyword.GetKeywordName(shader, shaderKeyword);

                // Mensicus is set on the UnderwaterRenderer component.
                if (shaderKeywordName.Contains("CREST_MENISCUS"))
                {
                    foreach (var underwaterRenderer in _underwaterRenderers)
                    {
                        if (!underwaterRenderer.IsMeniscusEnabled)
                        {
                            if (usedShaderKeywords.Contains(shaderKeyword))
                            {
                                // Keyword is both used and unused so we must skip stripping.
                                usedShaderKeywords.Remove(shaderKeyword);
                            }
                            else
                            {
                                unusedShaderKeyowrds.Add(shaderKeyword);
                            }

                            break;
                        }
                    }

                    continue;
                }

                foreach (var oceanMaterial in _oceanMaterials)
                {
                    if (!oceanMaterial.IsKeywordEnabled(shaderKeywordName))
                    {
                        if (usedShaderKeywords.Contains(shaderKeyword))
                        {
                            // Keyword is both used and unused so we must skip stripping.
                            usedShaderKeywords.Remove(shaderKeyword);
                        }
                        else
                        {
                            unusedShaderKeyowrds.Add(shaderKeyword);
                        }

                        break;
                    }
                }
            }

            // Loop over and strip variants.
            for (int index = 0; index < data.Count; index++)
            {
                var isStripped = false;

                foreach (var unusedShaderKeyword in unusedShaderKeyowrds)
                {
                    // Variant uses this inactive keyword so we can strip it.
                    if (data[index].shaderKeywordSet.IsEnabled(unusedShaderKeyword))
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

                foreach (var usedShaderKeyword in usedShaderKeywords)
                {
                    // Variant does not use this active keyword so we can strip it.
                    if (!data[index].shaderKeywordSet.IsEnabled(usedShaderKeyword))
                    {
                        // Strip variant.
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
