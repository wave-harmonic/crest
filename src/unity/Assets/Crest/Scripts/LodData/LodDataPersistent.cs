// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A persistent simulation that moves around with a displacement LOD.
    /// </summary>
    public abstract class LodDataPersistent : LodData
    {
        public static readonly float MAX_SIM_DELTA_TIME = 1f / 30f;

        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Nothing; } }
        public override RenderTexture DataTexture { get { return Cam.targetTexture; } }

        [SerializeField]
        protected SimSettingsBase _settings;
        public override void UseSettings(SimSettingsBase settings) { _settings = settings; }

        GameObject _renderSim;
        Material _renderSimMaterial;

        protected abstract string ShaderSim { get; }
        protected abstract Camera[] SimCameras { get; }

        float _simDeltaTimePrev = 1f / 60f;
        protected float SimDeltaTime { get { return Mathf.Min(Time.deltaTime, MAX_SIM_DELTA_TIME); } }

        protected override void Start()
        {
            base.Start();

            CreateRenderSimQuad();
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

        CommandBuffer _advanceSimCmdBuf;
        float _oceanLocalScalePrev = -1f;

        public void BindSourceData(int shapeSlot, Material properties, bool paramsOnly)
        {
            _pwMat._target = properties;
            var rd = LodTransform._renderDataPrevFrame.Validate(-1, this);
            BindData(shapeSlot, _pwMat, paramsOnly ? Texture2D.blackTexture : (Texture)PPRTs.Source, true, ref rd);
            _pwMat._target = null;
        }


        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (_advanceSimCmdBuf == null)
            {
                _advanceSimCmdBuf = new CommandBuffer();
                _advanceSimCmdBuf.name = "AdvanceSim_" + SimName;
                Cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _advanceSimCmdBuf);
                _advanceSimCmdBuf.DrawRenderer(GetComponentInChildren<MeshRenderer>(), _renderSimMaterial);
            }

            float dt = SimDeltaTime;
            _renderSimMaterial.SetFloat("_SimDeltaTime", dt);
            _renderSimMaterial.SetFloat("_SimDeltaTimePrev", _simDeltaTimePrev);
            _simDeltaTimePrev = dt;

            // compute which lod data we are sampling source data from. if a scale change has happened this can be any lod up or down the chain.
            float oceanLocalScale = OceanRenderer.Instance.transform.localScale.x;
            if (_oceanLocalScalePrev == -1f) _oceanLocalScalePrev = oceanLocalScale;
            float ratio = oceanLocalScale / _oceanLocalScalePrev;
            _oceanLocalScalePrev = oceanLocalScale;
            float ratio_l2 = Mathf.Log(ratio) / Mathf.Log(2f);
            int delta = Mathf.RoundToInt(ratio_l2);

            int srcDataIdx = LodTransform.LodIndex + delta;

            if (srcDataIdx >= 0 && srcDataIdx < SimCameras.Length)
            {
                // bind data to slot 0 - previous frame data
                SimCameras[srcDataIdx].GetComponent<LodDataPersistent>().BindSourceData(0, _renderSimMaterial, false);
            }
            else
            {
                // no source data - bind params only
                BindSourceData(0, _renderSimMaterial, true);
            }

            SetAdditionalSimParams(_renderSimMaterial);

            LateUpdateInternal();
        }

        protected virtual void LateUpdateInternal()
        {
        }

        /// <summary>
        /// Set any sim-specific shader params.
        /// </summary>
        protected virtual void SetAdditionalSimParams(Material simMaterial)
        {
        }

        PingPongRts _pprts; protected PingPongRts PPRTs { get { return _pprts ?? (_pprts = GetComponent<PingPongRts>()); } }
    }
}
