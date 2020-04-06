// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Crest
{
    // @volatile:UnderwaterMaskValues These MUST match the values in OceanConstants.hlsl
    public enum UnderwaterMaskValues
    {
        UNDERWATER_MASK_NO_MASK = 1,
        UNDERWATER_MASK_WATER_SURFACE_ABOVE = 0,
        UNDERWATER_MASK_WATER_SURFACE_BELOW = 2,
        UNDERWATER_WINDOW = 3,
    }

    internal static class UnderwaterPostProcessUtils
    {
        static readonly int sp_OceanHeight = Shader.PropertyToID("_OceanHeight");
        static readonly int sp_MainTex = Shader.PropertyToID("_MainTex");
        static readonly int sp_MaskTex = Shader.PropertyToID("_MaskTex");
        static readonly int sp_MaskDepthTex = Shader.PropertyToID("_MaskDepthTex");
        static readonly int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");
        static readonly int sp_InvViewProjectionRight = Shader.PropertyToID("_InvViewProjectionRight");
        static readonly int sp_InstanceData = Shader.PropertyToID("_InstanceData");
        static readonly int sp_AmbientLighting = Shader.PropertyToID("_AmbientLighting");

        internal class UnderwaterSphericalHarmonicsData
        {
            internal Color[] _ambientLighting = new Color[1];
            internal Vector3[] _shDirections = new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f) };
        }

        // This matches const on shader side
        internal const float UNDERWATER_MASK_NO_MASK = 1.0f;
        internal const string FULL_SCREEN_EFFECT = "_FULL_SCREEN_EFFECT";
        internal const string DEBUG_VIEW_OCEAN_MASK = "_DEBUG_VIEW_OCEAN_MASK";

        internal static void InitialiseMaskTextures(RenderTexture source, ref RenderTexture textureMask, ref RenderTexture depthBuffer, Vector2Int pixelDimensions)
        {
            // Note: we pass-through pixel dimensions explicitly as we have to handle this slightly differently in HDRP
            if (textureMask == null || textureMask.width != pixelDimensions.x || textureMask.height != pixelDimensions.y)
            {
                textureMask = new RenderTexture(source);
                textureMask.name = "Ocean Mask";
                // @Memory: We could investigate making this an 8-bit texture instead to reduce GPU memory usage.
                // We could also potentially try a half res mask as the mensicus could mask res issues.
                textureMask.format = RenderTextureFormat.RHalf;
                textureMask.width = pixelDimensions.x;
                textureMask.height = pixelDimensions.y;
                textureMask.Create();

                depthBuffer = new RenderTexture(source);
                depthBuffer.depth = 24;
                depthBuffer.enableRandomWrite = false;
                depthBuffer.name = "Ocean Mask Depth";
                depthBuffer.format = RenderTextureFormat.Depth;
                depthBuffer.width = pixelDimensions.x;
                depthBuffer.height = pixelDimensions.y;
                depthBuffer.Create();
            }
        }

        // Populates a screen space mask which will inform the underwater postprocess. As a future optimisation we may
        // be able to avoid this pass completely if we can reuse the camera depth after transparents are rendered.
        internal static void PopulateUnderwaterMasks(
            CommandBuffer commandBuffer, Camera camera, IUnderwaterPostProcessPerCameraData perCameraData,
            RenderBuffer colorBuffer, RenderBuffer depthBuffer,
            Material oceanMaskMaterial, Material generalMaskMaterial
        )
        {
            // Get all ocean chunks and render them using cmd buffer, but with mask shader
            commandBuffer.SetRenderTarget(colorBuffer, depthBuffer);
            commandBuffer.ClearRenderTarget(true, true, Color.white * UNDERWATER_MASK_NO_MASK);
            commandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            // Spends approx 0.2-0.3ms here on dell laptop
            foreach (var chunk in perCameraData.OceanChunksToRender)
            {
                commandBuffer.DrawRenderer(chunk, oceanMaskMaterial);
            }

            foreach (UnderwaterEffectFilter underwaterEffectFilter in perCameraData.GeneralUnderwaterMasksToRender)
            {
                commandBuffer.SetGlobalFloat(UnderwaterEffectFilter.sp_Mask, (float)underwaterEffectFilter.MaskType);
                commandBuffer.DrawRenderer(underwaterEffectFilter.Renderer, generalMaskMaterial);
            }

            {
                // The same camera has to perform post-processing when performing
                // VR multipass rendering so don't clear chunks until we have
                // renderered the right eye.
                // This was the approach recommended by Unity's post-processing
                // lead Thomas Hourdel.
                bool saveChunksToRender =
                    XRSettings.enabled &&
                    XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass &&
                    camera.stereoActiveEye != Camera.MonoOrStereoscopicEye.Right;

                if (!saveChunksToRender) perCameraData.OceanChunksToRender.Clear();
                if (!saveChunksToRender) perCameraData.GeneralUnderwaterMasksToRender.Clear();
            }
        }

        internal static void UpdatePostProcessMaterial(
            RenderTexture source,
            Camera camera,
            PropertyWrapperMaterial underwaterPostProcessMaterialWrapper,
            RenderTexture textureMask,
            RenderTexture depthBuffer,
            UnderwaterSphericalHarmonicsData sphericalHarmonicsData,
            bool copyParamsFromOceanMaterial,
            bool debugViewOceanMask
        )
        {
            Material underwaterPostProcessMaterial = underwaterPostProcessMaterialWrapper.material;
            if (copyParamsFromOceanMaterial)
            {
                // Measured this at approx 0.05ms on dell laptop
                underwaterPostProcessMaterial.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
            }

            // Enabling/disabling keywords each frame don't seem to have large measurable overhead
            if (debugViewOceanMask)
            {
                underwaterPostProcessMaterial.EnableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }
            else
            {
                underwaterPostProcessMaterial.DisableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }

            underwaterPostProcessMaterial.SetFloat(LodDataMgr.sp_LD_SliceIndex, 0);
            underwaterPostProcessMaterial.SetVector(sp_InstanceData, new Vector4(OceanRenderer.Instance.ViewerAltitudeLevelAlpha, 0f, 0f, OceanRenderer.Instance.CurrentLodCount));

            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(underwaterPostProcessMaterialWrapper);
            if (OceanRenderer.Instance._lodDataSeaDepths)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(underwaterPostProcessMaterialWrapper);
            }
            else
            {
                LodDataMgrSeaFloorDepth.BindNull(underwaterPostProcessMaterialWrapper);
            }

            if (OceanRenderer.Instance._lodDataShadow)
            {
                OceanRenderer.Instance._lodDataShadow.BindResultData(underwaterPostProcessMaterialWrapper);
            }
            else
            {
                LodDataMgrShadow.BindNull(underwaterPostProcessMaterialWrapper);
            }

            {
                float oceanHeight = OceanRenderer.Instance.transform.position.y;
                float maxOceanVerticalDisplacement = OceanRenderer.Instance.MaxVertDisplacement * 0.5f;
                float cameraHeight = camera.transform.position.y;
                bool forceFullShader = (cameraHeight + maxOceanVerticalDisplacement) <= oceanHeight;
                underwaterPostProcessMaterial.SetFloat(sp_OceanHeight, oceanHeight);
                if (forceFullShader)
                {
                    underwaterPostProcessMaterial.EnableKeyword(FULL_SCREEN_EFFECT);
                }
                else
                {
                    underwaterPostProcessMaterial.DisableKeyword(FULL_SCREEN_EFFECT);
                }
            }

            underwaterPostProcessMaterial.SetTexture(sp_MaskTex, textureMask);
            underwaterPostProcessMaterial.SetTexture(sp_MaskDepthTex, depthBuffer);

            // Have to set these explicitly as the built-in transforms aren't in world-space for the blit function
            if (!XRSettings.enabled || XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
            {

                var viewProjectionMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
                underwaterPostProcessMaterial.SetMatrix(sp_InvViewProjection, viewProjectionMatrix.inverse);
            }
            else
            {
                var viewProjectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) * camera.worldToCameraMatrix;
                underwaterPostProcessMaterial.SetMatrix(sp_InvViewProjection, viewProjectionMatrix.inverse);
                var viewProjectionMatrixRightEye = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right) * camera.worldToCameraMatrix;
                underwaterPostProcessMaterial.SetMatrix(sp_InvViewProjectionRight, viewProjectionMatrixRightEye.inverse);
            }

            // Not sure why we need to do this - blit should set it...?
            underwaterPostProcessMaterial.SetTexture(sp_MainTex, source);

            // Compute ambient lighting SH
            {
                // We could pass in a renderer which would prime this lookup. However it doesnt make sense to use an existing render
                // at different position, as this would then thrash it and negate the priming functionality. We could create a dummy invis GO
                // with a dummy Renderer which might be enoguh, but this is hacky enough that we'll wait for it to become a problem
                // rather than add a pre-emptive hack.

                UnityEngine.Profiling.Profiler.BeginSample("Underwater sample spherical harmonics");

                LightProbes.GetInterpolatedProbe(OceanRenderer.Instance.Viewpoint.position, null, out SphericalHarmonicsL2 sphericalHarmonicsL2);
                sphericalHarmonicsL2.Evaluate(sphericalHarmonicsData._shDirections, sphericalHarmonicsData._ambientLighting);
                underwaterPostProcessMaterial.SetVector(sp_AmbientLighting, sphericalHarmonicsData._ambientLighting[0]);

                UnityEngine.Profiling.Profiler.EndSample();
            }
        }
    }
}
