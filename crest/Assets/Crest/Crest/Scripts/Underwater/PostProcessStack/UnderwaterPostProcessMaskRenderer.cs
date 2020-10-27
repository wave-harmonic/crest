// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

using static Crest.UnderwaterPostProcessUtils;

namespace Crest
{
    [RequireComponent(typeof(Camera))]
    public class UnderwaterPostProcessMaskRenderer : MonoBehaviour
    {
        private Camera _mainCamera;
        private Plane[] _cameraFrustumPlanes;
        private CommandBuffer _maskCommandBuffer;
        private Material _oceanMaskMaterial = null;
        internal RenderTexture _textureMask;
        internal RenderTexture _depthBuffer;
        BoolParameter _disableOceanMask;
        internal readonly SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

        internal void Initialise(Material oceanMaskMaterial, BoolParameter disableOceanMask)
        {
            _mainCamera = GetComponent<Camera>();
            _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
            _maskCommandBuffer = new CommandBuffer();
            _maskCommandBuffer.name = "Ocean Mask Command Buffer";
            _mainCamera.AddCommandBuffer(
                // CameraEvent.BeforeForwardAlpha,
                XRHelpers.IsSinglePass && !XRHelpers.IsDoubleWide
                        ? CameraEvent.AfterForwardAlpha
                        : CameraEvent.BeforeForwardAlpha,
                _maskCommandBuffer
            );
            _oceanMaskMaterial = oceanMaskMaterial;
            _disableOceanMask = disableOceanMask;
        }

        void OnPreRender()
        {
            XRHelpers.Update(_mainCamera);

            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraFrustumPlanes);
            _maskCommandBuffer.Clear();

            {
                RenderTextureDescriptor descriptor = XRHelpers.IsRunning
                    ? XRHelpers.EyeRenderTextureDescriptor
                    : new RenderTextureDescriptor(_mainCamera.pixelWidth, _mainCamera.pixelHeight);
                InitialiseMaskTextures(descriptor, ref _textureMask, ref _depthBuffer);
            }

            for (var depthSlice = 0; depthSlice < _textureMask.volumeDepth; depthSlice++)
            {
                PopulateOceanMask(
                    _maskCommandBuffer, _mainCamera, OceanRenderer.Instance.Tiles, _cameraFrustumPlanes,
                    _textureMask, _depthBuffer,
                    _oceanMaskMaterial, depthSlice, 0,
                    _disableOceanMask
                );
            }
        }
    }
}
