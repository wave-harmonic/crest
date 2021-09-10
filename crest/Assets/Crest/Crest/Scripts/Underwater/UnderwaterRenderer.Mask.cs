// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    public partial class UnderwaterRenderer
    {
        internal const string SHADER_OCEAN_MASK = "Hidden/Crest/Underwater/Ocean Mask";
        internal const int k_ShaderPassOceanSurfaceMask = 0;
        internal const int k_ShaderPassOceanHorizonMask = 1;

        public static readonly int sp_CrestOceanMaskTexture = Shader.PropertyToID("_CrestOceanMaskTexture");
        public static readonly int sp_CrestOceanMaskDepthTexture = Shader.PropertyToID("_CrestOceanMaskDepthTexture");
        public static readonly int sp_CrestOceanOccluderMaskTexture = Shader.PropertyToID("_CrestOceanOccluderMaskTexture");
        public static readonly int sp_CrestOceanOccluderMaskDepthTexture = Shader.PropertyToID("_CrestOceanOccluderMaskDepthTexture");
        public static readonly int sp_FarPlaneOffset = Shader.PropertyToID("_FarPlaneOffset");

        internal Plane[] _cameraFrustumPlanes;
        CommandBuffer _oceanMaskCommandBuffer;
        PropertyWrapperMaterial _oceanMaskMaterial;
        RenderTexture _maskTexture;
        RenderTexture _depthTexture;

        RenderTexture _oceanOccluderMask;
        RenderTexture _oceanOccluderDepthBuffer;
        PropertyWrapperMaterial _oceanOccluderMaskMaterial = null;

        List<OceanOccluder> _oceanOccluderMasksToRender = new List<OceanOccluder>();
        public List<OceanOccluder> OceanOccluderMasksToRender => _oceanOccluderMasksToRender;

        const string OCEAN_OCCLUDER_MASK = "Crest/Underwater/Ocean Occluder Mask";

        public void RegisterOceanOccluder(OceanOccluder _oceanOccluder)
        {
            _oceanOccluderMasksToRender.Add(_oceanOccluder);
        }

        void SetupOceanMask()
        {
            if (_oceanMaskMaterial?.material == null)
            {
                _oceanMaskMaterial = new PropertyWrapperMaterial(SHADER_OCEAN_MASK);
            }

            if (_oceanOccluderMaskMaterial?.material == null)
            {
                _oceanOccluderMaskMaterial = new PropertyWrapperMaterial(OCEAN_OCCLUDER_MASK);
            }

            if (_oceanMaskCommandBuffer == null)
            {
                _oceanMaskCommandBuffer = new CommandBuffer()
                {
                    name = "Ocean Mask",
                };
            }

            _oceanOccluderMasksToRender = new List<OceanOccluder>();
        }

        void OnPreRenderOceanMask()
        {
            RenderTextureDescriptor descriptor = XRHelpers.GetRenderTextureDescriptor(_camera);
            descriptor.useDynamicScale = _camera.allowDynamicResolution;

            InitialiseMaskTextures(descriptor, ref _maskTexture, ref _depthTexture, true);
            InitialiseMaskTextures(descriptor, ref _oceanOccluderMask, ref _oceanOccluderDepthBuffer, false);

            _oceanMaskCommandBuffer.Clear();

            SetUnderwaterGlobals(_oceanMaskCommandBuffer, _camera, _sphericalHarmonicsData);

            _oceanMaskCommandBuffer.SetRenderTarget(_oceanOccluderMask.colorBuffer, _oceanOccluderDepthBuffer.depthBuffer);
            _oceanMaskCommandBuffer.ClearRenderTarget(true, true, Color.white);
            _oceanMaskCommandBuffer.SetViewProjectionMatrices(GetComponent<Camera>().worldToCameraMatrix, GetComponent<Camera>().projectionMatrix);

            foreach (OceanOccluder underwaterEffectFilter in _oceanOccluderMasksToRender)
            {
                _oceanMaskCommandBuffer.SetGlobalFloat(OceanOccluder.sp_OceanOccluderType, (float)underwaterEffectFilter.OccluderType);
                _oceanMaskCommandBuffer.DrawRenderer(underwaterEffectFilter.Renderer, _oceanOccluderMaskMaterial.material);
            }

            _oceanMaskCommandBuffer.SetGlobalTexture(sp_CrestOceanOccluderMaskTexture, _oceanOccluderMask);
            _oceanMaskCommandBuffer.SetGlobalTexture(sp_CrestOceanOccluderMaskDepthTexture, _oceanOccluderDepthBuffer);

            // Passing -1 to depth slice binds all slices. Important for XR SPI to work in both eyes.
            _oceanMaskCommandBuffer.SetRenderTarget(_maskTexture.colorBuffer, _depthTexture.depthBuffer, mipLevel: 0, CubemapFace.Unknown, depthSlice: -1);
            _oceanMaskCommandBuffer.ClearRenderTarget(true, true, Color.black);
            _oceanMaskCommandBuffer.SetGlobalTexture(sp_CrestOceanMaskTexture, _maskTexture.colorBuffer);
            _oceanMaskCommandBuffer.SetGlobalTexture(sp_CrestOceanMaskDepthTexture, _depthTexture.depthBuffer);

            PopulateOceanMask(
                _oceanMaskCommandBuffer,
                _camera,
                OceanRenderer.Instance.Tiles,
                _cameraFrustumPlanes,
                _oceanMaskMaterial.material,
                _farPlaneMultiplier,
                _debug._disableOceanMask
            );

            _oceanOccluderMasksToRender.Clear();
        }

        internal static void InitialiseMaskTextures(RenderTextureDescriptor desc, ref RenderTexture textureMask, ref RenderTexture depthBuffer, bool isOcean)
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
                textureMask.name = isOcean ? "Ocean Mask" : "Ocean Occluder Mask";
                // @Memory: We could investigate making this an 8-bit texture instead to reduce GPU memory usage.
                // We could also potentially try a half res mask as the mensicus could mask res issues.
                textureMask.format = RenderTextureFormat.RHalf;
                textureMask.Create();

                depthBuffer = new RenderTexture(desc);
                depthBuffer.depth = 24;
                depthBuffer.enableRandomWrite = false;
                depthBuffer.name = isOcean ? "Ocean Depth" : "Ocean Occluder Depth";
                depthBuffer.format = RenderTextureFormat.Depth;
                depthBuffer.Create();
            }
        }

        // Populates a screen space mask which will inform the underwater postprocess. As a future optimisation we may
        // be able to avoid this pass completely if we can reuse the camera depth after transparents are rendered.
        internal static void PopulateOceanMask(
            CommandBuffer commandBuffer,
            Camera camera,
            List<OceanChunkRenderer> chunksToRender,
            Plane[] frustumPlanes,
            Material oceanMaskMaterial,
            float farPlaneMultiplier,
            bool debugDisableOceanMask
        )
        {
            // Render horizon into mask using a fullscreen triangle at the far plane. Horizon must be rendered first or
            // it will overwrite the mask with incorrect values.
            {
                // Have to set these explicitly as the built-in transforms aren't in world-space for the blit function.
                if (XRHelpers.IsSinglePass)
                {
                    // NOTE: Not needed for HDRP.
                    oceanMaskMaterial.SetMatrix(sp_CrestInvViewProjection, (GL.GetGPUProjectionMatrix(XRHelpers.LeftEyeProjectionMatrix, false) * XRHelpers.LeftEyeViewMatrix).inverse);
                    oceanMaskMaterial.SetMatrix(sp_CrestInvViewProjectionRight, (GL.GetGPUProjectionMatrix(XRHelpers.RightEyeProjectionMatrix, false) * XRHelpers.RightEyeViewMatrix).inverse);
                }
                else
                {
                    // NOTE: Not needed for HDRP.
                    var inverseViewProjectionMatrix = (GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * camera.worldToCameraMatrix).inverse;
                    oceanMaskMaterial.SetMatrix(sp_CrestInvViewProjection, inverseViewProjectionMatrix);
                }

                // Compute _ZBufferParams x and y values.
                float zBufferParamsX; float zBufferParamsY;
                if (SystemInfo.usesReversedZBuffer)
                {
                    zBufferParamsY = 1f;
                    zBufferParamsX = camera.farClipPlane / camera.nearClipPlane - 1f;
                }
                else
                {
                    zBufferParamsY = camera.farClipPlane / camera.nearClipPlane;
                    zBufferParamsX = 1f - zBufferParamsY;
                }

                // Take 0-1 linear depth and convert non-linear depth. Scripted for performance saving.
                var farPlaneLerp = (1f - zBufferParamsY * farPlaneMultiplier) / (zBufferParamsX * farPlaneMultiplier);
                oceanMaskMaterial.SetFloat(sp_FarPlaneOffset, farPlaneLerp);

                // Render fullscreen triangle with horizon mask pass.
                commandBuffer.DrawProcedural(Matrix4x4.identity, oceanMaskMaterial, shaderPass: k_ShaderPassOceanHorizonMask, MeshTopology.Triangles, 3, 1);
            }

            GeometryUtility.CalculateFrustumPlanes(camera, frustumPlanes);

            // Get all ocean chunks and render them using cmd buffer, but with mask shader.
            if (!debugDisableOceanMask)
            {
                // Spends approx 0.2-0.3ms here on 2018 Dell XPS 15.
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
                        commandBuffer.DrawRenderer(renderer, oceanMaskMaterial, submeshIndex: 0, shaderPass: k_ShaderPassOceanSurfaceMask);
                    }
                    chunk._oceanDataHasBeenBound = false;
                }
            }
        }
    }
}
