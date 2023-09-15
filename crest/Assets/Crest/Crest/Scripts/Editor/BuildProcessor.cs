// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
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
    class BuildProcessor : IPreprocessShaders, IProcessSceneWithReport, IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;
        readonly List<Material> _oceanMaterials = new List<Material>();
        readonly List<UnderwaterRenderer> _underwaterRenderers = new List<UnderwaterRenderer>();

        static readonly string[] s_ShaderKeywordsToIgnoreStripping = new string[]
        {
            "_FULL_SCREEN_EFFECT",
            "_DEBUG_VIEW_OCEAN_MASK",
            "_DEBUG_VIEW_STENCIL",
            "CREST_UNDERWATER_BEFORE_TRANSPARENT",
            "CREST_FLOATING_ORIGIN",

            // XR keywords.
            "STEREO_ENABLED_ON",
            "STEREO_INSTANCING_ON",
            "UNITY_SINGLE_PASS_STEREO",
            "STEREO_MULTIVIEW_ON",

            // URP keywords.
            "_MAIN_LIGHT_SHADOWS",
            "_MAIN_LIGHT_SHADOWS_CASCADE",
            "_SHADOWS_SOFT",
        };

        bool IsWaterMaterial(Material material)
        {
            return material != null && material.shader != null && IsWaterShader(material.shader.name);
        }

        bool IsWaterShader(string name)
        {
            return name == "Crest/Ocean" || name == "Crest/Ocean URP" || name == "Crest/Framework";
        }

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

        public void OnPreprocessBuild(BuildReport report)
        {
            // Full coverage (Resources only).
            foreach (var material in Resources.LoadAll("", typeof(Material)).Cast<Material>())
            {
                if (IsWaterMaterial(material) && !_oceanMaterials.Contains(material))
                {
                    _oceanMaterials.Add(material);
                }
            }

#if CREST_UNITY_ADDRESSABLES
            // Full coverage (Addressables only).
            List<AddressableAssetEntry> assets = new List<AddressableAssetEntry>();
            AddressableAssetSettingsDefaultObject.Settings?.GetAllAssets(assets, includeSubObjects: true);
            foreach (var asset in assets)
            {
                if (asset.parentGroup.GetSchema<BundledAssetGroupSchema>()?.IncludeInBuild == true)
                {
                    var material = asset.MainAsset as Material;
                    if (IsWaterMaterial(material) && !_oceanMaterials.Contains(material))
                    {
                        _oceanMaterials.Add(material);
                    }
                }
            }
#endif
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // OnProcessScene is called on scene start too. Limit to building.
            if (!BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            // Resources.FindObjectsOfTypeAll will get all materials that are used for this scene.
            // This can retrieve stuff excluded from the build as it gets everything in memory.
            foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (IsWaterMaterial(material) && !_oceanMaterials.Contains(material))
                {
                    _oceanMaterials.Add(material);
                }
            }

            // Finds them in scenes and prefabs.
            // This can retrieve stuff excluded from the build as it gets everything in memory.
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

            // Since shader is in Resources folder, Unity will not strip it when not used so we have to.
            if (_underwaterRenderers.Count == 0)
            {
                data.Clear();
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
                    // Ignore Unity keywords (I do not think this actually does anything but I feel better with it here).
                    .Where(x => ShaderKeyword.IsKeywordLocal(x) || ShaderKeyword.GetGlobalKeywordType(x) == ShaderKeywordType.UserDefined)
                    // Ignore keywords from our list above.
#if UNITY_2021_2_OR_NEWER
                    .Where(x => !s_ShaderKeywordsToIgnoreStripping.Contains(x.name));
#else
                    .Where(x => !s_ShaderKeywordsToIgnoreStripping.Contains(ShaderKeyword.GetKeywordName(shader, x)));
#endif
                shaderKeywords.UnionWith(skipped);
            }

            // Get a list of active shader keywords.
            var usedShaderKeywords = new HashSet<ShaderKeyword>();
            foreach (var shaderKeyword in shaderKeywords)
            {
#if UNITY_2021_2_OR_NEWER
                var shaderKeywordName = shaderKeyword.name;
#else
                // GetKeywordName will work for both global and local keywords.
                var shaderKeywordName = ShaderKeyword.GetKeywordName(shader, shaderKeyword);
#endif

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
#if UNITY_2021_2_OR_NEWER
                var shaderKeywordName = shaderKeyword.name;
#else
                // GetKeywordName will work for both global and local keywords.
                var shaderKeywordName = ShaderKeyword.GetKeywordName(shader, shaderKeyword);
#endif

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
