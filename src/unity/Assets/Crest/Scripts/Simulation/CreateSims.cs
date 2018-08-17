// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Script that constructs the simulations.
    /// </summary>
    public class CreateSims : MonoBehaviour
    {
        public enum SimType
        {
            Wave,
            Foam,
        }

        public static GameObject CreateSimCam(int lodIdx, int lodCount, Transform parent, SimType simType, string name, SimSettingsBase settings, int layerIndex)
        {
            var simGO = new GameObject();
            simGO.transform.parent = parent;
            simGO.transform.localPosition = Vector3.zero;
            simGO.transform.localEulerAngles = 90f * Vector3.right;
            simGO.transform.localScale = Vector3.one;

            var sim = simType == SimType.Wave ? simGO.AddComponent<SimWave>() : simGO.AddComponent<SimFoam>()
                as SimBase;
            sim.InitLODData(lodIdx, lodCount);
            simGO.name = name;

            sim.UseSettings(settings);

            var cart = simGO.AddComponent<CreateAssignRenderTexture>();
            cart._width = cart._height = (int)(4f * OceanRenderer.Instance._baseVertDensity);
            cart._depthBits = 0;
            cart._format = sim.TextureFormat;
            cart._wrapMode = TextureWrapMode.Clamp;
            cart._antiAliasing = 1;
            cart._filterMode = FilterMode.Bilinear;
            cart._anisoLevel = 0;
            cart._useMipMap = false;
            cart._createPingPongTargets = true;
            cart._targetName = simGO.name;

            var cam = simGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Nothing;
            cam.cullingMask = 1 << layerIndex;
            cam.orthographic = true;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 500f;
            cam.depth = sim.Depth;
            cam.renderingPath = RenderingPath.Forward;
            cam.useOcclusionCulling = false;
            cam.allowHDR = true;
            cam.allowMSAA = false;
            cam.allowDynamicResolution = false;

            return simGO;
        }
    }
}
