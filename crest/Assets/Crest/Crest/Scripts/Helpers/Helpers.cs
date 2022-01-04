// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// General purpose helpers which, at the moment, do not warrant a seperate file.
    /// </summary>
    public static class Helpers
    {
        static Material s_UtilityMaterial;
        public static Material UtilityMaterial
        {
            get
            {
                if (s_UtilityMaterial == null)
                {
                    s_UtilityMaterial = new Material(Shader.Find("Hidden/Crest/Helpers/Utility"));
                }

                return s_UtilityMaterial;
            }
        }

        // Need to cast to int but no conversion cost.
        // https://stackoverflow.com/a/69148528
        internal enum UtilityPass
        {
            CopyColor,
            CopyDepth,
            ClearDepth,
            ClearStencil,
        }

        public static bool IsMSAAEnabled(Camera camera)
        {
            return camera.allowMSAA && QualitySettings.antiAliasing > 1;
        }

        public static bool IsMotionVectorsEnabled()
        {
            // Default to false until we support MVs.
            return false;
        }

        public static bool IsIntelGPU()
        {
            // Works for Windows and MacOS. Grabbed from Unity Graphics repository:
            // https://github.com/Unity-Technologies/Graphics/blob/68b0d42c/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/HDRenderPipeline.PostProcess.cs#L198-L199
            return SystemInfo.graphicsDeviceName.ToLowerInvariant().Contains("intel");
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            var temp = b;
            b = a;
            a = temp;
        }

        public static void SetGlobalKeyword(string keyword, bool enabled)
        {
            if (enabled)
            {
                Shader.EnableKeyword(keyword);
            }
            else
            {
                Shader.DisableKeyword(keyword);
            }
        }

        public static void CreateRenderTargetTexture(ref RenderTexture texture, ref RenderTargetIdentifier target, RenderTextureDescriptor descriptor)
        {
            if (texture != null && descriptor.width == texture.width && descriptor.height == texture.height &&
                descriptor.volumeDepth == texture.volumeDepth && descriptor.useDynamicScale == texture.useDynamicScale)
            {
                return;
            }
            else if (texture != null)
            {
                texture.Release();
            }

            texture = new RenderTexture(descriptor);
            target = new RenderTargetIdentifier
            (
                texture,
                mipLevel: 0,
                CubemapFace.Unknown,
                depthSlice: -1 // Bind all XR slices.
            );
        }

        public static void DestroyRenderTargetTexture(ref RenderTexture texture)
        {
            if (texture != null)
            {
                texture.Release();
                texture = null;
            }
        }

        /// <summary>
        /// Blit using full screen triangle.
        /// </summary>
        public static void Blit(CommandBuffer buffer, RenderTargetIdentifier target, Material material, int pass, MaterialPropertyBlock properties = null)
        {
            buffer.SetRenderTarget(target);
            buffer.DrawProcedural
            (
                Matrix4x4.identity,
                material,
                pass,
                MeshTopology.Triangles,
                vertexCount: 3,
                instanceCount: 1,
                properties
            );
        }
    }

    static class Extensions
    {
        public static void SetKeyword(this Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        public static void SetKeyword(this ComputeShader shader, string keyword, bool enabled)
        {
            if (enabled)
            {
                shader.EnableKeyword(keyword);
            }
            else
            {
                shader.DisableKeyword(keyword);
            }
        }

        public static void SetShaderKeyword(this CommandBuffer buffer, string keyword, bool enabled)
        {
            if (enabled)
            {
                buffer.EnableShaderKeyword(keyword);
            }
            else
            {
                buffer.DisableShaderKeyword(keyword);
            }
        }

        ///<summary>
        /// Sets the msaaSamples property to the highest supported MSAA level in the settings.
        ///</summary>
        public static void SetMSAASamples(this ref RenderTextureDescriptor descriptor, Camera camera)
        {
            // QualitySettings.antiAliasing is zero when disabled which is invalid for msaaSamples.
            // We need to set this first as GetRenderTextureSupportedMSAASampleCount uses it:
            // https://docs.unity3d.com/ScriptReference/SystemInfo.GetRenderTextureSupportedMSAASampleCount.html
            descriptor.msaaSamples = Helpers.IsMSAAEnabled(camera) ? Mathf.Max(QualitySettings.antiAliasing, 1) : 1;
            descriptor.msaaSamples = SystemInfo.GetRenderTextureSupportedMSAASampleCount(descriptor);
        }
    }
}
