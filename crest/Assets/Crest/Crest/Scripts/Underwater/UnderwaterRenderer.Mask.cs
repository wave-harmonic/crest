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
        const string k_ShaderPathWaterVolumeGeometry = "Hidden/Crest/Water Volume Geometry";
        internal const int k_ShaderPassOceanSurfaceMask = 0;
        internal const int k_ShaderPassOceanHorizonMask = 1;
        // This must match the Stencil Ref value for front and back face pass in:
        // Shaders/Underwater/Resources/UnderwaterEffect.shader
        // Shaders/Underwater/Resources/WaterVolumeGeometry.shader
        internal const int k_StencilValueVolume = 5;

        internal const string k_ComputeShaderFillMaskArtefacts = "CrestFillMaskArtefacts";
        internal const string k_ComputeShaderKernelFillMaskArtefacts = "FillMaskArtefacts";

        public static readonly int sp_CrestOceanMaskTexture = Shader.PropertyToID("_CrestOceanMaskTexture");
        public static readonly int sp_CrestOceanMaskDepthTexture = Shader.PropertyToID("_CrestOceanMaskDepthTexture");
        public static readonly int sp_CrestWaterVolumeFrontFaceTexture = Shader.PropertyToID("_CrestWaterVolumeFrontFaceTexture");
        public static readonly int sp_CrestWaterVolumeBackFaceTexture = Shader.PropertyToID("_CrestWaterVolumeBackFaceTexture");
        public static readonly int sp_FarPlaneOffset = Shader.PropertyToID("_FarPlaneOffset");

        internal enum VolumePass
        {
            FrontFace,
            BackFace,
        }

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

        internal Material _volumeMaterial = null;
        internal RenderTargetIdentifier _volumeBackFaceTarget = new RenderTargetIdentifier
        (
            sp_CrestWaterVolumeBackFaceTexture,
            mipLevel: 0,
            CubemapFace.Unknown,
            depthSlice: -1 // Bind all XR slices.
        );
        internal RenderTargetIdentifier _volumeFrontFaceTarget = new RenderTargetIdentifier
        (
            sp_CrestWaterVolumeFrontFaceTexture,
            mipLevel: 0,
            CubemapFace.Unknown,
            depthSlice: -1 // Bind all XR slices.
        );

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

            SetUpFixMaskArtefactsShader();
        }

        void OnDisableOceanMask()
        {
            DisableOceanMaskKeywords();
            CleanUpMaskTextures();
            CleanUpVolumeTextures();
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
            // Bail if we do not need to (re)create the textures.
            if (_maskRT != null && descriptor.width == _maskRT.width && descriptor.height == _maskRT.height && descriptor.volumeDepth == _maskRT.volumeDepth && descriptor.useDynamicScale == _maskRT.useDynamicScale)
            {
                return;
            }

            // Release textures before replacing them.
            if (_maskRT != null)
            {
                _maskRT.Release();
                _depthRT.Release();
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

            _maskRT = new RenderTexture(descriptor);
            _maskRT.name = "_CrestOceanMaskTexture";
            _maskTarget = new RenderTargetIdentifier
            (
                _maskRT,
                mipLevel: 0,
                CubemapFace.Unknown,
                depthSlice: -1 // Bind all XR slices.
            );

            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 24;
            descriptor.enableRandomWrite = false;

            _depthRT = new RenderTexture(descriptor);
            _depthRT.name = "_CrestOceanMaskDepthTexture";
            _depthTarget = new RenderTargetIdentifier
            (
                _depthRT,
                mipLevel: 0,
                CubemapFace.Unknown,
                depthSlice: -1 // Bind all XR slices.
            );
        }

        internal void SetUpVolumeTextures(RenderTextureDescriptor descriptor)
        {
            descriptor.msaaSamples = 1;
            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 24;

            Helpers.CreateRenderTargetTexture(ref _volumeFrontFaceRT, ref _volumeFrontFaceTarget, descriptor);
            _volumeFrontFaceRT.name = "_CrestVolumeFrontFaceTexture";

            if (_mode == Mode.Volume || _mode == Mode.VolumeFlyThrough)
            {
                Helpers.CreateRenderTargetTexture(ref _volumeBackFaceRT, ref _volumeBackFaceTarget, descriptor);
                _volumeBackFaceRT.name = "_CrestVolumeBackFaceTexture";
            }
        }

        internal void CleanUpVolumeTextures()
        {
            Helpers.DestroyRenderTargetTexture(ref _volumeFrontFaceRT);
            Helpers.DestroyRenderTargetTexture(ref _volumeBackFaceRT);
        }

        internal void CleanUpMaskTextures()
        {
            Helpers.DestroyRenderTargetTexture(ref _maskRT);
            Helpers.DestroyRenderTargetTexture(ref _depthRT);
        }

        internal void SetUpVolume(Material maskMaterial)
        {
            Helpers.SetGlobalKeyword(k_KeywordVolume2D, _mode == Mode.Portal);
            Helpers.SetGlobalKeyword(k_KeywordVolumeHasBackFace, _mode == Mode.Volume || _mode == Mode.VolumeFlyThrough);
            maskMaterial.SetKeyword(k_KeywordVolume, _mode != Mode.FullScreen);
            maskMaterial.SetInt("_StencilRef", UseStencilBufferOnMask ? k_StencilValueVolume : 0);
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
            if (_mode != Mode.FullScreen)
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
            buffer.SetGlobalTexture(sp_CrestWaterVolumeFrontFaceTexture, frontTarget);
            if (_mode == Mode.Portal) buffer.SetInvertCulling(_invertCulling);
            buffer.DrawMesh
            (
                _volumeGeometry.mesh,
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
                buffer.SetGlobalTexture(sp_CrestWaterVolumeBackFaceTexture, backTarget);
                buffer.DrawMesh
                (
                    _volumeGeometry.mesh,
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
            buffer.SetGlobalTexture(sp_CrestOceanMaskTexture, maskTarget);
            buffer.SetGlobalTexture(sp_CrestOceanMaskDepthTexture, depthTarget);
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
