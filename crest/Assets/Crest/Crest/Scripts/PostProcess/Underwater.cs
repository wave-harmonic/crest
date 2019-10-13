// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace Crest
{
    /// <summary>
    /// This class holds settings for the Underwater effect.
    /// </summary>
    [Serializable]
    // Visible ocean tile list may not be populated until after transparents
    [PostProcess(typeof(UnderwaterRenderer), PostProcessEvent.AfterStack, "Crest/Underwater")]
    public sealed class Underwater : PostProcessEffectSettings
    {
    }

    internal sealed class UnderwaterRenderer : PostProcessEffectRenderer<Underwater>
    {
        static readonly int sp_OceanHeight = Shader.PropertyToID("_OceanHeight");
        static readonly int sp_HorizonOrientation = Shader.PropertyToID("_HorizonOrientation");
        static readonly int sp_MainTex = Shader.PropertyToID("_MainTex");
        static readonly int sp_MaskTex = Shader.PropertyToID("_MaskTex");
        static readonly int sp_MaskDepthTex = Shader.PropertyToID("_MaskDepthTex");
        static readonly int sp_InvViewProjection = Shader.PropertyToID("_InvViewProjection");
        static readonly int sp_InvViewProjectionRight = Shader.PropertyToID("_InvViewProjectionRight");
        static readonly int sp_InstanceData = Shader.PropertyToID("_InstanceData");
        static readonly int sp_AmbientLighting = Shader.PropertyToID("_AmbientLighting");

        private const float UNDERWATER_MASK_NO_MASK = 1.0f;

        private const string SHADER_UNDERWATER = "Crest/Underwater/Post Process New";
        private const string SHADER_OCEAN_MASK = "Crest/Underwater/Ocean Mask";

        Shader _shader;
        Shader _shaderMask;

        Material _materialMask;

        public override void Init()
        {
            base.Init();

            InitialisedCorrectly();
        }

        private bool InitialisedCorrectly()
        {
            _shader = Shader.Find(SHADER_UNDERWATER);
            //if (_underwaterPostProcessMaterial == null)
            //{
            //    Debug.LogError("UnderwaterPostProcess must have a post processing material assigned", this);
            //    return false;
            //}

            _shaderMask = Shader.Find(SHADER_OCEAN_MASK);
            _materialMask = _shaderMask ? new Material(_shaderMask) : null;
            if (_materialMask == null)
            {
                Debug.LogError($"Could not create a material with shader {SHADER_OCEAN_MASK}");
                return false;
            }

            if (OceanRenderer.Instance && !OceanRenderer.Instance.OceanMaterial.IsKeywordEnabled("_UNDERWATER_ON"))
            {
                Debug.LogError("Underwater must be enabled on the ocean material for UnderwaterPostProcess to work", OceanRenderer.Instance);
                return false;
            }

            //return CheckMaterial();
            return true;
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Underwater");

            var sheet = context.propertySheets.Get(_shader);

            if (OceanRenderer.Instance != null)
            {
                RenderPopulateMask(cmd, context);

                cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            }
            else
            {
                cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            }

            cmd.EndSample("Underwater");
        }

        // Populates a screen space mask which will inform the underwater postprocess. As a future optimisation we may
        // be able to avoid this pass completely if we can reuse the camera depth after transparents are rendered.
        private void RenderPopulateMask(CommandBuffer cmd, PostProcessRenderContext context)
        {
            cmd.BeginSample("Populate mask");

            int tw = Mathf.FloorToInt(context.screenWidth / 2f);
            int th = Mathf.FloorToInt(context.screenHeight / 2f);
            bool singlePassDoubleWide = (context.stereoActive && (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass) && (context.camera.stereoTargetEye == StereoTargetEyeMask.Both));
            int tw_stereo = singlePassDoubleWide ? tw * 2 : tw;

            var maskID = Shader.PropertyToID("_Mask");
            context.GetScreenSpaceTemporaryRT(cmd, maskID, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Default, FilterMode.Bilinear, tw_stereo, th);

            //if (_textureMask == null || _textureMask.width != source.width || _textureMask.height != source.height)
            //{
            //    _textureMask = new RenderTexture(source);
            //    _textureMask.name = "Ocean Mask";
            //    // @Memory: We could investigate making this an 8-bit texture instead to reduce GPU memory usage.
            //    // We could also potentially try a half res mask as the meniscus could mask res issues.
            //    _textureMask.format = RenderTextureFormat.RHalf;
            //    _textureMask.Create();

            //    _depthBuffer = new RenderTexture(source);
            //    _depthBuffer.name = "Ocean Mask Depth";
            //    _depthBuffer.format = RenderTextureFormat.Depth;
            //    _depthBuffer.Create();
            //}

            var sheet = context.propertySheets.Get(_shaderMask);

            cmd.SetRenderTarget(maskID);
            cmd.ClearRenderTarget(false, true, Color.white * UNDERWATER_MASK_NO_MASK);

            //cmd.BlitFullscreenTriangle(lastDown, mipDown, sheet, pass);

            // Get all ocean chunks and render them using cmd buffer, but with mask shader
            //_commandBuffer.SetRenderTarget(_textureMask.colorBuffer, _depthBuffer.depthBuffer);
            //_commandBuffer.ClearRenderTarget(true, true, Color.white * UNDERWATER_MASK_NO_MASK);
            cmd.SetViewProjectionMatrices(context.camera.worldToCameraMatrix, context.camera.projectionMatrix);

            // This gets tiles from previous frame, because this code executes on PreCull, before OnWillRenderObject.. eek
            if (OceanChunkRenderer._visibleTiles.TryGetValue(context.camera, out var tiles))
            {
                foreach (var rend in tiles)
                {
                    cmd.DrawRenderer(rend, _materialMask);
                }
                tiles.Clear();
            }

            //var camera = context.camera;
            //if (!camera.TryGetCullingParameters(IsStereoEnabled(camera), out var cullingParameters))
            //    return;
            //UniversalAdditionalCameraData additionalCameraData = null;
            //if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.VR)
            //    camera.gameObject.TryGetComponent(out additionalCameraData);
            //ScriptableRenderer renderer = additionalCameraData.scriptableRenderer;
            //renderer.SetupCullingParameters(ref cullingParameters, ref cameraData);
            //var cullResults = context.Cull(ref cullingParameters);

            // TODO
            //{
            //    // The same camera has to perform post-processing when performing
            //    // VR multipass rendering so don't clear chunks until we have
            //    // renderered the right eye.
            //    // This was the approach recommended by Unity's post-processing
            //    // lead Thomas Hourdel.
            //    bool saveChunksToRender =
            //        XRSettings.enabled &&
            //        XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass &&
            //        _mainCamera.stereoActiveEye != Camera.MonoOrStereoscopicEye.Right;

            //    if (!saveChunksToRender) _oceanChunksToRender.Clear();
            //}

            cmd.EndSample("Populate mask");
        }
    }
}
