// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    public class CreateSims : MonoBehaviour
    {
        [System.Serializable]
        public class SimLayer
        {
            public SimType _simType;
            public SimResolution _resolution;

            [Tooltip("Create a layer for sim inputs to render into and put the name here.")]
            public string _shapeRenderLayer;
            // could populate a dropdown list for this: https://answers.unity.com/questions/609385/type-for-layer-selection.html, https://answers.unity.com/questions/458987/dropdownlist-with-string-array-in-editor-inspector.html
        }

        public enum SimType
        {
            Wave,
            Foam,
        }

        public enum SimResolution
        {
            Res125mm,
            Res25cm,
            Res50cm,
            Res1m,
            Res2m,
            Res4m,
            Res8m,
            Res16m,
            Res32m,
        }

        public SimLayer[] _simulationLayers;

        void Start()
        {
            foreach (var layer in _simulationLayers)
            {
                int layerIndex = LayerMask.NameToLayer(layer._shapeRenderLayer);

                if (string.IsNullOrEmpty(layer._shapeRenderLayer) || layerIndex == -1)
                {
                    Debug.LogError("Creation of a " + layer._simType.ToString() + " simulation type was skipped because no render layer was provided.", this);
                    continue;
                }

                var simGO = new GameObject();
                simGO.transform.parent = transform;
                simGO.transform.localPosition = Vector3.zero;
                simGO.transform.localEulerAngles = 90f * Vector3.right;
                simGO.transform.localScale = Vector3.one;

                var sim = layer._simType == SimType.Wave ? simGO.AddComponent<SimWave>() : simGO.AddComponent<SimFoam>()
                    as SimBase;
                sim._resolution = GetRes(layer._resolution);
                sim._shapeRenderLayer = layerIndex;
                simGO.name = "Sim_" + sim.SimName + "_" + layer._resolution.ToString();

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
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;
                cam.depth = sim.Depth;
                cam.renderingPath = RenderingPath.Forward;
                cam.useOcclusionCulling = false;
                cam.allowHDR = true;
                cam.allowMSAA = false;
                cam.allowDynamicResolution = false;
            }
        }

        float GetRes(SimResolution res)
        {
            switch (res)
            {
                case SimResolution.Res125mm: return 0.125f;
                case SimResolution.Res25cm: return 0.25f;
                case SimResolution.Res50cm: return 0.5f;
                case SimResolution.Res1m: return 1f;
                case SimResolution.Res2m: return 2f;
                case SimResolution.Res4m: return 4f;
                case SimResolution.Res8m: return 8f;
                case SimResolution.Res16m: return 16f;
                case SimResolution.Res32m: return 32f;
            }

            Debug.LogError("Resolution " + res.ToString() + " needs to be added to PersistentFoamSims.cs.", this);
            return -1f;
        }
    }
}
