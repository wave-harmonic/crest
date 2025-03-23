// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Internal;

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

        public static partial class ShaderIDs
        {
            // Local
            public static readonly int s_HorizonNormal = Shader.PropertyToID("_HorizonNormal");

            // Global
            public static readonly int s_CrestCameraColorTexture = Shader.PropertyToID("_CrestCameraColorTexture");
            public static readonly int s_CrestWaterVolumeStencil = Shader.PropertyToID("_CrestWaterVolumeStencil");
            public static readonly int s_CrestAmbientLighting = Shader.PropertyToID("_CrestAmbientLighting");
            public static readonly int s_CrestDataSliceOffset = Shader.PropertyToID("_CrestDataSliceOffset");
            public static readonly int s_CrestDepthFogDensity = Shader.PropertyToID("_CrestDepthFogDensity");
            public static readonly int s_CrestDiffuse = Shader.PropertyToID("_CrestDiffuse");
            public static readonly int s_CrestDiffuseGrazing = Shader.PropertyToID("_CrestDiffuseGrazing");
            public static readonly int s_CrestDiffuseShadow = Shader.PropertyToID("_CrestDiffuseShadow");
            public static readonly int s_CrestSubSurfaceColour = Shader.PropertyToID("_CrestSubSurfaceColour");
            public static readonly int s_CrestSubSurfaceSun = Shader.PropertyToID("_CrestSubSurfaceSun");
            public static readonly int s_CrestSubSurfaceBase = Shader.PropertyToID("_CrestSubSurfaceBase");
            public static readonly int s_CrestSubSurfaceSunFallOff = Shader.PropertyToID("_CrestSubSurfaceSunFallOff");

            // Built-ins
            public static readonly int s_WorldSpaceLightPos0 = Shader.PropertyToID("_WorldSpaceLightPos0");
            public static readonly int s_LightColor0 = Shader.PropertyToID("_LightColor0");
        }


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
        internal Material _currentOceanMaterial;
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
            ShaderIDs.s_CrestWaterVolumeStencil,
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
#if UNITY_EDITOR
            if (!IsFogEnabledForEditorCamera(_camera))
            {
                _underwaterEffectCommandBuffer?.Clear();
                return;
            }
