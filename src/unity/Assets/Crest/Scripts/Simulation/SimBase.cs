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

        [HideInInspector]
        public float _resolution = 1f;

        [SerializeField]
        protected SimSettingsBase _settings;
        public void UseSettings(SimSettingsBase settings) { _settings = settings; }

        GameObject _renderSim;
        Material _renderSimMaterial;
        Material _matClearSim;

        Vector3 _camPosSnappedLast;

        public abstract string SimName { get; }
        protected abstract string ShaderSim { get; }
        protected abstract string ShaderRenderResultsIntoDispTexture { get; }
        public abstract RenderTextureFormat TextureFormat { get; }
        public abstract int Depth { get; }
        public abstract SimSettingsBase CreateDefaultSettings();
        public RenderTexture SimTexture { get { return PPRTs.Target; }}

        // Override this function in order to determine how the results of a
        // given sim can be loaded into the Ocean system.
        protected abstract void LoadSimResults(Camera cam, WaveDataCam wdc);

        float _simDeltaTimePrev = 1f / 60f;
        protected float SimDeltaTime { get { return OceanRenderer.Instance._freezeTime ? 0f : Mathf.Min(Time.deltaTime, MAX_SIM_DELTA_TIME); } }

        private void Start()
        {
            CreateRenderSimQuad();

            _matClearSim = new Material(Shader.Find("Ocean/Shape/Sim/Clear"));
        }

        private void CreateRenderSimQuad()
        {
            // utility quad which will be rasterized by the shape camera
            _renderSim = CreateRasterQuad("RenderSim_" + SimName);
            _renderSim.transform.parent = transform;
            _renderSim.transform.localScale = Vector3.one;
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

        CommandBuffer _advanceSimCmdBuf;

        // TODO: Push this down into SimWave class as only it uses this.
        // (Other sims simply give their texture to the ocean).
        protected CommandBuffer _copySimResultsCmdBuf;
        int _bufAssignedCamIdx = -1;

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

                // clear the simulation data - so that it doesnt suddenly pop in later
                Graphics.Blit(Texture2D.blackTexture, PPRTs.Source, _matClearSim);
                Graphics.Blit(Texture2D.blackTexture, PPRTs.Target, _matClearSim);

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

            float dt = SimDeltaTime;
            _renderSimMaterial.SetFloat("_SimDeltaTime", dt);
            _renderSimMaterial.SetFloat("_SimDeltaTimePrev", _simDeltaTimePrev);
            if (!OceanRenderer.Instance._freezeTime)
                _simDeltaTimePrev = dt;

            _renderSimMaterial.SetTexture("_SimDataLastFrame", PPRTs.Source);
            wdc.ApplyMaterialParams(0, new PropertyWrapperMaterial(_renderSimMaterial));

            SetAdditionalSimParams(_renderSimMaterial);

            LoadSimResults(lodCam, wdc);

            AddPostRenderCommands(_copySimResultsCmdBuf);
        }

        /// <summary>
        /// Set any sim-specific shader params.
        /// </summary>
        protected virtual void SetAdditionalSimParams(Material simMaterial)
        {
        }

        /// <summary>
        /// Any render commands to perform after the sim has been advanced.
        /// </summary>
        /// <param name="postRenderCmdBuf"></param>
        protected virtual void AddPostRenderCommands(CommandBuffer postRenderCmdBuf)
        {
        }

        /// <summary>
        /// Called after all sims created - gives the sims a chance to reference eachother or do other init.
        /// </summary>
        public virtual void AllSimsCreated()
        {
        }

        PingPongRts _pprts2; protected PingPongRts PPRTs { get { return _pprts2 ?? (_pprts2 = GetComponent<PingPongRts>()); } }
        Camera _cam2; Camera Cam { get { return _cam2 ?? (_cam2 = GetComponent<Camera>()); } }
    }
}
