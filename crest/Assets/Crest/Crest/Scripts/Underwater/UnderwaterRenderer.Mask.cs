// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

namespace Crest
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    public partial class UnderwaterRenderer
    {
        const string k_ShaderPathOceanMask = "Hidden/Crest/Underwater/Ocean Mask";
        internal const int k_ShaderPassOceanSurfaceMask = 0;
        internal const int k_ShaderPassOceanHorizonMask = 1;
        internal const string k_ComputeShaderFillMaskArtefacts = "CrestFillMaskArtefacts";
        internal const string k_ComputeShaderKernelFillMaskArtefacts = "FillMaskArtefacts";

        public static readonly int sp_CrestOceanMaskTexture = Shader.PropertyToID("_CrestOceanMaskTexture");
        public static readonly int sp_CrestOceanMaskDepthTexture = Shader.PropertyToID("_CrestOceanMaskDepthTexture");
        public static readonly int sp_FarPlaneOffset = Shader.PropertyToID("_FarPlaneOffset");

        internal RenderTargetIdentifier _maskTarget = new RenderTargetIdentifier
        (
            sp_CrestOceanMaskTexture,
            mipLevel: 0,
            CubemapFace.Unknown,
            depthSlice: -1 // Bind all XR slices.
        );
        internal RenderTargetIdentifier _depthTarget = new RenderTargetIdentifier
        (
            sp_CrestOceanMaskDepthTexture,
            mipLevel: 0,
            CubemapFace.Unknown,
            depthSlice: -1 // Bind all XR slices.
        );

        internal Plane[] _cameraFrustumPlanes;
        CommandBuffer _oceanMaskCommandBuffer;
        PropertyWrapperMaterial _oceanMaskMaterial;

        RenderTexture _maskRT;
        RenderTexture _depthRT;

        ComputeShader _fixMaskComputeShader;
        int _fixMaskKernel;
        uint _fixMaskThreadGroupSizeX;
        uint _fixMaskThreadGroupSizeY;

        void SetupOceanMask()
        {
            if (_oceanMaskMaterial?.material == null)
            {
                _oceanMaskMaterial = new PropertyWrapperMaterial(k_ShaderPathOceanMask);
            }

            if (_oceanMaskCommandBuffer == null)
            {
                _oceanMaskCommandBuffer = new CommandBuffer()
                {
                    name = "Ocean Mask",
                };
            }

            SetUpFixMaskArtefactsShader();
        }

        internal void SetUpFixMaskArtefactsShader()
        {
            if (_fixMaskComputeShader != null)
            {
                return;
            }

            _fixMaskComputeShader = ComputeShaderHelpers.LoadShader(k_ComputeShaderFillMaskArtefacts);
            _fixMaskKernel = _fixMaskComputeShader.FindKernel(k_ComputeShaderKernelFillMaskArtefacts);
            _fixMaskComputeShader.GetKernelThreadGroupSizes
            (
                _fixMaskKernel,
                out _fixMaskThreadGroupSizeX,
                out _fixMaskThreadGroupSizeY,
                out _
            );
        }

        internal static void SetUpMaskTextures(CommandBuffer buffer, RenderTextureDescriptor descriptor)
        {
            // Bail if we do not need to (re)create the textures.
            if (Instance._maskRT != null && descriptor.width == Instance._maskRT.width && descriptor.height == Instance._maskRT.height)
            {
                return;
            }

            // Release textures before replacing them.
            if (Instance._maskRT != null)
            {
                Instance._maskRT.Release();
                Instance._depthRT.Release();
            }

            // This will disable MSAA for our textures as MSAA will break sampling later on. This looks safe to do as
            // Unity's CopyDepthPass does the same, but a possible better way or supporting MSAA is worth looking into.
            descriptor.msaaSamples = 1;
            // Without this sampling coordinates will be incorrect if used by camera. No harm always being "true".
            descriptor.useDynamicScale = true;

            // @Memory: We could investigate making this an 8-bit texture instead to reduce GPU memory usage.
            // @Memory: We could potentially try a half resolution mask as the mensicus could mask resolution issues.
            // Intel iGPU for Metal and DirectX both had issues with R16. 2021.11.18
            descriptor.colorFormat = Helpers.IsIntelGPU() ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf;
            descriptor.depthBufferBits = 0;
            descriptor.enableRandomWrite = true;

            Instance._maskRT = new RenderTexture(descriptor);
            Instance._maskTarget = new RenderTargetIdentifier
            (
                Instance._maskRT,
                mipLevel: 0,
                CubemapFace.Unknown,
                depthSlice: -1 // Bind all XR slices.
            );

            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 24;
            descriptor.enableRandomWrite = false;

            Instance._depthRT = new RenderTexture(descriptor);
            Instance._depthTarget = new RenderTargetIdentifier
            (
                Instance._depthRT,
                mipLevel: 0,
                CubemapFace.Unknown,
                depthSlice: -1 // Bind all XR slices.
            );
        }

        /// <summary>
        /// Releases temporary mask textures. Pass any available command buffer through.
        /// </summary>
        internal static void CleanUpMaskTextures()
        {
            if (Instance == null)
            {
                return;
            }

            if (Instance._maskRT != null)
            {
                Instance._maskRT.Release();
                Instance._maskRT = null;
            }

            if (Instance._depthRT != null)
            {
                Instance._depthRT.Release();
                Instance._depthRT = null;
            }
        }

        void OnPreRenderOceanMask()
        {
            RenderTextureDescriptor descriptor = XRHelpers.GetRenderTextureDescriptor(_camera);

            _oceanMaskCommandBuffer.Clear();
            // Must call after clear or temporaries will be cleared.
            SetUpMaskTextures(_oceanMaskCommandBuffer, descriptor);
            _oceanMaskCommandBuffer.SetRenderTarget(_maskTarget, _depthTarget);
            // Horizon pass makes clearing color redundant. If the horizon enabled ZWrite then we could skip clearing.
            _oceanMaskCommandBuffer.ClearRenderTarget(true, false, Color.black);
            _oceanMaskCommandBuffer.SetGlobalTexture(sp_CrestOceanMaskTexture, _maskTarget);
            _oceanMaskCommandBuffer.SetGlobalTexture(sp_CrestOceanMaskDepthTexture, _depthTarget);

            SetInverseViewProjectionMatrix(_oceanMaskMaterial.material);

            PopulateOceanMask(
                _oceanMaskCommandBuffer,
                _camera,
                OceanRenderer.Instance.Tiles,
                _cameraFrustumPlanes,
                _oceanMaskMaterial.material,
                _farPlaneMultiplier,
                _debug._disableOceanMask
            );

            FixMaskArtefacts(_oceanMaskCommandBuffer, descriptor, _maskTarget);
        }

        internal void FixMaskArtefacts(CommandBuffer buffer, RenderTextureDescriptor descriptor, RenderTargetIdentifier target)
        {
            if (_debug._disableArtifactCorrection)
            {
                return;
            }

            buffer.SetComputeTextureParam(_fixMaskComputeShader, _fixMaskKernel, sp_CrestOceanMaskTexture, target);
            _fixMaskComputeShader.SetKeyword("STEREO_INSTANCING_ON", XRHelpers.IsSinglePass);

            buffer.DispatchCompute
            (
                _fixMaskComputeShader,
                _fixMaskKernel,
                descriptor.width / (int)_fixMaskThreadGroupSizeX,
                descriptor.height / (int)_fixMaskThreadGroupSizeY,
                XRHelpers.IsSinglePass ? 2 : 1
            );
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
