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
        internal const int k_ShaderPassWaterBoundaryOuter = 0;
        internal const int k_ShaderPassWaterBoundaryInner = 1;

        public static readonly int sp_CrestOceanMaskTexture = Shader.PropertyToID("_CrestOceanMaskTexture");
        public static readonly int sp_CrestOceanMaskDepthTexture = Shader.PropertyToID("_CrestOceanMaskDepthTexture");
        public static readonly int sp_CrestWaterBoundaryGeometryOuterTexture = Shader.PropertyToID("_CrestWaterBoundaryGeometryOuterTexture");
        public static readonly int sp_CrestWaterBoundaryGeometryInnerTexture = Shader.PropertyToID("_CrestWaterBoundaryGeometryInnerTexture");
        public static readonly int sp_FarPlaneOffset = Shader.PropertyToID("_FarPlaneOffset");

        internal Plane[] _cameraFrustumPlanes;
        CommandBuffer _oceanMaskCommandBuffer;
        PropertyWrapperMaterial _oceanMaskMaterial;
        RenderTexture _maskTexture;
        RenderTexture _depthTexture;

        CommandBuffer _boundaryCommandBuffer;
        Material _boundaryMaterial = null;
        RenderTexture _boundaryInnerTexture;
        RenderTexture _boundaryOuterTexture;

        void SetupOceanMask()
        {
            if (_oceanMaskMaterial?.material == null)
            {
                _oceanMaskMaterial = new PropertyWrapperMaterial(SHADER_OCEAN_MASK);
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

        void OnPreRenderOceanMask()
        {
            RenderTextureDescriptor descriptor = XRHelpers.GetRenderTextureDescriptor(_camera);
            descriptor.useDynamicScale = _camera.allowDynamicResolution;

            InitialiseMaskTextures(descriptor, ref _maskTexture, ref _depthTexture);

            // Needed for convex hull as we need to clip the mask right up until the volume begins. It is used for non
            // convex hull, but could be skipped if we sample the clip surface in the mask.
            if (_waterVolumeBoundaryGeometry != null)
            {
                // Keep separate from mask.
                _boundaryCommandBuffer.Clear();
                _boundaryCommandBuffer.SetViewProjectionMatrices(_camera.worldToCameraMatrix, _camera.projectionMatrix);

                // Outer boundary.
                InitialiseClipSurfaceMaskTextures(descriptor, ref _boundaryInnerTexture, "Outer");
                _boundaryCommandBuffer.SetGlobalTexture(sp_CrestWaterBoundaryGeometryOuterTexture, _boundaryInnerTexture.depthBuffer);
                _boundaryCommandBuffer.SetRenderTarget(_boundaryInnerTexture.depthBuffer);
                _boundaryCommandBuffer.ClearRenderTarget(true, false, Color.black);
                _boundaryCommandBuffer.DrawMesh
                (
                    _waterVolumeBoundaryGeometry.mesh,
                    _waterVolumeBoundaryGeometry.transform.localToWorldMatrix,
                    _boundaryMaterial,
                    submeshIndex: 0,
                    k_ShaderPassWaterBoundaryOuter
                );

                // Inner boundary.
                InitialiseClipSurfaceMaskTextures(descriptor, ref _boundaryOuterTexture, "Inner");
                _boundaryCommandBuffer.SetGlobalTexture(sp_CrestWaterBoundaryGeometryInnerTexture, _boundaryOuterTexture.depthBuffer);
                _boundaryCommandBuffer.SetRenderTarget(_boundaryOuterTexture.depthBuffer);
                _boundaryCommandBuffer.ClearRenderTarget(true, false, Color.black);
                _boundaryCommandBuffer.DrawMesh
                (
                    _waterVolumeBoundaryGeometry.mesh,
                    _waterVolumeBoundaryGeometry.transform.localToWorldMatrix,
                    _boundaryMaterial,
                    submeshIndex: 0,
                    k_ShaderPassWaterBoundaryInner
                );

                _oceanMaskMaterial.material.EnableKeyword("_UNDERWATER_GEOMETRY_EFFECT");
                OceanRenderer.Instance.OceanMaterial.EnableKeyword(_isConvexHull ? "_UNDERWATER_GEOMETRY_EFFECT_CONVEX_HULL" : "_UNDERWATER_GEOMETRY_EFFECT_PLANE");
            }
            else
            {
                _oceanMaskMaterial.material.DisableKeyword("_UNDERWATER_GEOMETRY_EFFECT");
                OceanRenderer.Instance.OceanMaterial.EnableKeyword("_UNDERWATER_GEOMETRY_EFFECT_NONE");
            }

            _oceanMaskCommandBuffer.Clear();
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
        }

        internal static void InitialiseMaskTextures(RenderTextureDescriptor desc, ref RenderTexture textureMask, ref RenderTexture depthBuffer)
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
                // Have to set these explicitly as the built-in transforms aren't in world-space for the blit function.
                if (XRHelpers.IsSinglePass)
                {
                    // NOTE: Not needed for HDRP.
                    oceanMaskMaterial.SetMatrix(sp_InvViewProjection, (GL.GetGPUProjectionMatrix(XRHelpers.LeftEyeProjectionMatrix, false) * XRHelpers.LeftEyeViewMatrix).inverse);
                    oceanMaskMaterial.SetMatrix(sp_InvViewProjectionRight, (GL.GetGPUProjectionMatrix(XRHelpers.RightEyeProjectionMatrix, false) * XRHelpers.RightEyeViewMatrix).inverse);
                }
                else
                {
                    // NOTE: Not needed for HDRP.
                    var inverseViewProjectionMatrix = (GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * camera.worldToCameraMatrix).inverse;
                    oceanMaskMaterial.SetMatrix(sp_InvViewProjection, inverseViewProjectionMatrix);
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
