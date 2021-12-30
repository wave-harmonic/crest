// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using Unity.Collections;
    using UnityEngine;
    using UnityEngine.Rendering;

    public partial class UnderwaterRenderer
    {
        const string k_ShaderPathUnderwaterEffect = "Hidden/Crest/Underwater/Underwater Effect";
        internal const string k_KeywordFullScreenEffect = "_FULL_SCREEN_EFFECT";
        internal const string k_KeywordDebugViewOceanMask = "_DEBUG_VIEW_OCEAN_MASK";
        internal const string k_KeywordDebugViewStencil = "_DEBUG_VIEW_STENCIL";

        internal static readonly int sp_CrestCameraColorTexture = Shader.PropertyToID("_CrestCameraColorTexture");
        internal static readonly int sp_CrestWaterVolumeStencil = Shader.PropertyToID("_CrestWaterVolumeStencil");
        static readonly int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");
        static readonly int sp_InvViewProjectionRight = Shader.PropertyToID("_InvViewProjectionRight");
        static readonly int sp_AmbientLighting = Shader.PropertyToID("_AmbientLighting");
        static readonly int sp_HorizonNormal = Shader.PropertyToID("_HorizonNormal");
        static readonly int sp_DataSliceOffset = Shader.PropertyToID("_DataSliceOffset");

        // If changed then see how mode is used to select the front-face pass and whether a mapping is required.
        // :UnderwaterRenderer.Mode
        enum EffectPass
        {
            FullScreen,
            VolumeFrontFace2D,
            VolumeFrontFace3D,
            VolumeFrontFaceVolume,
            VolumeBackFace,
            VolumeScene,
        }

        CommandBuffer _underwaterEffectCommandBuffer;
        PropertyWrapperMaterial _underwaterEffectMaterial;
        internal readonly UnderwaterSphericalHarmonicsData _sphericalHarmonicsData = new UnderwaterSphericalHarmonicsData();

        RenderTargetIdentifier _colorTarget = new RenderTargetIdentifier
        (
            BuiltinRenderTextureType.CameraTarget,
            0,
            CubemapFace.Unknown,
            -1
        );
        internal RenderTargetIdentifier _depthStencilTarget = new RenderTargetIdentifier
        (
            sp_CrestWaterVolumeStencil,
            0,
            CubemapFace.Unknown,
            -1
        );

        internal class UnderwaterSphericalHarmonicsData
        {
            internal Color[] _ambientLighting = new Color[1];
            internal Vector3[] _shDirections = { new Vector3(0.0f, 0.0f, 0.0f) };
        }

        void SetupUnderwaterEffect()
        {
            if (_underwaterEffectMaterial?.material == null)
            {
                _underwaterEffectMaterial = new PropertyWrapperMaterial(k_ShaderPathUnderwaterEffect);
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
            temporaryColorBuffer.name = "_CrestCameraColorTexture";

            UpdatePostProcessMaterial(
                _mode,
                _camera,
                _underwaterEffectMaterial,
                _sphericalHarmonicsData,
                _meniscus,
                _firstRender || _copyOceanMaterialParamsEachFrame,
                _debug._viewOceanMask,
                _debug._viewStencil,
                _filterOceanData
            );

            // Call after UpdatePostProcessMaterial as it copies material from ocean which will overwrite this.
            SetInverseViewProjectionMatrix(_underwaterEffectMaterial.material);

            _underwaterEffectCommandBuffer.Clear();

            // Create a separate stencil buffer context by copying the depth texture.
            if (UseStencilBufferOnEffect)
            {
                descriptor.colorFormat = RenderTextureFormat.Depth;
                descriptor.depthBufferBits = 24;
                // bindMS is necessary in this case for depth.
                descriptor.SetMSAASamples(_camera);
                descriptor.bindMS = descriptor.msaaSamples > 1;

                _underwaterEffectCommandBuffer.GetTemporaryRT(sp_CrestWaterVolumeStencil, descriptor);

                // Use blit for MSAA. We should be able to use CopyTexture. Might be the following bug:
                // https://issuetracker.unity3d.com/product/unity/issues/guid/1308132
                if (Helpers.IsMSAAEnabled(_camera))
                {
                    // Blit with a depth write shader to populate the depth buffer.
                    Helpers.Blit(_underwaterEffectCommandBuffer, _depthStencilTarget, Helpers.UtilityMaterial, (int)Helpers.UtilityPass.CopyDepth);
                }
                else
                {
                    // Copy depth then clear stencil.
                    _underwaterEffectCommandBuffer.CopyTexture(BuiltinRenderTextureType.Depth, _depthStencilTarget);
                    Helpers.Blit(_underwaterEffectCommandBuffer, _depthStencilTarget, Helpers.UtilityMaterial, (int)Helpers.UtilityPass.ClearStencil);
                }
            }

            // Copy the color buffer into a texture.
            if (Helpers.IsMSAAEnabled(_camera))
            {
                // Use blit if MSAA is active because transparents were not included with CopyTexture.
                // This appears to be a bug. CopyTexture + MSAA works fine when the stencil is required.
                _underwaterEffectCommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, temporaryColorBuffer);
            }
            else
            {
                // Copy the frame buffer as we cannot read/write at the same time. If it causes problems, replace with Blit.
                _underwaterEffectCommandBuffer.CopyTexture(BuiltinRenderTextureType.CameraTarget, temporaryColorBuffer);
            }

            if (UseStencilBufferOnEffect)
            {
                _underwaterEffectCommandBuffer.SetRenderTarget(_colorTarget, _depthStencilTarget);
            }
            else
            {
                _underwaterEffectCommandBuffer.SetRenderTarget(_colorTarget);
            }

            _underwaterEffectMaterial.SetTexture(sp_CrestCameraColorTexture, temporaryColorBuffer);

            ExecuteEffect(_underwaterEffectCommandBuffer, _underwaterEffectMaterial.material);

            RenderTexture.ReleaseTemporary(temporaryColorBuffer);
            if (UseStencilBufferOnEffect)
            {
                _underwaterEffectCommandBuffer.ReleaseTemporaryRT(sp_CrestWaterVolumeStencil);
            }
        }

        internal void ExecuteEffect(CommandBuffer buffer, Material material, MaterialPropertyBlock properties = null)
        {
            if (_mode == Mode.FullScreen)
            {
                buffer.DrawProcedural
                (
                    Matrix4x4.identity,
                    material,
                    shaderPass: (int)EffectPass.FullScreen,
                    MeshTopology.Triangles,
                    vertexCount: 3,
                    instanceCount: 1,
                    properties
                );
            }
            else
            {
                if (_mode == Mode.Portal)
                {
                    buffer.SetInvertCulling(_invertCulling);
                }

                buffer.DrawMesh
                (
                    _volumeGeometry.mesh,
                    _volumeGeometry.transform.localToWorldMatrix,
                    material,
                    submeshIndex: 0,
                    // Use the mode to select the front-face pass. If the front-face passes in the shader change, then
                    // a mapping between Mode and EffectPass will need to be made.
                    // :UnderwaterRenderer.Mode
                    shaderPass: (int)_mode,
                    properties
                );

                buffer.SetInvertCulling(false);

                if (_mode == Mode.VolumeFlyThrough)
                {
                    buffer.DrawMesh
                    (
                        _volumeGeometry.mesh,
                        _volumeGeometry.transform.localToWorldMatrix,
                        material,
                        submeshIndex: 0,
                        shaderPass: (int)EffectPass.VolumeBackFace,
                        properties
                    );

                    buffer.DrawProcedural
                    (
                        Matrix4x4.identity,
                        material,
                        shaderPass: (int)EffectPass.VolumeScene,
                        MeshTopology.Triangles,
                        vertexCount: 3,
                        instanceCount: 1,
                        properties
                    );
                }
            }
        }

        internal static void UpdatePostProcessMaterial(
            Mode mode,
            Camera camera,
            PropertyWrapperMaterial underwaterPostProcessMaterialWrapper,
            UnderwaterSphericalHarmonicsData sphericalHarmonicsData,
            bool isMeniscusEnabled,
            bool copyParamsFromOceanMaterial,
            bool debugViewPostProcessMask,
            bool debugViewStencil,
            int dataSliceOffset
        )
        {
            Material underwaterPostProcessMaterial = underwaterPostProcessMaterialWrapper.material;
            if (copyParamsFromOceanMaterial)
            {
                // Measured this at approx 0.05ms on dell laptop
                underwaterPostProcessMaterial.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
            }

            underwaterPostProcessMaterial.SetVector("_DepthFogDensity", OceanRenderer.Instance.UnderwaterDepthFogDensity);

            // Enabling/disabling keywords each frame don't seem to have large measurable overhead
            underwaterPostProcessMaterial.SetKeyword(k_KeywordDebugViewOceanMask, debugViewPostProcessMask);
            underwaterPostProcessMaterial.SetKeyword(k_KeywordDebugViewStencil, debugViewStencil);
            underwaterPostProcessMaterial.SetKeyword("CREST_MENISCUS", isMeniscusEnabled);

            // We sample shadows at the camera position which will be the first slice.
            // We also use this for caustics to get the displacement.
            underwaterPostProcessMaterial.SetFloat(LodDataMgr.sp_LD_SliceIndex, 0);
            underwaterPostProcessMaterial.SetInt(sp_DataSliceOffset, dataSliceOffset);

            LodDataMgrAnimWaves.Bind(underwaterPostProcessMaterialWrapper);
            LodDataMgrSeaFloorDepth.Bind(underwaterPostProcessMaterialWrapper);
            LodDataMgrShadow.Bind(underwaterPostProcessMaterialWrapper);

            if (mode == Mode.FullScreen)
            {
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

                underwaterPostProcessMaterial.SetKeyword(k_KeywordFullScreenEffect, forceFullShader);
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

                LightProbes.GetInterpolatedProbe(OceanRenderer.Instance.ViewCamera.transform.position, null, out var sphericalHarmonicsL2);
                sphericalHarmonicsL2.Evaluate(sphericalHarmonicsData._shDirections, sphericalHarmonicsData._ambientLighting);
                underwaterPostProcessMaterial.SetVector(sp_AmbientLighting, sphericalHarmonicsData._ambientLighting[0]);

                UnityEngine.Profiling.Profiler.EndSample();
            }
        }
    }
}
