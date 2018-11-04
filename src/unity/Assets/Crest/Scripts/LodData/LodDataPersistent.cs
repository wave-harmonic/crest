// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

#if NODEF

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
        CommandBuffer _advanceSimCmdBuf;

        protected abstract string ShaderSim { get; }
        protected abstract LodDataPersistent[] SimComponents { get; }

        float _simDeltaTimePrev = 1f / 60f;
        protected float SimDeltaTime { get { return Mathf.Min(Time.deltaTime, MAX_SIM_DELTA_TIME); } }

        protected override void Start()
        {
            base.Start();

            _renderSimMaterial = new Material(Shader.Find(ShaderSim));
        }

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
            }

            _advanceSimCmdBuf.Clear();
            _advanceSimCmdBuf.Blit(null, PPRTs.Target, _renderSimMaterial);

            float dt = SimDeltaTime;
            _renderSimMaterial.SetFloat("_SimDeltaTime", dt);
            _renderSimMaterial.SetFloat("_SimDeltaTimePrev", _simDeltaTimePrev);
            _simDeltaTimePrev = dt;

            // compute which lod data we are sampling source data from. if a scale change has happened this can be any lod up or down the chain.
            int srcDataIdx = LodTransform.LodIndex + _scaleDifferencePow2;

            if (srcDataIdx >= 0 && srcDataIdx < SimComponents.Length)
            {
                // bind data to slot 0 - previous frame data
                SimComponents[srcDataIdx].BindSourceData(0, _renderSimMaterial, false);
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
    }
}
#endif
