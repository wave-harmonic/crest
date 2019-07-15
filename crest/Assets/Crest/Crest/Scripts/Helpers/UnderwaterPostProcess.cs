using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{

    [RequireComponent(typeof(Camera))]
    public class UnderwaterPostProcess : MonoBehaviour
    {
        public Material _underWaterPostProcMat;
        public Material _oceanMaskMat;
        private Camera _mainCamera;
        private RenderTexture _textureMask;
        private RenderTexture _depthBuffer;
        private CommandBuffer _commandBuffer;
        private PropertyWrapperMaterial _underWaterPostProcMatWrapper;
        static int sp_HorizonHeight = Shader.PropertyToID("_HorizonHeight");
        static int sp_HorizonOrientation = Shader.PropertyToID("_HorizonOrientation");
        static int sp_MaskTex = Shader.PropertyToID("_MaskTex");
        static int sp_MaskDepthTex = Shader.PropertyToID("_MaskDepthTex");

        void Start()
        {
            _mainCamera = GetComponent<Camera>();
            if (_mainCamera == null)
            {
                Debug.LogError("Underwater effects expect to be attached to a camera", this);
                enabled = false;

                return;
            }

            _commandBuffer = new CommandBuffer();
            _commandBuffer.name = "Underwater Post Process";

            if(_underWaterPostProcMat != null)
            {
                _underWaterPostProcMatWrapper = new PropertyWrapperMaterial(_underWaterPostProcMat);
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture target)
        {
            if (_oceanMaskMat == null)
            {
                _oceanMaskMat = OceanRenderer.Instance.OceanMaterial;
            }

            if (_textureMask == null)
            {
                _textureMask = new RenderTexture(source);
                _textureMask.name = "Ocean Mask";
                // TODO(UPP): See if we can make this an 8bit texture somehow
                _textureMask.format = RenderTextureFormat.RFloat;
                _textureMask.Create();

                _depthBuffer = new RenderTexture(source);
                _depthBuffer.name = "Ocean Mask Depth";
                _depthBuffer.format = RenderTextureFormat.Depth;
                _depthBuffer.Create();
            }

            // Get all ocean chunks and render them using cmd buffer, but with
            _commandBuffer.SetRenderTarget(_textureMask.colorBuffer, _depthBuffer.depthBuffer);
            _commandBuffer.ClearRenderTarget(true, true, Color.black);
            OceanChunkRenderer[] chunkComponents = Object.FindObjectsOfType<OceanChunkRenderer>();
            _oceanMaskMat.EnableKeyword("_UNDERWATER_MASK_ON");
            _commandBuffer.SetViewProjectionMatrices(_mainCamera.worldToCameraMatrix, _mainCamera.projectionMatrix);
            foreach (OceanChunkRenderer chunkComponent in chunkComponents)
            {
                Renderer renderer = chunkComponent.GetComponent<Renderer>();
                _commandBuffer.DrawRenderer(renderer, _oceanMaskMat);
            }

            // TODO(UPP): handle Roll
            float horizonRoll = 0.0f;
            float horizonHeight = 0.0f;
            {
                // Calculate the horizon height in screen space
                // TODO(UPP): Get this to actually work
                float halfFov = _mainCamera.fieldOfView * 0.5f;
                Vector3 cameraForward = _mainCamera.transform.forward;
                float cameraRotation = Mathf.Atan2(-1.0f * cameraForward.y, (new Vector2(cameraForward.x, cameraForward.z)).magnitude);
                float halfProp = Mathf.Tan(cameraRotation * 0.5f) / Mathf.Tan(halfFov * Mathf.Deg2Rad);
                horizonHeight = halfProp + 0.5f;
            }

            _underWaterPostProcMat.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
            _underWaterPostProcMat.SetFloat(OceanRenderer.sp_LD_SliceIndex, 0);

            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(_underWaterPostProcMatWrapper);
            if (OceanRenderer.Instance._lodDataSeaDepths)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(_underWaterPostProcMatWrapper);
            }
            else
            {
                LodDataMgrSeaFloorDepth.BindNull(_underWaterPostProcMatWrapper);
            }
            if (OceanRenderer.Instance._lodDataShadow)
            {
                OceanRenderer.Instance._lodDataShadow.BindResultData(_underWaterPostProcMatWrapper);
            }
            else
            {
                LodDataMgrShadow.BindNull(_underWaterPostProcMatWrapper);
            }

            _underWaterPostProcMat.SetFloat(sp_HorizonHeight, horizonHeight);
            _underWaterPostProcMat.SetFloat(sp_HorizonOrientation, horizonRoll);
            _underWaterPostProcMat.SetTexture(sp_MaskTex, _textureMask);
            _underWaterPostProcMat.SetTexture(sp_MaskDepthTex, _depthBuffer);

            _commandBuffer.Blit(source, target, _underWaterPostProcMat);

            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _oceanMaskMat.DisableKeyword("_UNDERWATER_MASK_ON");
            _commandBuffer.Clear();

            // Need this to prevent Unity from giving the following warning.
            // - OnRenderImage() possibly didn't write anything to the destination texture!
            Graphics.SetRenderTarget(target);
        }
    }

}