#endif

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
                this,
                _mode,
                _camera,
                _underwaterEffectMaterial,
                _sphericalHarmonicsData,
                _meniscus,
                _firstRender || _copyOceanMaterialParamsEachFrame,
                _debug._viewOceanMask,
                _debug._viewStencil,
                _filterOceanData,
                ref _currentOceanMaterial,
                _enableShaderAPI
            );

            // Call after UpdatePostProcessMaterial as it copies material from ocean which will overwrite this.
            SetInverseViewProjectionMatrix(_underwaterEffectMaterial.material);

            _underwaterEffectCommandBuffer.Clear();

            if (RenderSettings.sun != null)
            {
                // Unity does not set up lighting for us so we will get the last value which could incorrect.
                // SetGlobalColor is just an alias for SetGlobalVector (no color space conversion like Material.SetColor):
                // https://docs.unity3d.com/2017.4/Documentation/ScriptReference/Shader.SetGlobalColor.html
                _underwaterEffectCommandBuffer.SetGlobalVector(ShaderIDs.s_LightColor0, RenderSettings.sun.FinalColor());
                _underwaterEffectCommandBuffer.SetGlobalVector(ShaderIDs.s_WorldSpaceLightPos0, -RenderSettings.sun.transform.forward);
            }

            // Create a separate stencil buffer context by copying the depth texture.
            if (UseStencilBufferOnEffect)
            {
                descriptor.colorFormat = RenderTextureFormat.Depth;
                descriptor.depthBufferBits = 24;
                // bindMS is necessary in this case for depth.
                descriptor.SetMSAASamples(_camera);
                descriptor.bindMS = descriptor.msaaSamples > 1;

                _underwaterEffectCommandBuffer.GetTemporaryRT(ShaderIDs.s_CrestWaterVolumeStencil, descriptor);

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
            _underwaterEffectCommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, temporaryColorBuffer);

            if (UseStencilBufferOnEffect)
            {
                _underwaterEffectCommandBuffer.SetRenderTarget(_colorTarget, _depthStencilTarget);
            }
            else
            {
                _underwaterEffectCommandBuffer.SetRenderTarget(_colorTarget);
            }

            _underwaterEffectMaterial.SetTexture(ShaderIDs.s_CrestCameraColorTexture, temporaryColorBuffer);

            ExecuteEffect(_underwaterEffectCommandBuffer, _underwaterEffectMaterial.material);

            RenderTexture.ReleaseTemporary(temporaryColorBuffer);
            if (UseStencilBufferOnEffect)
            {
                _underwaterEffectCommandBuffer.ReleaseTemporaryRT(ShaderIDs.s_CrestWaterVolumeStencil);
            }
        }

        internal void ExecuteEffect(CommandBuffer buffer, Material material, MaterialPropertyBlock properties = null)
        {
            if (_mode == Mode.FullScreen || _volumeGeometry == null)
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
                    _volumeGeometry.sharedMesh,
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
                        _volumeGeometry.sharedMesh,
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

        internal static void UpdateGlobals(Material oceanMaterial)
        {
            // We will have the wrong color values if we do not use linear:
            // https://forum.unity.com/threads/fragment-shader-output-colour-has-incorrect-values-when-hardcoded.377657/
            Shader.SetGlobalColor(ShaderIDs.s_CrestDiffuse, oceanMaterial.GetColor(OceanRenderer.ShaderIDs.s_Diffuse).MaybeLinear());
            Shader.SetGlobalColor(ShaderIDs.s_CrestDiffuseGrazing, oceanMaterial.GetColor(OceanRenderer.ShaderIDs.s_DiffuseGrazing).MaybeLinear());
            Shader.SetGlobalColor(ShaderIDs.s_CrestDiffuseShadow, oceanMaterial.GetColor(OceanRenderer.ShaderIDs.s_DiffuseShadow).MaybeLinear());
            Shader.SetGlobalColor(ShaderIDs.s_CrestSubSurfaceColour, oceanMaterial.GetColor(OceanRenderer.ShaderIDs.s_SubSurfaceColour).MaybeLinear());
            Shader.SetGlobalFloat(ShaderIDs.s_CrestSubSurfaceSun, oceanMaterial.GetFloat(OceanRenderer.ShaderIDs.s_SubSurfaceSun));
            Shader.SetGlobalFloat(ShaderIDs.s_CrestSubSurfaceBase, oceanMaterial.GetFloat(OceanRenderer.ShaderIDs.s_SubSurfaceBase));
            Shader.SetGlobalFloat(ShaderIDs.s_CrestSubSurfaceSunFallOff, oceanMaterial.GetFloat(OceanRenderer.ShaderIDs.s_SubSurfaceSunFallOff));

            Helpers.SetGlobalKeyword("CREST_SUBSURFACESCATTERING_ON", oceanMaterial.IsKeywordEnabled("_SUBSURFACESCATTERING_ON"));
            Helpers.SetGlobalKeyword("CREST_SHADOWS_ON", oceanMaterial.IsKeywordEnabled("_SHADOWS_ON"));
        }

        internal static void UpdatePostProcessMaterial(
            UnderwaterRenderer renderer,
            Mode mode,
            Camera camera,
            PropertyWrapperMaterial underwaterPostProcessMaterialWrapper,
            UnderwaterSphericalHarmonicsData sphericalHarmonicsData,
            bool isMeniscusEnabled,
            bool copyParamsFromOceanMaterial,
            bool debugViewPostProcessMask,
            bool debugViewStencil,
            int dataSliceOffset,
            ref Material currentOceanMaterial,
            bool setGlobalShaderData
        )
        {
            Material underwaterPostProcessMaterial = underwaterPostProcessMaterialWrapper.material;

            // Copy ocean material parameters to underwater material.
            {
                WaterBody dominantWaterBody = null;
                var material = OceanRenderer.Instance.OceanMaterial;
                // Grab material from a water body if camera is within its XZ bounds.
                foreach (var body in WaterBody.WaterBodies)
                {
                    if (body._overrideMaterial == null)
                    {
                        continue;
                    }

                    var bounds = body.AABB;
                    var position = camera.transform.position;
                    var contained =
                        position.x >= bounds.min.x && position.x <= bounds.max.x &&
                        position.z >= bounds.min.z && position.z <= bounds.max.z;
                    if (contained)
                    {
                        dominantWaterBody = body;
                        material = body._overrideMaterial;
                        // Water bodies should not overlap so grab the first one.
                        break;
                    }
                }

                if (copyParamsFromOceanMaterial || material != currentOceanMaterial)
                {
                    currentOceanMaterial = material;

                    if (material != null)
                    {
                        // Measured this at approx 0.05ms on Dell laptop.
                        underwaterPostProcessMaterial.CopyPropertiesFromMaterial(material);

                        AfterCopyMaterial?.Invoke(material);

                        if (setGlobalShaderData)
                        {
                            UpdateGlobals(material);
                        }
                    }
                }

                Vector3 depthFogDensity;

                if (!IsCullable)
                {
                    depthFogDensity = dominantWaterBody == null
                        ? OceanRenderer.Instance.OceanMaterial.GetVector(OceanRenderer.ShaderIDs.s_DepthFogDensity) * renderer._depthFogDensityFactor
                        : dominantWaterBody._overrideMaterial.GetVector(OceanRenderer.ShaderIDs.s_DepthFogDensity) * renderer._depthFogDensityFactor;
                }
                else
                {
                    depthFogDensity = dominantWaterBody == null
                        ? OceanRenderer.Instance.UnderwaterDepthFogDensity : dominantWaterBody.UnderwaterDepthFogDensity;
                }

                if (setGlobalShaderData)
                {
                    Shader.SetGlobalVector(ShaderIDs.s_CrestDepthFogDensity, depthFogDensity);
                }

                underwaterPostProcessMaterial.SetVector(OceanRenderer.ShaderIDs.s_DepthFogDensity, depthFogDensity);
            }

            // Enabling/disabling keywords each frame don't seem to have large measurable overhead
            underwaterPostProcessMaterial.SetKeyword(k_KeywordDebugViewOceanMask, debugViewPostProcessMask);
            underwaterPostProcessMaterial.SetKeyword(k_KeywordDebugViewStencil, debugViewStencil);
            underwaterPostProcessMaterial.SetKeyword("CREST_MENISCUS", isMeniscusEnabled);

            // We sample shadows at the camera position. Pass a user defined slice offset for smoothing out detail.
            Helpers.SetShaderInt(underwaterPostProcessMaterial, ShaderIDs.s_CrestDataSliceOffset, dataSliceOffset, setGlobalShaderData);
            // We use this for caustics to get the displacement.
            underwaterPostProcessMaterial.SetInt(LodDataMgr.sp_LD_SliceIndex, 0);

            LodDataMgrAnimWaves.Bind(underwaterPostProcessMaterialWrapper);
            LodDataMgrSeaFloorDepth.Bind(underwaterPostProcessMaterialWrapper);
            LodDataMgrShadow.Bind(underwaterPostProcessMaterialWrapper);

            if (mode == Mode.FullScreen)
            {
                float seaLevel = OceanRenderer.Instance.SeaLevel;
                var heightAboveWater = renderer != null ? renderer.HeightAboveWater : OceanRenderer.Instance.ViewerHeightAboveWater;

                // We don't both setting the horizon value if we know we are going to be having to apply the effect
                // full-screen anyway.
                var forceFullShader = heightAboveWater < -2f;
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

                underwaterPostProcessMaterial.SetVector(ShaderIDs.s_HorizonNormal, projectedNormal);
            }

            // Compute ambient lighting SH.
            {
                // We could pass in a renderer which would prime this lookup. However it doesnt make sense to use an existing render
                // at different position, as this would then thrash it and negate the priming functionality. We could create a dummy invis GO
                // with a dummy Renderer which might be enough, but this is hacky enough that we'll wait for it to become a problem
                // rather than add a pre-emptive hack.
                UnityEngine.Profiling.Profiler.BeginSample("Underwater Sample Spherical Harmonics");
                LightProbes.GetInterpolatedProbe(camera.transform.position, null, out var sphericalHarmonicsL2);
                sphericalHarmonicsL2.Evaluate(sphericalHarmonicsData._shDirections, sphericalHarmonicsData._ambientLighting);
                Helpers.SetShaderVector(underwaterPostProcessMaterial, ShaderIDs.s_CrestAmbientLighting, sphericalHarmonicsData._ambientLighting[0], setGlobalShaderData);
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }
    }
}
