// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Crest.Internal;

namespace Crest
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    public partial class UnderwaterRenderer
    {
        const string k_ShaderPathOceanMask = "Hidden/Crest/Underwater/Ocean Mask";
        const string k_ShaderPathWaterVolumeGeometry = "Hidden/Crest/Water Volume Geometry";
        internal const int k_ShaderPassOceanSurfaceMask = 0;
        internal const int k_ShaderPassOceanHorizonMask = 1;
        // This must match the Stencil Ref value for front and back face pass in:
        // Shaders/Underwater/Resources/UnderwaterEffect.shader
        // Shaders/Underwater/Resources/WaterVolumeGeometry.shader
        internal const int k_StencilValueVolume = 5;

        // NOTE: Must match CREST_MASK_BELOW_SURFACE in OceanConstants.hlsl.
        const float k_MaskBelowSurface = -1f;
        // NOTE: Must match CREST_MASK_BELOW_SURFACE_CULLED in OceanConstants.hlsl.
        const float k_MaskBelowSurfaceCull = -2f;

        internal const string k_ComputeShaderFillMaskArtefacts = "CrestFillMaskArtefacts";
        internal const string k_ComputeShaderKernelFillMaskArtefacts = "FillMaskArtefacts";

        public static partial class ShaderIDs
        {
            // Local
            public static readonly int s_InvViewProjection = Shader.PropertyToID("_InvViewProjection");
            public static readonly int s_InvViewProjectionRight = Shader.PropertyToID("_InvViewProjectionRight");
            public static readonly int s_FarPlaneOffset = Shader.PropertyToID("_FarPlaneOffset");
            public static readonly int s_MaskBelowSurface = Shader.PropertyToID("_MaskBelowSurface");

            // Global
            public static readonly int s_CrestOceanMaskTexture = Shader.PropertyToID("_CrestOceanMaskTexture");
            public static readonly int s_CrestOceanMaskDepthTexture = Shader.PropertyToID("_CrestOceanMaskDepthTexture");
            public static readonly int s_CrestWaterVolumeFrontFaceTexture = Shader.PropertyToID("_CrestWaterVolumeFrontFaceTexture");
            public static readonly int s_CrestWaterVolumeBackFaceTexture = Shader.PropertyToID("_CrestWaterVolumeBackFaceTexture");

            public static readonly int s_StencilRef = Shader.PropertyToID("_StencilRef");
        }

        internal enum VolumePass
        {
            FrontFace,
            BackFace,
        }

        internal RenderTargetIdentifier _maskTarget;
        internal RenderTargetIdentifier _depthTarget;

        internal Plane[] _cameraFrustumPlanes;
        CommandBuffer _oceanMaskCommandBuffer;
        PropertyWrapperMaterial _oceanMaskMaterial;

        internal Material _volumeMaterial = null;
        internal RenderTargetIdentifier _volumeBackFaceTarget;
        internal RenderTargetIdentifier _volumeFrontFaceTarget;

        RenderTexture _maskRT;
        RenderTexture _depthRT;
        RenderTexture _volumeFrontFaceRT;
        RenderTexture _volumeBackFaceRT;

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
        }

        internal void OnEnableMask()
        {
            if (_volumeMaterial == null)
            {
                _volumeMaterial = new Material(Shader.Find(k_ShaderPathWaterVolumeGeometry));
            }

            // Create a reference to handle the RT. The RT properties will be replaced with a descriptor before the
            // native object is created, and since it is lazy it is near zero cost.
            Helpers.CreateRenderTargetTextureReference(ref _maskRT, ref _maskTarget);
            _maskRT.name = "_CrestOceanMaskTexture";
            Helpers.CreateRenderTargetTextureReference(ref _depthRT, ref _depthTarget);
            _depthRT.name = "_CrestOceanMaskDepthTexture";
            Helpers.CreateRenderTargetTextureReference(ref _volumeFrontFaceRT, ref _volumeFrontFaceTarget);
            _volumeFrontFaceRT.name = "_CrestVolumeFrontFaceTexture";
            Helpers.CreateRenderTargetTextureReference(ref _volumeBackFaceRT, ref _volumeBackFaceTarget);
            _volumeBackFaceRT.name = "_CrestVolumeBackFaceTexture";

            SetUpFixMaskArtefactsShader();
        }

        internal void OnDisableMask()
        {
            DisableOceanMaskKeywords();
            if (_maskRT != null) _maskRT.Release();
            if (_depthRT != null) _depthRT.Release();
            if (_volumeFrontFaceRT != null) _volumeFrontFaceRT.Release();
            if (_volumeBackFaceRT != null) _volumeBackFaceRT.Release();
        }

        internal static void DisableOceanMaskKeywords()
        {
            // Multiple keywords from same set can be enabled at the same time leading to undefined behaviour so we need
            // to disable all keywords from a set first.
            // https://docs.unity3d.com/Manual/shader-keywords-scripts.html
            // Global keywords are easier to manage. Otherwise we would have to track the material etc.
            Shader.DisableKeyword(k_KeywordVolume2D);
            Shader.DisableKeyword(k_KeywordVolumeHasBackFace);
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

        internal void SetUpMaskTextures(RenderTextureDescriptor descriptor)
        {
            if (!Helpers.RenderTargetTextureNeedsUpdating(_maskRT, descriptor))
            {
                return;
            }

            // This will disable MSAA for our textures as MSAA will break sampling later on. This looks safe to do as
            // Unity's CopyDepthPass does the same, but a possible better way or supporting MSAA is worth looking into.
            descriptor.msaaSamples = 1;

            // @Memory: We could investigate making this an 8-bit texture instead to reduce GPU memory usage.
            // @Memory: We could potentially try a half resolution mask as the mensicus could mask resolution issues.
            // Intel iGPU for Metal and DirectX both had issues with R16. 2021.11.18
            descriptor.colorFormat = Helpers.IsIntelGPU() ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf;
            descriptor.depthBufferBits = 0;
            descriptor.enableRandomWrite = true;

            _maskRT.Release();
            _maskRT.descriptor = descriptor;

            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 24;
            descriptor.enableRandomWrite = false;

            _depthRT.Release();
            _depthRT.descriptor = descriptor;
        }

        internal void SetUpVolumeTextures(RenderTextureDescriptor descriptor)
        {
            if (!Helpers.RenderTargetTextureNeedsUpdating(_volumeFrontFaceRT, descriptor))
            {
                return;
            }

            descriptor.msaaSamples = 1;
            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 24;

            _volumeFrontFaceRT.Release();
            _volumeFrontFaceRT.descriptor = descriptor;

            if (_mode == Mode.Volume || _mode == Mode.VolumeFlyThrough)
            {
                _volumeBackFaceRT.Release();
                _volumeBackFaceRT.descriptor = descriptor;
            }
        }

        internal void SetUpVolume(Material maskMaterial)
        {
            Helpers.SetGlobalKeyword(k_KeywordVolume2D, _mode == Mode.Portal);
            Helpers.SetGlobalKeyword(k_KeywordVolumeHasBackFace, _mode == Mode.Volume || _mode == Mode.VolumeFlyThrough);
            maskMaterial.SetKeyword(k_KeywordVolume, _mode != Mode.FullScreen);
            maskMaterial.SetInt(ShaderIDs.s_StencilRef, UseStencilBufferOnMask ? k_StencilValueVolume : 0);
        }

        void OnPreRenderOceanMask()
        {
            _oceanMaskCommandBuffer.Clear();

            RenderTextureDescriptor descriptor = XRHelpers.GetRenderTextureDescriptor(_camera);

            descriptor.useDynamicScale = _camera.allowDynamicResolution;

            // Keywords and other things.
            SetUpVolume(_oceanMaskMaterial.material);
            SetUpMaskTextures(descriptor);

            // Populate water volume before mask so we can use the stencil.
            if (_mode != Mode.FullScreen && _volumeGeometry != null)
            {
                SetUpVolumeTextures(descriptor);
                PopulateVolume(_oceanMaskCommandBuffer, _volumeFrontFaceTarget, _volumeBackFaceTarget);
                // Copy only the stencil by copying everything and clearing depth.
                _oceanMaskCommandBuffer.CopyTexture(_mode == Mode.Portal ? _volumeFrontFaceTarget : _volumeBackFaceTarget, _depthTarget);
                Helpers.Blit(_oceanMaskCommandBuffer, _depthTarget, Helpers.UtilityMaterial, (int)Helpers.UtilityPass.ClearDepth);
            }

            SetUpMask(_oceanMaskCommandBuffer, _maskTarget, _depthTarget);
            SetInverseViewProjectionMatrix(_oceanMaskMaterial.material);
            PopulateOceanMask(
                _oceanMaskCommandBuffer,
                _camera,
                OceanRenderer.Instance.Tiles,
                _cameraFrustumPlanes,
                _oceanMaskMaterial.material,
                _farPlaneMultiplier,
                _enableShaderAPI,
                _debug._disableOceanMask
            );

            FixMaskArtefacts(_oceanMaskCommandBuffer, descriptor, _maskTarget);
        }

        internal void PopulateVolume(CommandBuffer buffer, RenderTargetIdentifier frontTarget, RenderTargetIdentifier backTarget, MaterialPropertyBlock properties = null, Vector2Int targetSize = default)
        {
            // Front faces.
            buffer.SetRenderTarget(frontTarget);
            // Support RTHandle scaling.
            if (targetSize != Vector2Int.zero) buffer.SetViewport(new Rect(0f, 0f, targetSize.x, targetSize.y));
            buffer.ClearRenderTarget(true, false, Color.black);
            buffer.SetGlobalTexture(ShaderIDs.s_CrestWaterVolumeFrontFaceTexture, frontTarget);
            if (_mode == Mode.Portal) buffer.SetInvertCulling(_invertCulling);
            buffer.DrawMesh
            (
                _volumeGeometry.sharedMesh,
                _volumeGeometry.transform.localToWorldMatrix,
                _volumeMaterial,
                submeshIndex: 0,
                (int)VolumePass.FrontFace,
                properties
            );
            buffer.SetInvertCulling(false);

            if (_mode == Mode.Volume || _mode == Mode.VolumeFlyThrough)
            {
                // Back faces.
                buffer.SetRenderTarget(backTarget);
                // Support RTHandle scaling.
                if (targetSize != Vector2Int.zero) buffer.SetViewport(new Rect(0f, 0f, targetSize.x, targetSize.y));
                buffer.ClearRenderTarget(true, false, Color.black);
                buffer.SetGlobalTexture(ShaderIDs.s_CrestWaterVolumeBackFaceTexture, backTarget);
                buffer.DrawMesh
                (
                    _volumeGeometry.sharedMesh,
                    _volumeGeometry.transform.localToWorldMatrix,
                    _volumeMaterial,
                    submeshIndex: 0,
                    (int)VolumePass.BackFace,
                    properties
                );
            }
        }

        internal void SetUpMask(CommandBuffer buffer, RenderTargetIdentifier maskTarget, RenderTargetIdentifier depthTarget)
        {
            buffer.SetRenderTarget(maskTarget, depthTarget);
            // When using the stencil we are already clearing depth and do not want to clear the stencil too. Clear
            // color only when using the stencil as the horizon effectively clears it when not using it.
            buffer.ClearRenderTarget(!UseStencilBufferOnMask, UseStencilBufferOnMask, Color.black);
            buffer.SetGlobalTexture(ShaderIDs.s_CrestOceanMaskTexture, maskTarget);
            buffer.SetGlobalTexture(ShaderIDs.s_CrestOceanMaskDepthTexture, depthTarget);
        }

        internal void FixMaskArtefacts(CommandBuffer buffer, RenderTextureDescriptor descriptor, RenderTargetIdentifier target)
        {
            if (_debug._disableArtifactCorrection)
            {
                return;
            }

            buffer.SetComputeTextureParam(_fixMaskComputeShader, _fixMaskKernel, ShaderIDs.s_CrestOceanMaskTexture, target);
            // XR SPI will have a volume depth of two. If using RTHandles, then set manually as will be two for all cameras.
            _fixMaskComputeShader.SetKeyword("STEREO_INSTANCING_ON", descriptor.volumeDepth > 1);

            buffer.DispatchCompute
            (
                _fixMaskComputeShader,
                _fixMaskKernel,
                // Viewport sizes are not perfect so round up to cover.
                Mathf.CeilToInt((float)descriptor.width / _fixMaskThreadGroupSizeX),
                Mathf.CeilToInt((float)descriptor.height / _fixMaskThreadGroupSizeY),
                descriptor.volumeDepth
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
            bool enableShaderAPI,
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
                oceanMaskMaterial.SetFloat(ShaderIDs.s_FarPlaneOffset, farPlaneLerp);

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
                    // Can happen in edit mode.
                    if (renderer == null) continue;
                    Bounds bounds = renderer.bounds;
                    if (GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                    {
                        if ((!chunk._oceanDataHasBeenBound) && chunk.enabled)
                        {
                            chunk.BindOceanData(camera);
                        }

                        // Handle culled tiles for when underwater is rendered before the transparent pass.
                        chunk._mpb.SetFloat(ShaderIDs.s_MaskBelowSurface, !enableShaderAPI || renderer.enabled ? k_MaskBelowSurface : k_MaskBelowSurfaceCull);
                        renderer.SetPropertyBlock(chunk._mpb.materialPropertyBlock);

                        commandBuffer.DrawRenderer(renderer, oceanMaskMaterial, submeshIndex: 0, shaderPass: k_ShaderPassOceanSurfaceMask);
                    }
                    chunk._oceanDataHasBeenBound = false;
                }
            }
        }
    }
}
