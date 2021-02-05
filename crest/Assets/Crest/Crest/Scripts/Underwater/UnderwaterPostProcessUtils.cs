// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Crest
{
    public static class UnderwaterPostProcessUtils
    {
        public static readonly int sp_CrestOceanMaskTexture = Shader.PropertyToID("_CrestOceanMaskTexture");
        public static readonly int sp_CrestOceanMaskDepthTexture = Shader.PropertyToID("_CrestOceanMaskDepthTexture");

        static readonly int sp_OceanHeight = Shader.PropertyToID("_OceanHeight");
        static readonly int sp_MainTex = Shader.PropertyToID("_MainTex");
        static readonly int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");
        static readonly int sp_InvViewProjectionRight = Shader.PropertyToID("_InvViewProjectionRight");
        static readonly int sp_InstanceData = Shader.PropertyToID("_InstanceData");
        static readonly int sp_AmbientLighting = Shader.PropertyToID("_AmbientLighting");
        static readonly int sp_HorizonPosNormal = Shader.PropertyToID("_HorizonPosNormal");
        static readonly int sp_HorizonPosNormalRight = Shader.PropertyToID("_HorizonPosNormalRight");
        static readonly int sp_DataSliceOffset = Shader.PropertyToID("_DataSliceOffset");

        public const string tooltipHorizonSafetyMarginMultiplier = "A safety margin multiplier to adjust horizon line based on camera position to avoid minor artifacts caused by floating point precision issues, the default value has been chosen based on careful experimentation.";
        public const string tooltipFilterOceanData = "How much to smooth ocean data such as water depth, light scattering, shadowing. Helps to smooth flickering that can occur under camera motion.";
        public const string tooltipMeniscus = "Add a meniscus to the boundary between water and air.";

        // A magic number found after a small-amount of iteration that is used to deal with horizon-line floating-point
        // issues. It allows us to give it a small *nudge* in the right direction based on whether the camera is above
        // or below the horizon line itself already.
        public const float DefaultHorizonSafetyMarginMultiplier = 0.01f;

        public const int DefaultFilterOceanDataValue = LodDataMgr.MAX_LOD_COUNT - 2;
        public const int MinFilterOceanDataValue = 0;
        public const int MaxFilterOceanDataValue = LodDataMgr.MAX_LOD_COUNT - 2;

        public class UnderwaterSphericalHarmonicsData
        {
            internal Color[] _ambientLighting = new Color[1];
            internal Vector3[] _shDirections = { new Vector3(0.0f, 0.0f, 0.0f) };
        }

        // This matches const on shader side
        internal const float UNDERWATER_MASK_NO_MASK = 1.0f;
        internal const string FULL_SCREEN_EFFECT = "_FULL_SCREEN_EFFECT";
        internal const string DEBUG_VIEW_OCEAN_MASK = "_DEBUG_VIEW_OCEAN_MASK";

        public static void InitialiseMaskTextures(RenderTextureDescriptor desc, ref RenderTexture textureMask, ref RenderTexture depthBuffer)
        {
            // Note: we pass-through pixel dimensions explicitly as we have to handle this slightly differently in HDRP
            if (textureMask == null || textureMask.width != desc.width || textureMask.height != desc.height)
            {
                // @Performance: We should consider either a temporary RT or use an RTHandle if appropriate
                // RenderTexture is a "native engine object". We have to release it to avoid memory leaks.
                if (textureMask != null)
                {
                    textureMask.Release();
                    depthBuffer.Release();
                }

                textureMask = new RenderTexture(desc);
                textureMask.depth = 0;
                textureMask.name = "Ocean Mask";
                // @Memory: We could investigate making this an 8-bit texture instead to reduce GPU memory usage.
                // We could also potentially try a half res mask as the mensicus could mask res issues.
                textureMask.format = RenderTextureFormat.RHalf;
                textureMask.Create();

                depthBuffer = new RenderTexture(desc);
                depthBuffer.depth = 24;
                depthBuffer.enableRandomWrite = false;
                depthBuffer.name = "Ocean Mask Depth";
                depthBuffer.format = RenderTextureFormat.Depth;
                depthBuffer.Create();
            }
        }

        // Populates a screen space mask which will inform the underwater postprocess. As a future optimisation we may
        // be able to avoid this pass completely if we can reuse the camera depth after transparents are rendered.
        public static void PopulateOceanMask(
            CommandBuffer commandBuffer, Camera camera, List<OceanChunkRenderer> chunksToRender, Plane[] frustumPlanes,
            RenderTexture colorBuffer, RenderTexture depthBuffer,
            Material oceanMaskMaterial,
            bool debugDisableOceanMask
        )
        {
            // Get all ocean chunks and render them using cmd buffer, but with mask shader
            commandBuffer.SetRenderTarget(colorBuffer.colorBuffer, depthBuffer.depthBuffer);
            commandBuffer.ClearRenderTarget(true, true, Color.white * UNDERWATER_MASK_NO_MASK);
            commandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            if (!debugDisableOceanMask)
            {
                // Spends approx 0.2-0.3ms here on dell laptop
                foreach (OceanChunkRenderer chunk in chunksToRender)
                {
                    Renderer renderer = chunk.Rend;
                    Bounds bounds = renderer.bounds;
                    if (GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                    {
                        if ((!chunk._oceanDataHasBeenBound) && chunk.enabled)
                        {
                            chunk.BindOceanData(camera);
                        }
                        commandBuffer.DrawRenderer(renderer, oceanMaskMaterial);
                    }
                    chunk._oceanDataHasBeenBound = false;
                }
            }

            commandBuffer.SetGlobalTexture(sp_CrestOceanMaskTexture, colorBuffer);
            commandBuffer.SetGlobalTexture(sp_CrestOceanMaskDepthTexture, depthBuffer);

        }

        public static void UpdatePostProcessMaterial(
            RenderTargetIdentifier source,
            Camera camera,
            IPropertyWrapper underwaterPostProcessMaterialWrapper,
            UnderwaterSphericalHarmonicsData sphericalHarmonicsData,
            bool isMeniscusEnabled,
            bool copyParamsFromOceanMaterial,
            bool debugViewPostProcessMask,
            float horizonSafetyMarginMultiplier,
            int dataSliceOffset
        )
        {
            // TODO(TRC):Now Re-enable this with a property wrapper abstraction
            if (copyParamsFromOceanMaterial)
            {
                // Measured this at approx 0.05ms on dell laptop
                underwaterPostProcessMaterialWrapper.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
            }

            // Enable/Disable meniscus.
            if (isMeniscusEnabled)
            {
                underwaterPostProcessMaterialWrapper.EnableKeyword("CREST_MENISCUS");
            }
            else
            {
                underwaterPostProcessMaterialWrapper.DisableKeyword("CREST_MENISCUS");
            }

            // Enabling/disabling keywords each frame don't seem to have large measurable overhead
            if (debugViewPostProcessMask)
            {
                underwaterPostProcessMaterialWrapper.EnableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }
            else
            {
                underwaterPostProcessMaterialWrapper.DisableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }

            underwaterPostProcessMaterialWrapper.SetFloat(LodDataMgr.sp_LD_SliceIndex, 0);
            underwaterPostProcessMaterialWrapper.SetVector(sp_InstanceData, new Vector4(OceanRenderer.Instance.ViewerAltitudeLevelAlpha, 0f, 0f, OceanRenderer.Instance.CurrentLodCount));

            LodDataMgrAnimWaves.Bind(underwaterPostProcessMaterialWrapper);
            LodDataMgrSeaFloorDepth.Bind(underwaterPostProcessMaterialWrapper);
            LodDataMgrShadow.Bind(underwaterPostProcessMaterialWrapper);

            float seaLevel = OceanRenderer.Instance.SeaLevel;
            {
                // We only apply the horizon safety margin multiplier to horizon if and only if
                // concrete height of the camera relative to the water and the height of the camera
                // relative to the sea-level are the same. This ensures that in incredibly turbulent
                // water - if in doubt - use the neutral horizon.
                float seaLevelHeightDifference = camera.transform.position.y - seaLevel;
                if (seaLevelHeightDifference >= 0.0f ^ OceanRenderer.Instance.ViewerHeightAboveWater >= 0.0f)
                {
                    horizonSafetyMarginMultiplier = 0.0f;
                }
            }
            {
                underwaterPostProcessMaterialWrapper.SetFloat(sp_OceanHeight, seaLevel);
                underwaterPostProcessMaterialWrapper.SetInt(sp_DataSliceOffset, dataSliceOffset);

                float maxOceanVerticalDisplacement = OceanRenderer.Instance.MaxVertDisplacement * 0.5f;
                float cameraYPosition = camera.transform.position.y;
                float nearPlaneFrustumWorldHeight;
                {

                    float current = camera.ViewportToWorldPoint(new Vector3(0f, 0f, camera.nearClipPlane)).y;
                    float maxY = current, minY = current;

                    current = camera.ViewportToWorldPoint(new Vector3(0f, 1f, camera.nearClipPlane)).y;
                    maxY = Mathf.Max(maxY, current);
                    minY = Mathf.Min(minY, current);

                    current = camera.ViewportToWorldPoint(new Vector3(1f, 0f, camera.nearClipPlane)).y;
                    maxY = Mathf.Max(maxY, current);
                    minY = Mathf.Min(minY, current);

                    current = camera.ViewportToWorldPoint(new Vector3(1f, 1f, camera.nearClipPlane)).y;
                    maxY = Mathf.Max(maxY, current);
                    minY = Mathf.Min(minY, current);

                    nearPlaneFrustumWorldHeight = maxY - minY;
                }
                // We don't both setting the horizon value if we know we are going to be having to apply the post-processing
                // effect full-screen anyway.
                bool forceFullShader = (cameraYPosition + nearPlaneFrustumWorldHeight + maxOceanVerticalDisplacement) <= seaLevel;

                underwaterPostProcessMaterialWrapper.SetFloat(sp_OceanHeight, seaLevel);
                if (forceFullShader)
                {
                    underwaterPostProcessMaterialWrapper.EnableKeyword(FULL_SCREEN_EFFECT);
                }
                else
                {
                    underwaterPostProcessMaterialWrapper.DisableKeyword(FULL_SCREEN_EFFECT);
                }

            }

            // Have to set these explicitly as the built-in transforms aren't in world-space for the blit function
            if (!XRSettings.enabled || XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
            {

                var inverseViewProjectionMatrix = (camera.projectionMatrix * camera.worldToCameraMatrix).inverse;
                underwaterPostProcessMaterialWrapper.SetMatrix(sp_InvViewProjection, inverseViewProjectionMatrix);

                {
                    GetHorizonPosNormal(camera, Camera.MonoOrStereoscopicEye.Mono, seaLevel, horizonSafetyMarginMultiplier, out Vector2 pos, out Vector2 normal);
                    underwaterPostProcessMaterialWrapper.SetVector(sp_HorizonPosNormal, new Vector4(pos.x, pos.y, normal.x, normal.y));
                }
            }
            else
            {
                // Store projection matrix to restore later.
                var projectionMatrix = camera.projectionMatrix;

                // We need to set the matrix ourselves. Maybe ViewportToWorldPoint has a bug.
                camera.projectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);

                var inverseViewProjectionMatrix = (camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) * camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left)).inverse;
                underwaterPostProcessMaterialWrapper.SetMatrix(sp_InvViewProjection, inverseViewProjectionMatrix);

                {
                    GetHorizonPosNormal(camera, Camera.MonoOrStereoscopicEye.Left, seaLevel, horizonSafetyMarginMultiplier, out Vector2 pos, out Vector2 normal);
                    underwaterPostProcessMaterialWrapper.SetVector(sp_HorizonPosNormal, new Vector4(pos.x, pos.y, normal.x, normal.y));
                }

                // We need to set the matrix ourselves. Maybe ViewportToWorldPoint has a bug.
                camera.projectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);

                var inverseViewProjectionMatrixRightEye = (camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right) * camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right)).inverse;
                underwaterPostProcessMaterialWrapper.SetMatrix(sp_InvViewProjectionRight, inverseViewProjectionMatrixRightEye);

                {
                    GetHorizonPosNormal(camera, Camera.MonoOrStereoscopicEye.Right, seaLevel, horizonSafetyMarginMultiplier, out Vector2 pos, out Vector2 normal);
                    underwaterPostProcessMaterialWrapper.SetVector(sp_HorizonPosNormalRight, new Vector4(pos.x, pos.y, normal.x, normal.y));
                }

                // Restore projection matrix.
                camera.projectionMatrix = projectionMatrix;
            }

            // Not sure why we need to do this - blit should set it...?
            // TODO(TRC):Now Re-enable this with a property wrapper abstraction
            // underwaterPostProcessMaterialWrapper.SetTexture(sp_MainTex, source);

            // Compute ambient lighting SH
            {
                // We could pass in a renderer which would prime this lookup. However it doesnt make sense to use an existing render
                // at different position, as this would then thrash it and negate the priming functionality. We could create a dummy invis GO
                // with a dummy Renderer which might be enoguh, but this is hacky enough that we'll wait for it to become a problem
                // rather than add a pre-emptive hack.

                UnityEngine.Profiling.Profiler.BeginSample("Underwater sample spherical harmonics");

                LightProbes.GetInterpolatedProbe(OceanRenderer.Instance.ViewCamera.transform.position, null, out SphericalHarmonicsL2 sphericalHarmonicsL2);
                sphericalHarmonicsL2.Evaluate(sphericalHarmonicsData._shDirections, sphericalHarmonicsData._ambientLighting);
                underwaterPostProcessMaterialWrapper.SetVector(sp_AmbientLighting, sphericalHarmonicsData._ambientLighting[0]);

                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        /// <summary>
        /// Compute intersection between the frustum far plane and the ocean plane, and return view space pos and normal
        /// for this horizon line.
        /// </summary>
        static void GetHorizonPosNormal(Camera camera, Camera.MonoOrStereoscopicEye eye, float seaLevel, float horizonSafetyMarginMultiplier, out Vector2 resultPos, out Vector2 resultNormal)
        {
            // Set up back points of frustum
            NativeArray<Vector3> v_screenXY_viewZ = new NativeArray<Vector3>(4, Allocator.Temp);
            NativeArray<Vector3> v_world = new NativeArray<Vector3>(4, Allocator.Temp);
            try
            {

                v_screenXY_viewZ[0] = new Vector3(0f, 0f, camera.farClipPlane);
                v_screenXY_viewZ[1] = new Vector3(0f, 1f, camera.farClipPlane);
                v_screenXY_viewZ[2] = new Vector3(1f, 1f, camera.farClipPlane);
                v_screenXY_viewZ[3] = new Vector3(1f, 0f, camera.farClipPlane);

                // Project out to world
                for (int i = 0; i < v_world.Length; i++)
                {
                    v_world[i] = camera.ViewportToWorldPoint(v_screenXY_viewZ[i], eye);
                }

                NativeArray<Vector2> intersectionsScreen = new NativeArray<Vector2>(2, Allocator.Temp);
                // This is only used to disambiguate the normal later. Could be removed if we were more careful with point order/indices below.
                NativeArray<Vector3> intersectionsWorld = new NativeArray<Vector3>(2, Allocator.Temp);
                try
                {
                    var resultCount = 0;

                    // Iterate over each back point
                    for (int i = 0; i < 4; i++)
                    {
                        // Get next back point, to obtain line segment between them
                        var inext = (i + 1) % 4;

                        // See if one point is above and one point is below sea level - then sign of the two differences
                        // will be different, and multiplying them will give a negative
                        if ((v_world[i].y - seaLevel) * (v_world[inext].y - seaLevel) < 0f)
                        {
                            // Proportion along line segment where intersection occurs
                            float prop = Mathf.Abs((seaLevel - v_world[i].y) / (v_world[inext].y - v_world[i].y));
                            intersectionsScreen[resultCount] = Vector2.Lerp(v_screenXY_viewZ[i], v_screenXY_viewZ[inext], prop);
                            intersectionsWorld[resultCount] = Vector3.Lerp(v_world[i], v_world[inext], prop);

                            resultCount++;
                        }
                    }

                    // Two distinct results - far plane intersects water
                    if (resultCount == 2 /*&& (props[1] - props[0]).sqrMagnitude > 0.000001f*/)
                    {
                        resultPos = intersectionsScreen[0];
                        var tangent = intersectionsScreen[0] - intersectionsScreen[1];
                        resultNormal.x = -tangent.y;
                        resultNormal.y = tangent.x;

                        // Disambiguate the normal. The tangent normal might go from left to right or right to left
                        // since we do not handle ordering of intersection points.
                        if (Vector3.Dot(intersectionsWorld[0] - intersectionsWorld[1], camera.transform.right) > 0f)
                        {
                            resultNormal = -resultNormal;
                        }

                        // Invert the normal if camera is upside down.
                        if (camera.transform.up.y <= 0f)
                        {
                            resultNormal = -resultNormal;
                        }

                        // The above will sometimes produce a normal that is inverted around 90° along the Z axis. Here
                        // we are using world up to make sure that water is world down.
                        {
                            var cameraFacing = Vector3.Dot(camera.transform.right, Vector3.up);
                            var normalFacing = Vector2.Dot(resultNormal, Vector2.right);

                            if (cameraFacing > 0.75f && normalFacing > 0.9f)
                            {
                                resultNormal = -resultNormal;
                            }
                            else if (cameraFacing < -0.75f && normalFacing < -0.9f)
                            {
                                resultNormal = -resultNormal;
                            }
                        }

                        // Calculate a scale value so that the multiplier is consistent when rotating camera. We need
                        // to do this because we are working in view space which is always 0-1.
                        {
                            var angleFromWorldNormal = Mathf.Abs(Vector2.Angle(Vector2.up, -resultNormal.normalized) / 90f);
                            if (angleFromWorldNormal > 1f)
                            {
                                angleFromWorldNormal = Mathf.Abs(2f - angleFromWorldNormal);
                            }
                            horizonSafetyMarginMultiplier /= Mathf.Lerp(1f, camera.aspect, angleFromWorldNormal);
                        }

                        // Get the sign (with zero) of the camera-to-sea-level to set the multiplier direction. We don't
                        // want the distance as it will influence the size of the safety margin which it might then
                        // appear in turbulent water edge cases.
                        var cameraToSeaLevelSign = seaLevel - camera.transform.position.y;
                        cameraToSeaLevelSign = cameraToSeaLevelSign > 0f ? 1f : cameraToSeaLevelSign < 0f ? -1f : 0f;

                        // We want to invert the direction of the multiplier when underwater.
                        horizonSafetyMarginMultiplier *= -cameraToSeaLevelSign;
                        // For compatibility so previous 0.01f property value is the same strength as before.
                        horizonSafetyMarginMultiplier *= 0.01f;
                        // We use the normal so the multiplier is applied in the correct direction.
                        resultPos += resultNormal.normalized * horizonSafetyMarginMultiplier;
                    }
                    else
                    {
                        // 1 or 0 results - far plane either touches ocean plane or is completely above/below
                        resultNormal = Vector2.up;
                        bool found = false;
                        resultPos = default;
                        for (int i = 0; i < 4; i++)
                        {
                            if (v_world[i].y < seaLevel)
                            {
                                // Underwater
                                resultPos = Vector2.zero;
                                found = true;
                                break;
                            }
                            else if (v_world[i].y > seaLevel)
                            {
                                // Underwater
                                resultPos = Vector2.up;
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            throw new System.Exception("GetHorizonPosNormal: Could not determine if far plane is above or below water.");
                        }
                    }
                }
                finally
                {
                    intersectionsScreen.Dispose();
                    intersectionsWorld.Dispose();
                }
            }
            finally
            {
                v_screenXY_viewZ.Dispose();
                v_world.Dispose();
            }
        }

        public static bool ShouldBeEnabled()
        {
            if (!Application.isPlaying)
            {
                return false;
            }
            if (OceanRenderer.Instance != null)
            {
                return OceanRenderer.Instance.ViewerHeightAboveWater < 2f;
            }
            else
            {
                return false;
            }
        }
    }
}
