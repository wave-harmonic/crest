// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

using static Crest.UnderwaterPostProcessUtils;

namespace Crest
{
    /// <summary>
    /// Underwater Post Process. If a camera needs to go underwater it needs to have this script attached. This adds fullscreen passes and should
    /// only be used if necessary. This effect disables itself when camera is not close to the water volume.
    ///
    /// For convenience, all shader material settings are copied from the main ocean shader. This includes underwater
    /// specific features such as enabling the meniscus.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnderwaterPostProcessMaskRenderer : MonoBehaviour
    {
        BoolParameter _disableOceanMask;

        internal RenderTexture _textureMask;
        internal RenderTexture _depthBuffer;
        private CommandBuffer _maskCommandBuffer;

        private Plane[] _cameraFrustumPlanes;

        private Material _oceanMaskMaterial = null;
        private Camera _mainCamera;
        internal readonly SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

        internal void Initialise(Material oceanMaskMaterial, BoolParameter disableOceanMask)
        {
            _mainCamera = GetComponent<Camera>();
            _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
            _maskCommandBuffer = new CommandBuffer();
            _maskCommandBuffer.name = "Ocean Mask Command Buffer";
            _mainCamera.AddCommandBuffer(
                CameraEvent.BeforeForwardAlpha,
                _maskCommandBuffer
            );
            _oceanMaskMaterial = oceanMaskMaterial;
            _disableOceanMask = disableOceanMask;
        }

        void OnPreRender()
        {
            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraFrustumPlanes);
            _maskCommandBuffer.Clear();

            {
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor(_mainCamera.pixelWidth, _mainCamera.pixelHeight);
                InitialiseMaskTextures(descriptor, ref _textureMask, ref _depthBuffer);
            }

            PopulateOceanMask(
                _maskCommandBuffer, _mainCamera, OceanRenderer.Instance.Tiles, _cameraFrustumPlanes,
                _textureMask, _depthBuffer,
                _oceanMaskMaterial,
                _disableOceanMask.value
            );

        }
    }
}
