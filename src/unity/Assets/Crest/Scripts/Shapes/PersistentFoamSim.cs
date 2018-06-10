// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A persistent foam simulation that moves around with a displacement LOD.
    /// </summary>
    public class PersistentFoamSim : MonoBehaviour
    {
        [HideInInspector]
        public float _resolution = 1f;
        [HideInInspector]
        public int _shapeRenderLayer;

        Camera _cam;
        PingPongRts _pprts;

        Material _copySimMaterial = null;

        GameObject _renderSim;
        Material _renderSimMaterial;

        Vector3 _camPosSnappedLast;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            _pprts = GetComponent<PingPongRts>();

            CreateRenderSimQuad();

            _copySimMaterial = new Material(Shader.Find("Ocean/Shape/Sim/Foam Add To Disps"));
        }

        private void CreateRenderSimQuad()
        {
            // utility quad which will be rasterized by the shape camera
            _renderSim = CreateRasterQuad("RenderFoamSim");
            _renderSim.layer = _shapeRenderLayer;
            _renderSim.transform.parent = transform;
            _renderSim.transform.localScale = Vector3.one;
            _renderSim.transform.localPosition = Vector3.forward * 25f;
            _renderSim.transform.localRotation = Quaternion.identity;
            _renderSim.GetComponent<Renderer>().material = _renderSimMaterial = new Material(Shader.Find("Ocean/Shape/Sim/Foam"));
        }

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
        int _bufAssignedCamIdx = -1;

        void LateUpdate()
        {
            if (_copySimResultsCmdBuf == null)
            {
                _copySimResultsCmdBuf = new CommandBuffer();
                _copySimResultsCmdBuf.name = "CopyFoamSimResults";
            }

            int lodIndex = OceanRenderer.Instance.GetLodIndex(_resolution);

            // is the lod for the sim target resolution currently rendering?
            if (lodIndex == -1)
            {
                // no - clear the copy sim results command buffer
                if (_copySimResultsCmdBuf != null)
                {
                    _copySimResultsCmdBuf.Clear();
                }

                // unassign from any camera if it is assigned
                if (_bufAssignedCamIdx != -1)
                {
                    _cam.RemoveCommandBuffer(CameraEvent.AfterEverything, _copySimResultsCmdBuf);
                    //OceanRenderer.Instance.Builder._shapeCameras[_bufAssignedCamIdx].RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _copySimResultsCmdBuf);
                    _bufAssignedCamIdx = -1;
                }

                return;
            }

            // now make sure the command buffer is assigned to the correct camera
            if(_bufAssignedCamIdx != lodIndex)
            {
                if (_bufAssignedCamIdx != -1)
                {
                    _cam.RemoveCommandBuffer(CameraEvent.AfterEverything, _copySimResultsCmdBuf);
                    //OceanRenderer.Instance.Builder._shapeCameras[_bufAssignedCamIdx].RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _copySimResultsCmdBuf);
                }

                _cam.AddCommandBuffer(CameraEvent.AfterEverything, _copySimResultsCmdBuf);
                //OceanRenderer.Instance.Builder._shapeCameras[lodIndex].AddCommandBuffer(CameraEvent.AfterForwardAlpha, _copySimResultsCmdBuf);
                _bufAssignedCamIdx = lodIndex;
            }

            var lodCam = OceanRenderer.Instance.Builder._shapeCameras[lodIndex];
            var wdc = OceanRenderer.Instance.Builder._shapeWDCs[lodIndex];

            transform.position = lodCam.transform.position;

            _cam.orthographicSize = lodCam.orthographicSize;
            transform.localScale = (Vector3.right + Vector3.up) * _cam.orthographicSize * 2f + Vector3.forward;

            _cam.projectionMatrix = lodCam.projectionMatrix;

            Vector3 posDelta = wdc._renderData._posSnapped - _camPosSnappedLast;
            _renderSimMaterial.SetVector("_CameraPositionDelta", posDelta);
            _camPosSnappedLast = wdc._renderData._posSnapped;

            _renderSimMaterial.SetTexture("_FoamLastFrame", _pprts._sourceThisFrame);
            wdc.ApplyMaterialParams(0, new PropertyWrapperMaterial(_renderSimMaterial));

            if (_copySimMaterial)
            {
                _copySimMaterial.mainTexture = _pprts._targetThisFrame;

                _copySimResultsCmdBuf.Clear();
                // this does NOT work - 
                _copySimResultsCmdBuf.Blit(_pprts._targetThisFrame, lodCam.targetTexture, _copySimMaterial);
            }


            //Debug.Log(Time.frameCount + ": PersistentFoamSim::LateUpdate", this);
        }
    }
}
