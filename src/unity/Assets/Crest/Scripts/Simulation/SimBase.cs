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
        public static readonly float MAX_SIM_DELTA_TIME = 1f / 30f;

        int _lodIndex = -1;
        int _lodCount = -1;
        public void InitLODData(int lodIndex, int lodCount) { _lodIndex = lodIndex; _lodCount = lodCount; }

        [SerializeField]
        protected SimSettingsBase _settings;
        public void UseSettings(SimSettingsBase settings) { _settings = settings; }
        public virtual SimSettingsBase Settings { get { return _settings; } }

        Material _copySimMaterial = null;

        GameObject _renderSim;
        Material _renderSimMaterial;

        Vector3 _camPosSnappedLast;

        public abstract string SimName { get; }
        protected abstract string ShaderSim { get; }
        protected abstract string ShaderRenderResultsIntoDispTexture { get; }
        public abstract RenderTextureFormat TextureFormat { get; }
        public abstract int Depth { get; }
        protected abstract Camera[] SimCameras { get; }

        float _simDeltaTimePrev = 1f / 60f;
        protected float SimDeltaTime { get { return Mathf.Min(Time.deltaTime, MAX_SIM_DELTA_TIME); } }

        private void Start()
        {
            CreateRenderSimQuad();

            _copySimMaterial = new Material(Shader.Find(ShaderRenderResultsIntoDispTexture));
        }

        private void CreateRenderSimQuad()
        {
            // utility quad which will be rasterized by the shape camera
            _renderSim = CreateRasterQuad("RenderSim_" + SimName);
            _renderSim.transform.parent = transform;
            _renderSim.transform.localScale = Vector3.one * 4f;
            _renderSim.transform.localPosition = Vector3.forward * 25f;
            _renderSim.transform.localRotation = Quaternion.identity;
            _renderSim.GetComponent<Renderer>().material = _renderSimMaterial = new Material(Shader.Find(ShaderSim));
            _renderSim.GetComponent<Renderer>().enabled = false;
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

        CommandBuffer _advanceSimCmdBuf, _copySimResultsCmdBuf;
        float _oceanLocalScale = -1f;

        void LateUpdate()
        {
            if (_advanceSimCmdBuf == null)
            {
                _advanceSimCmdBuf = new CommandBuffer();
                _advanceSimCmdBuf.name = "AdvanceSim_" + SimName;
                Cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _advanceSimCmdBuf);
                _advanceSimCmdBuf.DrawRenderer(GetComponentInChildren<MeshRenderer>(), _renderSimMaterial);
            }


            if (_copySimResultsCmdBuf == null)
            {
                _copySimResultsCmdBuf = new CommandBuffer();
                _copySimResultsCmdBuf.name = "CopySimResults_" + SimName;
                OceanRenderer.Instance.Builder._shapeCameras[_lodIndex].AddCommandBuffer(CameraEvent.AfterForwardAlpha, _copySimResultsCmdBuf);
            }

            var lodCam = OceanRenderer.Instance.Builder._shapeCameras[_lodIndex];
            var wdc = OceanRenderer.Instance.Builder._shapeWDCs[_lodIndex];

            transform.position = lodCam.transform.position;

            Cam.orthographicSize = 2f * transform.lossyScale.x;

            Cam.projectionMatrix = lodCam.projectionMatrix;

            Vector3 posDelta = wdc._renderData._posSnapped - _camPosSnappedLast;
            _renderSimMaterial.SetVector("_CameraPositionDelta", posDelta);
            _camPosSnappedLast = wdc._renderData._posSnapped;

            float dt = SimDeltaTime;
            _renderSimMaterial.SetFloat("_SimDeltaTime", dt);
            _renderSimMaterial.SetFloat("_SimDeltaTimePrev", _simDeltaTimePrev);
            _simDeltaTimePrev = dt;

            float oceanLocalScale = OceanRenderer.Instance.transform.localScale.x;
            if (_oceanLocalScale == -1f)
            {
                _oceanLocalScale = OceanRenderer.Instance.transform.localScale.x;
            }
            else if (_oceanLocalScale == oceanLocalScale /*|| true*/)
            {
                // no change in scale - sample from same target as last frame
                _renderSimMaterial.SetTexture("_SimDataLastFrame", PPRTs.Source);
                wdc.ApplyMaterialParams(0, _renderSimMaterial);
            }
            else
            {
                // scale changed - transfer results up or down chain
                if (oceanLocalScale > _oceanLocalScale && _lodIndex < _lodCount - 1)
                {
                    // down chain
                    //Debug.Log(_lodIndex.ToString() + ": Sample from 1 above");
                    _renderSimMaterial.SetTexture("_SimDataLastFrame", SimCameras[_lodIndex + 1].GetComponent<PingPongRts>().Source);
                    OceanRenderer.Instance.Builder._shapeWDCs[_lodIndex + 1].ApplyMaterialParams(0, _renderSimMaterial);
                }
                else if (oceanLocalScale < _oceanLocalScale && _lodIndex > 0)
                {
                    // up chain
                    //Debug.Log(_lodIndex.ToString() + ": Sample from 1 below");
                    _renderSimMaterial.SetTexture("_SimDataLastFrame", SimCameras[_lodIndex - 1].GetComponent<PingPongRts>().Source);
                    OceanRenderer.Instance.Builder._shapeWDCs[_lodIndex - 1].ApplyMaterialParams(0, _renderSimMaterial);
                }
                else
                {
                    // at top or bottom of chain - no buffer to transfer results from - take 0 state
                    //Debug.Log(_lodIndex.ToString() + ": Clear");
                    _renderSimMaterial.SetTexture("_SimDataLastFrame", Texture2D.blackTexture);
                    wdc.ApplyMaterialParams(0, _renderSimMaterial);
                }

                _oceanLocalScale = oceanLocalScale;
            }

            SetAdditionalSimParams(_renderSimMaterial);

            if (_copySimMaterial)
            {
                _copySimMaterial.mainTexture = PPRTs.Target;

                SetAdditionalCopySimParams(_copySimMaterial);

                _copySimResultsCmdBuf.Clear();
                _copySimResultsCmdBuf.Blit(PPRTs.Target, lodCam.targetTexture, _copySimMaterial);
            }
        }

        /// <summary>
        /// Set any sim-specific shader params.
        /// </summary>
        protected virtual void SetAdditionalSimParams(Material simMaterial)
        {
        }

        /// <summary>
        /// Set any sim-specific shader params.
        /// </summary>
        protected virtual void SetAdditionalCopySimParams(Material copySimMaterial)
        {
        }

        PingPongRts _pprts2; protected PingPongRts PPRTs { get { return _pprts2 ?? (_pprts2 = GetComponent<PingPongRts>()); } }
        Camera _cam2; protected Camera Cam { get { return _cam2 ?? (_cam2 = GetComponent<Camera>()); } }
    }
}
