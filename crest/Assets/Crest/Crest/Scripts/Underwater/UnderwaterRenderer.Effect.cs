// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using Unity.Collections;
    using UnityEngine;
    using UnityEngine.Rendering;

    public partial class UnderwaterRenderer
    {
        const string SHADER_UNDERWATER_EFFECT = "Hidden/Crest/Underwater/Underwater Effect";
        internal const string FULL_SCREEN_EFFECT = "_FULL_SCREEN_EFFECT";
        internal const string DEBUG_VIEW_OCEAN_MASK = "_DEBUG_VIEW_OCEAN_MASK";

        internal static readonly int sp_CrestCameraColorTexture = Shader.PropertyToID("_CrestCameraColorTexture");
        static readonly int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");
        static readonly int sp_InvViewProjectionRight = Shader.PropertyToID("_InvViewProjectionRight");
        static readonly int sp_AmbientLighting = Shader.PropertyToID("_AmbientLighting");
        static readonly int sp_HorizonNormal = Shader.PropertyToID("_HorizonNormal");
        static readonly int sp_DataSliceOffset = Shader.PropertyToID("_DataSliceOffset");

        CommandBuffer _underwaterEffectCommandBuffer;
        PropertyWrapperMaterial _underwaterEffectMaterial;
        internal readonly UnderwaterSphericalHarmonicsData _sphericalHarmonicsData = new UnderwaterSphericalHarmonicsData();

        internal class UnderwaterSphericalHarmonicsData
        {
            internal Color[] _ambientLighting = new Color[1];
            internal Vector3[] _shDirections = { new Vector3(0.0f, 0.0f, 0.0f) };
        }

        void SetupUnderwaterEffect()
        {
            if (_underwaterEffectMaterial?.material == null)
            {
                _underwaterEffectMaterial = new PropertyWrapperMaterial(SHADER_UNDERWATER_EFFECT);
            }

            if (_underwaterEffectCommandBuffer == null)
            {
                _underwaterEffectCommandBuffer = new CommandBuffer()
                {
                    name = "Underwater Pass",
                };
            }
        }

        void OnPreRenderUnderwaterEffect()
        {
            // Ensure legacy underwater fog is disabled.
            if (_firstRender)
            {
                OceanRenderer.Instance.OceanMaterial.DisableKeyword("_OLD_UNDERWATER");
            }

            RenderTextureDescriptor descriptor = XRHelpers.GetRenderTextureDescriptor(_camera);
            descriptor.useDynamicScale = _camera.allowDynamicResolution;
            // Format must be correct for CopyTexture to work. Hopefully this is good enough.
            if (_camera.allowHDR)
            {
                descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            }

            var temporaryColorBuffer = RenderTexture.GetTemporary(descriptor);

            UpdatePostProcessMaterial(
                _camera,
                _underwaterEffectMaterial,
                _sphericalHarmonicsData,
                _meniscus,
                _firstRender || _copyOceanMaterialParamsEachFrame,
                _debug._viewOceanMask,
                _filterOceanData
            );

            _underwaterEffectCommandBuffer.Clear();

            if (_camera.allowMSAA)
            {
                // Use blit if MSAA is active because transparents were not included with CopyTexture.
                // Not sure if we need an MSAA resolve? Not sure how to do that...
                _underwaterEffectCommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, temporaryColorBuffer);
            }
            else
            {
                // Copy the frame buffer as we cannot read/write at the same time. If it causes problems, replace with Blit.
                _underwaterEffectCommandBuffer.CopyTexture(BuiltinRenderTextureType.CameraTarget, temporaryColorBuffer);
            }

            _underwaterEffectMaterial.SetTexture(sp_CrestCameraColorTexture, temporaryColorBuffer);

            _underwaterEffectCommandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown, -1);
            _underwaterEffectCommandBuffer.DrawProcedural(Matrix4x4.identity, _underwaterEffectMaterial.material, -1, MeshTopology.Triangles, 3, 1);

            RenderTexture.ReleaseTemporary(temporaryColorBuffer);
        }

        internal static void UpdatePostProcessMaterial(
            Camera camera,
            PropertyWrapperMaterial underwaterPostProcessMaterialWrapper,
            UnderwaterSphericalHarmonicsData sphericalHarmonicsData,
            bool isMeniscusEnabled,
            bool copyParamsFromOceanMaterial,
            bool debugViewPostProcessMask,
            int dataSliceOffset
        )
        {
            Material underwaterPostProcessMaterial = underwaterPostProcessMaterialWrapper.material;
            if (copyParamsFromOceanMaterial)
            {
                // Measured this at approx 0.05ms on dell laptop
                underwaterPostProcessMaterial.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
            }

            // Enable/Disable meniscus.
            if (isMeniscusEnabled)
            {
                underwaterPostProcessMaterial.EnableKeyword("CREST_MENISCUS");
            }
            else
            {
                underwaterPostProcessMaterial.DisableKeyword("CREST_MENISCUS");
            }

            // Enabling/disabling keywords each frame don't seem to have large measurable overhead
            if (debugViewPostProcessMask)
            {
                underwaterPostProcessMaterial.EnableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }
            else
            {
                underwaterPostProcessMaterial.DisableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }

            // We sample shadows at the camera position which will be the first slice.
            // We also use this for caustics to get the displacement.
            underwaterPostProcessMaterial.SetFloat(LodDataMgr.sp_LD_SliceIndex, 0);
            underwaterPostProcessMaterial.SetInt(sp_DataSliceOffset, dataSliceOffset);

            LodDataMgrAnimWaves.Bind(underwaterPostProcessMaterialWrapper);
            LodDataMgrSeaFloorDepth.Bind(underwaterPostProcessMaterialWrapper);
            LodDataMgrShadow.Bind(underwaterPostProcessMaterialWrapper);

            float seaLevel = OceanRenderer.Instance.SeaLevel;

            // We don't both setting the horizon value if we know we are going to be having to apply the effect
            // full-screen anyway.
            var forceFullShader = OceanRenderer.Instance.ViewerHeightAboveWater < -2f;
            if (!forceFullShader)
            {
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

                forceFullShader = (cameraYPosition + nearPlaneFrustumWorldHeight + maxOceanVerticalDisplacement) <= seaLevel;
            }

            if (forceFullShader)
            {
                underwaterPostProcessMaterial.EnableKeyword(FULL_SCREEN_EFFECT);
            }
            else
            {
                underwaterPostProcessMaterial.DisableKeyword(FULL_SCREEN_EFFECT);
            }

            // Have to set these explicitly as the built-in transforms aren't in world-space for the blit function
            if (XRHelpers.IsSinglePass)
            {
                // NOTE: Not needed for HDRP.
                underwaterPostProcessMaterial.SetMatrix(sp_InvViewProjection, (GL.GetGPUProjectionMatrix(XRHelpers.LeftEyeProjectionMatrix, false) * XRHelpers.LeftEyeViewMatrix).inverse);
                underwaterPostProcessMaterial.SetMatrix(sp_InvViewProjectionRight, (GL.GetGPUProjectionMatrix(XRHelpers.RightEyeProjectionMatrix, false) * XRHelpers.RightEyeViewMatrix).inverse);
            }
            else
            {
                // NOTE: Not needed for HDRP.
                var inverseViewProjectionMatrix = (GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * camera.worldToCameraMatrix).inverse;
                underwaterPostProcessMaterial.SetMatrix(sp_InvViewProjection, inverseViewProjectionMatrix);
            }

            // Project ocean normal onto camera plane.
            {
                var projectedNormal = new Vector2
                (
                    Vector3.Dot(Vector3.up, camera.transform.right),
                    Vector3.Dot(Vector3.up, camera.transform.up)
                );

                underwaterPostProcessMaterial.SetVector(sp_HorizonNormal, projectedNormal);
            }

            // Compute ambient lighting SH
            {
                // We could pass in a renderer which would prime this lookup. However it doesnt make sense to use an existing render
                // at different position, as this would then thrash it and negate the priming functionality. We could create a dummy invis GO
                // with a dummy Renderer which might be enoguh, but this is hacky enough that we'll wait for it to become a problem
                // rather than add a pre-emptive hack.

                UnityEngine.Profiling.Profiler.BeginSample("Underwater sample spherical harmonics");

                LightProbes.GetInterpolatedProbe(OceanRenderer.Instance.ViewCamera.transform.position, null, out SphericalHarmonicsL2 sphericalHarmonicsL2);
                sphericalHarmonicsL2.Evaluate(sphericalHarmonicsData._shDirections, sphericalHarmonicsData._ambientLighting);
                underwaterPostProcessMaterial.SetVector(sp_AmbientLighting, sphericalHarmonicsData._ambientLighting[0]);

                UnityEngine.Profiling.Profiler.EndSample();
            }
        }
    }
}
