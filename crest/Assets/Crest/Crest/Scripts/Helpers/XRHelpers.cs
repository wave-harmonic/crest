// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// An adaptor layer for both VR/XR modules.

// ENABLE_VR is defined if platform support XR.
#if ENABLE_VR && (ENABLE_VR_MODULE || ENABLE_XR_MODULE)
    #define _XR_ENABLED
#endif

#if ENABLE_VR && ENABLE_VR_MODULE
    #define _VR_MODULE_ENABLED
#endif

#if ENABLE_VR && ENABLE_XR_MODULE
    #define _XR_MODULE_ENABLED
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if _XR_ENABLED
using UnityEngine.XR;
#endif

namespace Crest
{
    public static class XRHelpers
    {
        static List<XRDisplaySubsystem> _displayList = new List<XRDisplaySubsystem>();

        public static Matrix4x4 LeftEyeProjectionMatrix { get; private set; }
        public static Matrix4x4 RightEyeProjectionMatrix { get; private set; }
        public static Matrix4x4 LeftEyeViewMatrix { get; private set; }
        public static Matrix4x4 RightEyeViewMatrix { get; private set; }

        public static bool IsRunning
        {
            get
            {
                #if !_XR_ENABLED
                    return false;
                #endif

                return IsNewSDKRunning || IsOldSDKRunning;
            }
        }

        public static bool IsNewSDKRunning
        {
            get
            {
                // XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem is another way to access the XR
                // device. But that requires a dependency on com.unity.xr.management.
                #if _XR_MODULE_ENABLED
                    return _displayList.Count > 0 && _displayList[0].running;
                #else
                    return false;
                #endif
            }
        }

        public static bool IsOldSDKRunning
        {
            get
            {
                #if _VR_MODULE_ENABLED
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
                #if !_XR_ENABLED
                    return false;
                #endif

                #if _VR_MODULE_ENABLED
                    if (IsOldSDKRunning)
                    {
                        return XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass;
                    }
                #endif

                if (IsNewSDKRunning)
                {
                    return !(bool)Display?.singlePassRenderingDisabled;
                }

                return false;
            }
        }

        // Double-wide is legacy single pass (single pass without instancing). It is being phased out.
        public static bool IsDoubleWide
        {
            get
            {
                #if _VR_MODULE_ENABLED
                    return IsOldSDKRunning && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePass;
                #else
                    return false;
                #endif
            }
        }

        public static bool IsLegacyRenderer => GraphicsSettings.currentRenderPipeline == null;

        // This is according to HDRP
        public static int MaximumViews => IsSinglePass ? 2 : 1;

        // Unity only supports one display right now.
        public static XRDisplaySubsystem Display => IsNewSDKRunning ? _displayList[0] : null;

        // This works for both old and new XR API in legacy renderer. If the VR module is disabled, this will be a
        // compilation error, but an alternative is unknown at this point. Maybe once the new XR API improves.
        public static RenderTextureDescriptor EyeRenderTextureDescriptor => XRSettings.eyeTextureDesc;

        public static void SetViewProjectionMatrices(Camera camera, int viewIndex, int passIndex, CommandBuffer commandBuffer)
        {
            if (IsLegacyRenderer)
            {
                // Built-in is the same for old or new XR.
                var eye = (Camera.StereoscopicEye)(IsSinglePass ? viewIndex : passIndex);
                // It appears that the command buffer matrix changes were being overwritten so we just pass it as a
                // property instead. According to documentation, GetGPUProjectionMatrix is correct way to get the final
                // matrix similar to how Unity would do it. Only XR SPI uses this matrix.
                commandBuffer.SetGlobalMatrix("_ViewProjectionMatrix",
                    GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(eye), true) *
                    camera.GetStereoViewMatrix(eye));
                // Since XR SPI will use the above matrix, we can just set this one normally for everything else.
                commandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            }
            else if (IsNewSDKRunning)
            {
                if (Display.GetRenderPassCount() > 0)
                {
                    Display.GetRenderPass(passIndex, out var xrPass);
                    xrPass.GetRenderParameter(camera, viewIndex, out var xrEye);
                    commandBuffer.SetViewProjectionMatrices(xrEye.view, xrEye.projection);
                }
            }
            else
            {
                var eye = (Camera.StereoscopicEye) (IsSinglePass ? viewIndex : passIndex);
                commandBuffer.SetViewProjectionMatrices(camera.GetStereoViewMatrix(eye), camera.GetStereoProjectionMatrix(eye));
            }
        }

        public static void SetViewProjectionMatrices(Camera camera)
        {
            if (IsLegacyRenderer)
            {
                return;
            }

            if (IsNewSDKRunning)
            {
                if (IsSinglePass)
                {
                    camera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, LeftEyeViewMatrix);
                    camera.SetStereoViewMatrix(Camera.StereoscopicEye.Right, RightEyeViewMatrix);
                }
                else
                {
                    camera.projectionMatrix = LeftEyeProjectionMatrix;
                }
            }
        }

        public static void UpdatePassIndex(ref int passIndex)
        {
            if (IsSinglePass)
            {
                passIndex = 0;
            }
            else
            {
                passIndex += 1;
                passIndex %= 2;
            }
        }

        public static void Update(Camera camera)
        {
            #if _XR_MODULE_ENABLED
                SubsystemManager.GetInstances(_displayList);
            #endif

            if (!IsRunning)
            {
                return;
            }

            // Let's cache these values for SPI.
            if (IsSinglePass)
            {
                if (!IsLegacyRenderer && IsNewSDKRunning && Display.GetRenderPassCount() > 0)
                {
                    Display.GetRenderPass(0, out XRDisplaySubsystem.XRRenderPass xrPass);
                    xrPass.GetRenderParameter(camera, 0, out XRDisplaySubsystem.XRRenderParameter xrLeftEye);
                    xrPass.GetRenderParameter(camera, 1, out XRDisplaySubsystem.XRRenderParameter xrRightEye);
                    LeftEyeViewMatrix = xrLeftEye.view;
                    RightEyeViewMatrix = xrRightEye.view;
                    LeftEyeProjectionMatrix = xrLeftEye.projection;
                    RightEyeProjectionMatrix = xrRightEye.projection;
                }
                else
                {
                    LeftEyeViewMatrix = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                    RightEyeViewMatrix = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
                    LeftEyeProjectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                    RightEyeProjectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                }
            }
        }
    }
}
