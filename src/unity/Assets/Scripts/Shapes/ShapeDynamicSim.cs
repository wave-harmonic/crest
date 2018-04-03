// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A dynamic shape simulation that moves around with a displacement LOD.
    /// </summary>
    public class ShapeDynamicSim : MonoBehaviour
    {
        [HideInInspector]
        public float _resolution = 0.5f;

        Camera _cam;
        PingPongRts _pprts;

        //GameObject _copySimResultsToDisplacements;
        Material _copySimMaterial;

        GameObject _renderSim;
        Material _renderSimMaterial;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            _pprts = GetComponent<PingPongRts>();

            CreateRenderSimQuad();
            //CreateCopySimQuad();
            _copySimMaterial = new Material(Shader.Find("Ocean/Shape/Sim/Add To Disps"));
        }

        private void CreateRenderSimQuad()
        {
            // utility quad which will be rasterized by the shape camera
            _renderSim = CreateRasterQuad("RenderSim");
            _renderSim.layer = LayerMask.NameToLayer(ShapeDynamicSims.DYNAMIC_SIM_LAYER_NAME);
            _renderSim.transform.parent = transform;
            _renderSim.transform.localScale = Vector3.one;
            _renderSim.transform.localPosition = Vector3.forward * 25f;
            _renderSim.transform.localRotation = Quaternion.identity;
            _renderSim.GetComponent<Renderer>().material = _renderSimMaterial = new Material(Shader.Find("Ocean/Shape/Sim/2D Wave Equation"));
        }

        //private void CreateCopySimQuad()
        //{
        //    // utility quad which will be rasterized by the shape camera
        //    _copySimResultsToDisplacements = CreateRasterQuad("CopySimResultsToDisplacements");
        //    _copySimResultsToDisplacements.transform.parent = transform;
        //    _copySimResultsToDisplacements.transform.localScale = Vector3.one;
        //    _copySimResultsToDisplacements.transform.localPosition = Vector3.forward * 25f;
        //    _copySimResultsToDisplacements.transform.localRotation = Quaternion.identity;
        //    _copySimResultsToDisplacements.GetComponent<Renderer>().material = _copySimMaterial = new Material(Shader.Find("Ocean/Shape/Sim/Add To Disps"));
        //}

        GameObject CreateRasterQuad(string name)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Quad);
            result.name = name;
            Destroy(result.GetComponent<Collider>());

            var rend = result.GetComponent<Renderer>();
            rend.lightProbeUsage = LightProbeUsage.Off;
            rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            rend.allowOcclusionWhenDynamic = false;

            return result;
        }

        CommandBuffer _copySimResultsCmdBuf;
        
        void LateUpdate()
        {
            int lodIndex = OceanRenderer.Instance.GetLodIndex(_resolution);
            if (lodIndex == -1)
            {
                if (_copySimResultsCmdBuf != null)
                {
                    _copySimResultsCmdBuf.Clear();
                }
                return;
            }

            if (_copySimResultsCmdBuf == null)
            {
                _copySimResultsCmdBuf = new CommandBuffer();
                _copySimResultsCmdBuf.name = "CopySimResults";
                OceanRenderer.Instance.Builder._shapeCameras[lodIndex].AddCommandBuffer(CameraEvent.AfterForwardAlpha, _copySimResultsCmdBuf);
            }

            var lodCam = OceanRenderer.Instance.Builder._shapeCameras[lodIndex];
            var wdc = lodCam.GetComponent<WaveDataCam>();

            transform.position = lodCam.transform.position;

            _cam.orthographicSize = lodCam.orthographicSize;
            transform.localScale = (Vector3.right + Vector3.up) * _cam.orthographicSize * 2f + Vector3.forward;

            _cam.projectionMatrix = lodCam.projectionMatrix;

            Vector3 posDelta = wdc._renderData._posSnapped - wdc._renderData._posSnappedLast;
            _renderSimMaterial.SetVector("_CameraPositionDelta", posDelta);

            _renderSimMaterial.SetTexture("_WavePPTSource", _pprts._sourceThisFrame);

            _copySimMaterial.mainTexture = _pprts._targetThisFrame;

            _copySimResultsCmdBuf.Clear();
            _copySimResultsCmdBuf.Blit(_pprts._targetThisFrame, RenderTexture.active, _copySimMaterial);
        }
    }
}
