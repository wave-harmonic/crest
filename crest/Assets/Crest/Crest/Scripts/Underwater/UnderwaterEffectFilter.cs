// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.Rendering;
#else
using UnityEngine.Rendering;
#endif

namespace Crest
{

    public class UnderwaterEffectFilter : MonoBehaviour
    {
        private static Camera _currentCamera;
        private Renderer _rend;

        public static readonly int sp_Mask = Shader.PropertyToID("_Mask");

        public UnderwaterMaskValues MaskType = UnderwaterMaskValues.UnderwaterDisableFront;
        public Renderer Renderer => _rend;

        void Start()
        {
            _rend = GetComponent<Renderer>();
            if (_rend == null)
            {
                Debug.LogError($"UnderwaterEffectFilter can only be added to Game Objects with a Renderer attached!", this);
                enabled = false;
            }
        }

        [RuntimeInitializeOnLoadMethod]
        static void RunOnStart()
        {
#if UNITY_2018
            RenderPipeline.beginCameraRendering -= BeginCameraRendering;
            RenderPipeline.beginCameraRendering += BeginCameraRendering;
#else
        RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
#endif
        }

#if UNITY_2018
        private static void BeginCameraRendering(Camera camera)
#else
        private static void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
#endif
        {
            _currentCamera = camera;
        }


        bool _hasCopied = false;

        // Called when visible to a camera
        void OnWillRenderObject()
        {
            // check if built-in pipeline being used
            if (Camera.current != null)
            {
                _currentCamera = Camera.current;
            }
            // TODO(TRC):Now Remove this hack!
            if(MaskType == UnderwaterMaskValues.UnderwaterDisableFront && !_hasCopied)
            {
                Material material = GetComponent<MeshRenderer>().material;
                material.CopyPropertiesFromMaterial(OceanRenderer.Instance.OceanMaterial);
                material.SetTexture("_SurfaceNormal", OceanRenderer.Instance.OceanMaterial.GetTexture("_Normals"));
            }

            var underwater = _currentCamera.GetComponent<UnderwaterPostProcess>();
            if (underwater != null && underwater.enabled)
            {
                underwater.RegisterGeneralUnderwaterMaskToRender(this);
            }
        }
    }
}
