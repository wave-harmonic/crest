using UnityEngine;

namespace Crest
{
    public class ShapeDynamicSims : MonoBehaviour
    {
        public const string DYNAMIC_SIM_LAYER_NAME = "DynamicSimData";

        public enum SimResolution
        {
            Res125mm = 0,
            Res25cm,
            Res50cm,
            Res1m,
            Res2m,
            Res4m,
            Res8m,
            Res16m,
            Res32m,
        }

        public SimResolution _resolution;

        void Start()
        {
            //foreach (var res in _resolution)
            {
                var res = _resolution;

                var simGO = new GameObject("DynamicSim_" + res.ToString());
                simGO.transform.parent = transform;
                simGO.transform.localPosition = Vector3.zero;
                simGO.transform.localEulerAngles = 90f * Vector3.right;
                simGO.transform.localScale = Vector3.one;

                var cart = simGO.AddComponent<CreateAssignRenderTexture>();
                cart._targetName = simGO.name;
                cart._width = cart._height = 192;
                cart._depthBits = 0;
                cart._format = RenderTextureFormat.ARGBFloat;
                cart._wrapMode = TextureWrapMode.Clamp;
                cart._antiAliasing = 1;
                cart._filterMode = FilterMode.Bilinear;
                cart._anisoLevel = 0;
                cart._useMipMap = false;
                cart._createPingPongTargets = true;

                var sim = simGO.AddComponent<ShapeDynamicSim>();
                sim._resolution = GetRes(res);

                var cam = simGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Nothing;
                cam.cullingMask = 1 << LayerMask.NameToLayer(DYNAMIC_SIM_LAYER_NAME);
                cam.orthographic = true;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;
                cam.depth = -20;
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

            Debug.LogError("Resolution " + res.ToString() + " needs to be added to ShapeDynamicSims.cs.", this);
            return -1f;
        }
    }
}
