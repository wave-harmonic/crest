// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    public class GeometryShaderHelpers
    {
        // Unity does not allow us to specifically query if a given platform does
        // or doesn't support geometry shaders, so we have use these heuristics
        // to test for this ourselves.
        public static bool PlatformSupportsGeometryShaders
        {
            get
            {
                // Only use geometry shader if target device supports it.
                // Check for specific platforms which have poor/lacking geometry
                // shader support first, before checking runtime platform info.
#if UNITY_PS4 || UNITY_ANDROID || UNITY_IOS
                // Mobile Devices don't support geometry shaders at all in a way
                // that makes sense to support them:
                // https://www.reddit.com/r/vulkan/comments/91q0qx/do_geometry_shaders_still_suck/

                // Note: Whilst the PS4 supports geometry shaders, we get weird
                // stippling artifacts which need to be investigated:
                // https://github.com/crest-ocean/crest/issues/290
                return false;
#else
                // Check runtime platform info for geometry shader support just
                // in case.
                // See https://docs.unity3d.com/2018.1/Documentation/Manual/SL-ShaderCompileTargets.html
                // See https://docs.unity3d.com/ScriptReference/SystemInfo-graphicsShaderLevel.html
                if (SystemInfo.graphicsShaderLevel <= 35 ||
                    SystemInfo.graphicsShaderLevel == 45 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                {
                    return false;
                }
                return true;
#endif
            }
        }
    }
}
