// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A persistent simulation that moves around with a displacement LOD.
    /// </summary>
    public abstract class SimBase : MonoBehaviour
    {
        [HideInInspector]
        public float _resolution = 1f;
        [HideInInspector]
        public int _shapeRenderLayer;

        Material _copySimMaterial = null;

        GameObject _renderSim;
        Material _renderSimMaterial;

        Vector3 _camPosSnappedLast;

        public abstract string SimName { get; }
        protected abstract string ShaderSim { get; }
        protected abstract string ShaderTextureLastSimResult { get; }
        protected abstract string ShaderRenderResultsIntoDispTexture { get; }
        public abstract RenderTextureFormat TextureFormat { get; }

        private void Start()
        {
            CreateRenderSimQuad();

            _copySimMaterial = new Material(Shader.Find(ShaderRenderResultsIntoDispTexture));
        }

        private void CreateRenderSimQuad()
        {
            // utility quad which will be rasterized by the shape camera
            _renderSim = CreateRasterQuad("RenderSim_" + SimName);
            _renderSim.layer = _shapeRenderLayer;
            _renderSim.transform.parent = transform;
            _renderSim.transform.localScale = Vector3.one;
            _renderSim.transform.localPosition = Vector3.forward * 25f;
            _renderSim.transform.localRotation = Quaternion.identity;
            _renderSim.GetComponent<Renderer>().material = _renderSimMaterial = new Material(Shader.Find(ShaderSim));
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
                _copySimResultsCmdBuf.name = "CopySimResults_" + SimName;
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
                    OceanRenderer.Instance.Builder._shapeCameras[_bufAssignedCamIdx].RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _copySimResultsCmdBuf);
                    _bufAssignedCamIdx = -1;
                }

                return;
            }

            // now make sure the command buffer is assigned to the correct camera
            if(_bufAssignedCamIdx != lodIndex)
            {
                if (_bufAssignedCamIdx != -1)
                {
                    OceanRenderer.Instance.Builder._shapeCameras[_bufAssignedCamIdx].RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _copySimResultsCmdBuf);
                }

                OceanRenderer.Instance.Builder._shapeCameras[lodIndex].AddCommandBuffer(CameraEvent.AfterForwardAlpha, _copySimResultsCmdBuf);
                _bufAssignedCamIdx = lodIndex;
            }

            var lodCam = OceanRenderer.Instance.Builder._shapeCameras[lodIndex];
            var wdc = OceanRenderer.Instance.Builder._shapeWDCs[lodIndex];

            transform.position = lodCam.transform.position;

            Cam.orthographicSize = lodCam.orthographicSize;
            transform.localScale = (Vector3.right + Vector3.up) * Cam.orthographicSize * 2f + Vector3.forward;

            Cam.projectionMatrix = lodCam.projectionMatrix;

            Vector3 posDelta = wdc._renderData._posSnapped - _camPosSnappedLast;
            _renderSimMaterial.SetVector("_CameraPositionDelta", posDelta);
            _camPosSnappedLast = wdc._renderData._posSnapped;

            _renderSimMaterial.SetTexture(ShaderTextureLastSimResult, PPRTs.Source);
            wdc.ApplyMaterialParams(0, new PropertyWrapperMaterial(_renderSimMaterial));

            if (_copySimMaterial)
            {
                _copySimMaterial.mainTexture = PPRTs.Target;

                _copySimResultsCmdBuf.Clear();
                _copySimResultsCmdBuf.Blit(PPRTs.Target, lodCam.targetTexture, _copySimMaterial);
            }
        }

        PingPongRts _pprts2; PingPongRts PPRTs { get { return _pprts2 ?? (_pprts2 = GetComponent<PingPongRts>()); } }
        Camera _cam2; Camera Cam { get { return _cam2 ?? (_cam2 = GetComponent<Camera>()); } }
    }
}
