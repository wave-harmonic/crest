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
        internal const int k_ShaderPassWaterBoundaryFrontFace = 0;
        internal const int k_ShaderPassWaterBoundaryBackFace = 1;

        public static readonly int sp_CrestOceanMaskTexture = Shader.PropertyToID("_CrestOceanMaskTexture");
        public static readonly int sp_CrestOceanMaskDepthTexture = Shader.PropertyToID("_CrestOceanMaskDepthTexture");
        public static readonly int sp_CrestWaterBoundaryGeometryFrontFaceTexture = Shader.PropertyToID("_CrestWaterBoundaryGeometryFrontFaceTexture");
        public static readonly int sp_CrestWaterBoundaryGeometryBackFaceTexture = Shader.PropertyToID("_CrestWaterBoundaryGeometryBackFaceTexture");
        public static readonly int sp_FarPlaneOffset = Shader.PropertyToID("_FarPlaneOffset");

        RenderTargetIdentifier _maskTarget = new RenderTargetIdentifier
        (
            sp_CrestOceanMaskTexture,
            mipLevel: 0,
            CubemapFace.Unknown,
            depthSlice: -1 // Bind all XR slices.
        );
        RenderTargetIdentifier _depthTarget = new RenderTargetIdentifier
        (
            sp_CrestOceanMaskDepthTexture,
            mipLevel: 0,
            CubemapFace.Unknown,
            depthSlice: -1 // Bind all XR slices.
        );

        internal Plane[] _cameraFrustumPlanes;
        CommandBuffer _oceanMaskCommandBuffer;
        PropertyWrapperMaterial _oceanMaskMaterial;

        CommandBuffer _boundaryCommandBuffer;
        Material _boundaryMaterial = null;
        RenderTargetIdentifier _boundaryBackFaceTarget = new RenderTargetIdentifier
        (
            sp_CrestWaterBoundaryGeometryBackFaceTexture,
            mipLevel: 0,
            CubemapFace.Unknown,
            depthSlice: -1 // Bind all XR slices.
        );
        RenderTargetIdentifier _boundaryFrontFaceTarget = new RenderTargetIdentifier
        (
            sp_CrestWaterBoundaryGeometryFrontFaceTexture,
            mipLevel: 0,
            CubemapFace.Unknown,
            depthSlice: -1 // Bind all XR slices.
        );

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

            if (_boundaryMaterial == null)
            {
                _boundaryMaterial = new Material(Shader.Find("Crest/Hidden/Water Boundary Geometry"));
            }

            if (_boundaryCommandBuffer == null)
            {
                _boundaryCommandBuffer = new CommandBuffer()
                {
                    name = "Water Boundary Geometry",
                };
            }
        }

        void OnDisableOceanMask()
        {
            DisableOceanMaskKeywords(_oceanMaskMaterial.material);
        }

        void DisableOceanMaskKeywords(Material material)
        {
            // Multiple keywords from same set can be enabled at the same time leading to undefined behaviour so we need
            // to disable all keywords from a set first.
            // https://docs.unity3d.com/Manual/shader-keywords-scripts.html
            material.DisableKeyword(k_KeywordBoundary2D);
            material.DisableKeyword(k_KeywordBoundaryHasBackFace);
            // Handling ocean keywords here.
            OceanRenderer.Instance.OceanMaterial.DisableKeyword(k_KeywordBoundary2D);
            OceanRenderer.Instance.OceanMaterial.DisableKeyword(k_KeywordBoundaryHasBackFace);
        }

        void SetUpMaskTextures(CommandBuffer buffer, RenderTextureDescriptor descriptor)
        {
            // This will disable MSAA for our textures as MSAA will break sampling later on. This looks safe to do as
            // Unity's CopyDepthPass does the same, but a possible better way or supporting MSAA is worth looking into.
            descriptor.msaaSamples = 1;
            // Without this sampling coordinates will be incorrect if used by camera. No harm always being "true".
            descriptor.useDynamicScale = true;

            // @Memory: We could investigate making this an 8-bit texture instead to reduce GPU memory usage.
            // @Memory: We could potentially try a half resolution mask as the mensicus could mask resolution issues.
            descriptor.colorFormat = RenderTextureFormat.RHalf;
            descriptor.depthBufferBits = 0;
            buffer.GetTemporaryRT(sp_CrestOceanMaskTexture, descriptor);

            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 24;
            buffer.GetTemporaryRT(sp_CrestOceanMaskDepthTexture, descriptor);
        }

        /// <summary>
        /// Releases temporary mask textures. Pass any available command buffer through.
        /// </summary>
        static void CleanUpMaskTextures(CommandBuffer buffer)
        {
            // According to the following source code, we can release a temporary RT using a different CB than the one
            // which allocated it. Unity uses CommandBufferPool.Get in OnCameraSetup (RTs allocated) and OnCameraCleanup
            // (RTs released) which means they could be different CBs. So pass any available CB through.
            // com.unity.render-pipelines.universal/Runtime/ScriptableRenderer.cs
            //
            // Manually releasing the textures right after they are no longer used is best (after underwater effect).
            // But they will be released for us if we fail to do so:
            // > Any temporary textures that were not explicitly released will be removed after camera is done
            // > rendering, or after Graphics.ExecuteCommandBuffer is done.
            // https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.ReleaseTemporaryRT.html
            buffer.ReleaseTemporaryRT(sp_CrestOceanMaskTexture);
            buffer.ReleaseTemporaryRT(sp_CrestOceanMaskDepthTexture);
        }

        void SetUpBoundaryTextures(CommandBuffer buffer, RenderTextureDescriptor descriptor)
        {
            descriptor.msaaSamples = 1;
            descriptor.useDynamicScale = true;
            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 24;

            buffer.GetTemporaryRT(sp_CrestWaterBoundaryGeometryFrontFaceTexture, descriptor);

            if (_mode == Mode.Geometry3D || _mode == Mode.GeometryVolume)
            {
                buffer.GetTemporaryRT(sp_CrestWaterBoundaryGeometryBackFaceTexture, descriptor);
            }
        }

        void CleanUpBoundaryTextures(Mode mode, CommandBuffer buffer)
        {
            buffer.ReleaseTemporaryRT(sp_CrestWaterBoundaryGeometryFrontFaceTexture);

            if (mode == Mode.Geometry3D || mode == Mode.GeometryVolume)
            {
                buffer.ReleaseTemporaryRT(sp_CrestWaterBoundaryGeometryBackFaceTexture);
            }
        }

        void OnPreRenderOceanMask()
        {
            RenderTextureDescriptor descriptor = XRHelpers.GetRenderTextureDescriptor(_camera);

            DisableOceanMaskKeywords(_oceanMaskMaterial.material);

            // Needed for convex hull as we need to clip the mask right up until the volume begins. It is used for non
            // convex hull, but could be skipped if we sample the clip surface in the mask.
            if (_mode != Mode.FullScreen)
            {
                // Keep separate from mask.
                _boundaryCommandBuffer.Clear();
                SetUpBoundaryTextures(_boundaryCommandBuffer, descriptor);
                _boundaryCommandBuffer.SetViewProjectionMatrices(_camera.worldToCameraMatrix, _camera.projectionMatrix);

                // Front faces.
                _boundaryCommandBuffer.SetRenderTarget(BuiltinRenderTextureType.None, _boundaryFrontFaceTarget);
                _boundaryCommandBuffer.ClearRenderTarget(true, false, Color.black);
                _boundaryCommandBuffer.SetGlobalTexture(sp_CrestWaterBoundaryGeometryFrontFaceTexture, _boundaryFrontFaceTarget);
                _boundaryCommandBuffer.DrawMesh
                (
                    _waterVolumeBoundaryGeometry.mesh,
                    _waterVolumeBoundaryGeometry.transform.localToWorldMatrix,
                    _boundaryMaterial,
                    submeshIndex: 0,
                    k_ShaderPassWaterBoundaryFrontFace
                );

                if (_mode == Mode.Geometry3D || _mode == Mode.GeometryVolume)
                {
                    // Back faces.
                    _boundaryCommandBuffer.SetRenderTarget(BuiltinRenderTextureType.None, _boundaryBackFaceTarget);
                    _boundaryCommandBuffer.ClearRenderTarget(true, false, Color.black);
                    _boundaryCommandBuffer.SetGlobalTexture(sp_CrestWaterBoundaryGeometryBackFaceTexture, _boundaryBackFaceTarget);
                    _boundaryCommandBuffer.DrawMesh
                    (
                        _waterVolumeBoundaryGeometry.mesh,
                        _waterVolumeBoundaryGeometry.transform.localToWorldMatrix,
                        _boundaryMaterial,
                        submeshIndex: 0,
                        k_ShaderPassWaterBoundaryBackFace
                    );
                }

                switch (_mode)
                {
                    case Mode.Geometry2D:
                        _oceanMaskMaterial.material.EnableKeyword(k_KeywordBoundary2D);
                        OceanRenderer.Instance.OceanMaterial.EnableKeyword(k_KeywordBoundary2D);
                        break;
                    case Mode.Geometry3D:
                        _oceanMaskMaterial.material.EnableKeyword(k_KeywordBoundaryHasBackFace);
                        OceanRenderer.Instance.OceanMaterial.EnableKeyword(k_KeywordBoundaryHasBackFace);
                        break;
                    case Mode.GeometryVolume:
                        _oceanMaskMaterial.material.EnableKeyword(k_KeywordBoundaryHasBackFace);
                        OceanRenderer.Instance.OceanMaterial.EnableKeyword(k_KeywordBoundaryHasBackFace);
                        break;
                }
            }

            _oceanMaskCommandBuffer.Clear();
            // Must call after clear or temporaries will be cleared.
            SetUpMaskTextures(_oceanMaskCommandBuffer, descriptor);
            _oceanMaskCommandBuffer.SetRenderTarget(_maskTarget, _depthTarget);
            _oceanMaskCommandBuffer.ClearRenderTarget(true, true, Color.white * 0.5f);
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
        }

        internal static void InitialiseClipSurfaceMaskTextures(RenderTextureDescriptor desc, ref RenderTexture depthBuffer, string name)
        {
            // Note: we pass-through pixel dimensions explicitly as we have to handle this slightly differently in HDRP
            if (depthBuffer == null || depthBuffer.width != desc.width || depthBuffer.height != desc.height)
            {
                // @Performance: We should consider either a temporary RT or use an RTHandle if appropriate
                // RenderTexture is a "native engine object". We have to release it to avoid memory leaks.
                if (depthBuffer != null)
                {
                    depthBuffer.Release();
                }

                depthBuffer = new RenderTexture(desc)
                {
                    depth = 24,
                    enableRandomWrite = false,
                    name = $"Clip Surface Mask {name}",
                    format = RenderTextureFormat.Depth,
                };

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
