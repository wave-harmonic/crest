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

        public static readonly int sp_CrestOceanMaskTexture = Shader.PropertyToID("_CrestOceanMaskTexture");
        public static readonly int sp_CrestOceanMaskDepthTexture = Shader.PropertyToID("_CrestOceanMaskDepthTexture");
        public static readonly int sp_CrestWaterBoundaryGeometryTexture = Shader.PropertyToID("_CrestWaterBoundaryGeometryTexture");

        // This matches const on shader side.
        internal const float UNDERWATER_MASK_NO_MASK = 1.0f;

        internal Plane[] _cameraFrustumPlanes;
        CommandBuffer _oceanMaskCommandBuffer;
        PropertyWrapperMaterial _oceanMaskMaterial;
        RenderTexture _maskTexture;
        RenderTexture _depthTexture;

        RenderTexture _waterBoundaryGeometryTexture;
        CommandBuffer _waterBoundaryGeometryCommandBuffer;
        Material _waterBoundaryGeometryMaterial = null;

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

            if (_waterBoundaryGeometryMaterial == null)
            {
                _waterBoundaryGeometryMaterial = new Material(Shader.Find("Crest/Hidden/Water Boundary Geometry"));
            }

            if (_waterBoundaryGeometryCommandBuffer == null)
            {
                _waterBoundaryGeometryCommandBuffer = new CommandBuffer()
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
                InitialiseClipSurfaceMaskTextures(descriptor, ref _waterBoundaryGeometryTexture);

                // Keep separate from mask.
                _waterBoundaryGeometryCommandBuffer.Clear();
                _waterBoundaryGeometryCommandBuffer.SetRenderTarget(_waterBoundaryGeometryTexture.depthBuffer);
                _waterBoundaryGeometryCommandBuffer.ClearRenderTarget(true, false, Color.black);
                _waterBoundaryGeometryCommandBuffer.SetViewProjectionMatrices(_camera.worldToCameraMatrix, _camera.projectionMatrix);
                _waterBoundaryGeometryCommandBuffer.SetGlobalTexture(sp_CrestWaterBoundaryGeometryTexture, _waterBoundaryGeometryTexture.depthBuffer);
                _waterBoundaryGeometryCommandBuffer.DrawMesh(_waterVolumeBoundaryGeometry.mesh, _waterVolumeBoundaryGeometry.transform.localToWorldMatrix, _waterBoundaryGeometryMaterial, 0, 0);

                _oceanMaskMaterial.material.EnableKeyword("_UNDERWATER_GEOMETRY_EFFECT");
                OceanRenderer.Instance.OceanMaterial.EnableKeyword("_UNDERWATER_GEOMETRY_EFFECT");
            }
            else
            {
                _oceanMaskMaterial.material.DisableKeyword("_UNDERWATER_GEOMETRY_EFFECT");
                OceanRenderer.Instance.OceanMaterial.DisableKeyword("_UNDERWATER_GEOMETRY_EFFECT");
            }

            _oceanMaskCommandBuffer.Clear();
            // Passing -1 to depth slice binds all slices. Important for XR SPI to work in both eyes.
            _oceanMaskCommandBuffer.SetRenderTarget(_maskTexture.colorBuffer, _depthTexture.depthBuffer, mipLevel: 0, CubemapFace.Unknown, depthSlice: -1);
            _oceanMaskCommandBuffer.ClearRenderTarget(true, true, Color.white * UNDERWATER_MASK_NO_MASK);
            _oceanMaskCommandBuffer.SetGlobalTexture(sp_CrestOceanMaskTexture, _maskTexture.colorBuffer);
            _oceanMaskCommandBuffer.SetGlobalTexture(sp_CrestOceanMaskDepthTexture, _depthTexture.depthBuffer);

            PopulateOceanMask(
                _oceanMaskCommandBuffer,
                _camera,
                OceanRenderer.Instance.Tiles,
                _cameraFrustumPlanes,
                _oceanMaskMaterial.material,
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

        internal static void InitialiseClipSurfaceMaskTextures(RenderTextureDescriptor desc, ref RenderTexture depthBuffer)
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
                    name = "Clip Surface Mask",
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
            bool debugDisableOceanMask
        )
        {
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
                        commandBuffer.DrawRenderer(renderer, oceanMaskMaterial);
                    }
                    chunk._oceanDataHasBeenBound = false;
                }
            }
        }
    }
}
