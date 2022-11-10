﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Adaptor layer for XR module. Could be replaced with the following one day:
// com.unity.render-pipelines.core/Runtime/Common/XRGraphics.cs

// Currently, only the horizon line uses it.

// ENABLE_VR is defined if platform support XR.
// ENABLE_VR_MODULE is defined if VR module is installed.
// VR module depends on XR module so we only need to check the VR module.
#if ENABLE_VR && ENABLE_VR_MODULE
#define _XR_ENABLED
#endif

namespace Crest
{
    using System.Collections.Generic;
    using UnityEngine;

#if _XR_ENABLED
    using UnityEngine.XR;
#endif

    public static class XRHelpers
    {
        // NOTE: This is the same value as Unity, but in the future it could be higher.
        const int k_MaximumViews = 2;

#if _XR_ENABLED
        readonly static List<XRDisplaySubsystem> _displayList = new List<XRDisplaySubsystem>();

        // Unity only supports one display right now.
        public static XRDisplaySubsystem Display => IsRunning ? _displayList[0] : null;
#endif

        public static Matrix4x4 LeftEyeProjectionMatrix { get; private set; }
        public static Matrix4x4 RightEyeProjectionMatrix { get; private set; }
        public static Matrix4x4 LeftEyeViewMatrix { get; private set; }
        public static Matrix4x4 RightEyeViewMatrix { get; private set; }

        public static bool IsRunning
        {
            get
            {
#if _XR_ENABLED
                return XRSettings.enabled;
#else
                return false;
#endif
            }
        }

        public static bool IsSinglePass
        {
            get
            {
#if _XR_ENABLED
                // TODO: What about multiview?
                return IsRunning && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced;
#else
                return false;
#endif
            }
        }

        static Texture2DArray s_WhiteTexture = null;
        public static Texture2DArray WhiteTexture
        {
            get
            {
                if (s_WhiteTexture == null)
                {
                    s_WhiteTexture = TextureArrayHelpers.CreateTexture2DArray(Texture2D.whiteTexture, k_MaximumViews);
                    s_WhiteTexture.name = "Crest White Texture XR";
                }
                return s_WhiteTexture;
            }
        }

        public static RenderTextureDescriptor GetRenderTextureDescriptor(Camera camera)
        {
#if _XR_ENABLED
            if (camera.stereoEnabled)
            {
                return XRSettings.eyeTextureDesc;
            }
            else
#endif
            {
                // As recommended by Unity, in 2021.2 using SystemInfo.GetGraphicsFormat with DefaultFormat.LDR is
                // necessary or gamma color space texture is returned:
                // https://docs.unity3d.com/ScriptReference/Experimental.Rendering.DefaultFormat.html
                return new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight, SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR), 0);
            }
        }

        public static void SetViewProjectionMatrices(Camera camera, int passIndex)
        {
#if _XR_ENABLED
            if (!XRSettings.enabled || IsSinglePass)
            {
                return;
            }
            // Not going to use cached values here just in case.
            Display.GetRenderPass(passIndex, out var xrPass);
            xrPass.GetRenderParameter(camera, renderParameterIndex: 0, out var xrEye);
            camera.projectionMatrix = xrEye.projection;
#endif
        }

        public static void UpdatePassIndex(ref int passIndex)
        {
            if (IsRunning)
            {
#if _XR_ENABLED
                if (XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
                {
                    // Alternate between left and right eye.
                    passIndex += 1;
                    passIndex %= 2;
                }
                else
                {
                    passIndex = 0;
                }
#endif
            }
            else
            {
                passIndex = -1;
            }
        }

        public static void Update(Camera camera)
        {
#if _XR_ENABLED
            SubsystemManager.GetInstances(_displayList);
#endif

            if (!camera.stereoEnabled || !IsSinglePass)
            {
                return;
            }

#if _XR_ENABLED
            // XR SPI only has one pass by definition.
            Display.GetRenderPass(renderPassIndex: 0, out var xrPass);
            // Grab left and right eye.
            xrPass.GetRenderParameter(camera, renderParameterIndex: 0, out var xrLeftEye);
            xrPass.GetRenderParameter(camera, renderParameterIndex: 1, out var xrRightEye);
            // Store all the matrices.
            LeftEyeViewMatrix = xrLeftEye.view;
            RightEyeViewMatrix = xrRightEye.view;
            LeftEyeProjectionMatrix = xrLeftEye.projection;
            RightEyeProjectionMatrix = xrRightEye.projection;
#endif
        }
    }
}
