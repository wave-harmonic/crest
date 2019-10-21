// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// TODO
// create and render to depth buffer

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
        static readonly int sp_maskID = Shader.PropertyToID("_Mask");

        private const float UNDERWATER_MASK_NO_MASK = 1.0f;

        private const string FULL_SCREEN_EFFECT = "_FULL_SCREEN_EFFECT";
        private const string DEBUG_VIEW_OCEAN_MASK = "_DEBUG_VIEW_OCEAN_MASK";

        private const string SHADER_UNDERWATER = "Crest/Underwater/Post Process New";
        private const string SHADER_OCEAN_MASK = "Crest/Underwater/Ocean Mask";

        Shader _shader;
        PropertyWrapperMPB _propertyWrapper = new PropertyWrapperMPB();

        Shader _shaderMask;
        Material _materialMask;

        //private RenderTexture _depthBuffer;

        Color[] _ambientLighting = new Color[1];
        SphericalHarmonicsL2 _sphericalHarmonicsL2;
        Vector3[] _shDirections = new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f) };

        public override void Init()
        {
            base.Init();

            InitialisedCorrectly();
        }

        private bool InitialisedCorrectly()
        {
            _shader = Shader.Find(SHADER_UNDERWATER);

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

                RenderUpdateMaterial(context.source, context.camera, sheet);

                // blit with sheet associated with the shader
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
            bool singlePassDoubleWide = false; // (context.stereoActive && (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass) && (context.camera.stereoTargetEye == StereoTargetEyeMask.Both));
            int tw_stereo = singlePassDoubleWide ? tw * 2 : tw;

            context.GetScreenSpaceTemporaryRT(cmd, sp_maskID, 24, RenderTextureFormat.RHalf, RenderTextureReadWrite.Default, FilterMode.Bilinear, tw_stereo, th);
            //context.GetScreenSpaceTemporaryRT(cmd, sp_MaskDepthTex, 24, RenderTextureFormat.Depth);

            //if (_depthBuffer == null || _depthBuffer.width != tw_stereo || _depthBuffer.height != th)
            //{
            //    _depthBuffer = new RenderTexture(tw_stereo, th, 1);
            //    _depthBuffer.name = "Ocean Mask Depth";
            //    _depthBuffer.format = RenderTextureFormat.Depth;
            //    _depthBuffer.Create();
            //}

            var sheet = context.propertySheets.Get(_shaderMask);

            cmd.SetRenderTarget(sp_maskID);
            cmd.ClearRenderTarget(true, true, Color.white * UNDERWATER_MASK_NO_MASK);

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

        void RenderUpdateMaterial(RenderTargetIdentifier source, Camera camera, PropertySheet sheet)
        {
            //if (_firstRender || _copyOceanMaterialParamsEachFrame)
            {
                // Measured this at approx 0.05ms on dell laptop
                //_material.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
            }

            // Enabling/disabling keywords each frame don't seem to have large measurable overhead
            //if (_viewOceanMask)
            //{
            //    _underwaterPostProcessMaterial.EnableKeyword(DEBUG_VIEW_OCEAN_MASK);
            //}
            //else
            {
                sheet.DisableKeyword(DEBUG_VIEW_OCEAN_MASK);
            }

            sheet.properties.SetFloat(LodDataMgr.sp_LD_SliceIndex, 0);
            sheet.properties.SetVector(sp_InstanceData, new Vector4(OceanRenderer.Instance.ViewerAltitudeLevelAlpha, 0f, 0f, OceanRenderer.Instance.CurrentLodCount));

            _propertyWrapper.materialPropertyBlock = sheet.properties;

            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(_propertyWrapper);
            if (OceanRenderer.Instance._lodDataSeaDepths)
            {
                OceanRenderer.Instance._lodDataSeaDepths.BindResultData(_propertyWrapper);
            }
            else
            {
                LodDataMgrSeaFloorDepth.BindNull(_propertyWrapper);
            }

            if (OceanRenderer.Instance._lodDataShadow)
            {
                OceanRenderer.Instance._lodDataShadow.BindResultData(_propertyWrapper);
            }
            else
            {
                LodDataMgrShadow.BindNull(_propertyWrapper);
            }

            {
                float oceanHeight = OceanRenderer.Instance.transform.position.y;
                float maxOceanVerticalDisplacement = OceanRenderer.Instance.MaxVertDisplacement * 0.5f;
                float cameraHeight = camera.transform.position.y;
                bool forceFullShader = (cameraHeight + maxOceanVerticalDisplacement) <= oceanHeight;
                sheet.properties.SetFloat(sp_OceanHeight, oceanHeight);
                if (forceFullShader)
                {
                    sheet.EnableKeyword(FULL_SCREEN_EFFECT);
                }
                else
                {
                    sheet.DisableKeyword(FULL_SCREEN_EFFECT);
                }
            }

            // happens automatically due to sp_maskID ?
            //_material.SetTexture(sp_MaskTex, _textureMask);
            // TODO
            //_material.SetTexture(sp_MaskDepthTex, _depthBuffer);

            // Have to set these explicitly as the built-in transforms aren't in world-space for the blit function
            //if (!XRSettings.enabled || XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
            {

                var viewProjectionMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
                var invViewProjectionMatrix = viewProjectionMatrix.inverse;
                sheet.properties.SetMatrix(sp_InvViewProjection, invViewProjectionMatrix);
            }
            //else
            //{
            //    var viewProjectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) * camera.worldToCameraMatrix;
            //    _material.SetMatrix(sp_InvViewProjection, viewProjectionMatrix.inverse);
            //    var viewProjectionMatrixRightEye = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right) * camera.worldToCameraMatrix;
            //    _material.SetMatrix(sp_InvViewProjectionRight, viewProjectionMatrixRightEye.inverse);
            //}

            // Not sure why we need to do this - blit should set it...?
            //_material.SetTexture(sp_MainTex, source);

            // Compute ambient lighting SH
            {
                // We could pass in a renderer which would prime this lookup. However it doesnt make sense to use an existing render
                // at different position, as this would then thrash it and negate the priming functionality. We could create a dummy invis GO
                // with a dummy Renderer which might be enoguh, but this is hacky enough that we'll wait for it to become a problem
                // rather than add a pre-emptive hack.

                UnityEngine.Profiling.Profiler.BeginSample("Underwater sample spherical harmonics");

                LightProbes.GetInterpolatedProbe(OceanRenderer.Instance.Viewpoint.position, null, out _sphericalHarmonicsL2);
                _sphericalHarmonicsL2.Evaluate(_shDirections, _ambientLighting);
                sheet.properties.SetVector(sp_AmbientLighting, _ambientLighting[0]);

                UnityEngine.Profiling.Profiler.EndSample();
            }
        }
    }
}
