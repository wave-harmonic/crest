// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A persistent simulation that moves around with a displacement LOD.
    /// </summary>
    public abstract class LodDataMgrPersistent : LodDataMgr
    {
        public static readonly float MAX_SIM_DELTA_TIME = 1f / 30f;

        public override CameraClearFlags CamClearFlags { get { return CameraClearFlags.Nothing; } }

        [SerializeField]
        protected SimSettingsBase _settings;
        public override void UseSettings(SimSettingsBase settings) { _settings = settings; }

        RenderTexture[] _sources;

        GameObject _renderSim;
        Material[] _renderSimMaterial;

        protected abstract string ShaderSim { get; }

        float _simDeltaTimePrev = 1f / 60f;
        protected float SimDeltaTime { get { return Mathf.Min(Time.deltaTime, MAX_SIM_DELTA_TIME); } }

        protected override void Start()
        {
            base.Start();

            _renderSimMaterial = new Material[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _renderSimMaterial.Length; i++)
            {
                _renderSimMaterial[i] = new Material(Shader.Find(ShaderSim));
            }
        }

        protected override void InitData()
        {
            base.InitData();

            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormat, 0);

            _sources = new RenderTexture[OceanRenderer.Instance.CurrentLodCount];
            for (int i = 0; i < _sources.Length; i++)
            {
                _sources[i] = new RenderTexture(desc);
                _sources[i].wrapMode = TextureWrapMode.Clamp;
                _sources[i].antiAliasing = 1;
                _sources[i].filterMode = FilterMode.Bilinear;
                _sources[i].anisoLevel = 0;
                _sources[i].useMipMap = false;
                _sources[i].name = SimName + "_" + i + "_1";
            }
        }

        public void BindSourceData(int lodIdx, int shapeSlot, Material properties, bool paramsOnly)
        {
            _pwMat._target = properties;
            var rd = OceanRenderer.Instance._lods[lodIdx]._renderDataPrevFrame.Validate(-1, this);
            BindData(lodIdx, shapeSlot, _pwMat, paramsOnly ? Texture2D.blackTexture : (Texture)_sources[lodIdx], true, ref rd);
            _pwMat._target = null;
        }

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            //if (_advanceSimCmdBuf == null)
            //{
            //    _advanceSimCmdBuf = new CommandBuffer();
            //    _advanceSimCmdBuf.name = "AdvanceSim_" + SimName;
            //    // TODO
            //    //Cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _advanceSimCmdBuf);
            //}

            //_advanceSimCmdBuf.Clear();

            var lodCount = OceanRenderer.Instance.CurrentLodCount;

            for (var lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                SwapRTs(ref _sources[lodIdx], ref _targets[lodIdx]);
            }

            for (var lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                var dt = SimDeltaTime;
                _renderSimMaterial[lodIdx].SetFloat("_SimDeltaTime", dt);
                _renderSimMaterial[lodIdx].SetFloat("_SimDeltaTimePrev", _simDeltaTimePrev);
                _simDeltaTimePrev = dt;

                // compute which lod data we are sampling source data from. if a scale change has happened this can be any lod up or down the chain.
                var srcDataIdx = lodIdx + _scaleDifferencePow2;

                if (srcDataIdx >= 0 && srcDataIdx < lodCount)
                {
                    // bind data to slot 0 - previous frame data
                    BindSourceData(srcDataIdx, 0, _renderSimMaterial[lodIdx], false);
                }
                else
                {
                    // no source data - bind params only
                    BindSourceData(lodIdx, 0, _renderSimMaterial[lodIdx], true);
                }

                SetAdditionalSimParams(lodIdx, _renderSimMaterial[lodIdx]);

                buf.SetRenderTarget(DataTexture(lodIdx));
                buf.Blit(null, _targets[lodIdx], _renderSimMaterial[lodIdx]);

                SubmitDraws(lodIdx, buf);

                if (!BuildCommandBufferInternal(lodIdx))
                    continue;
            }
        }

        protected virtual bool BuildCommandBufferInternal(int lodIdx)
        {
            return true;
        }

        /// <summary>
        /// Set any sim-specific shader params.
        /// </summary>
        protected virtual void SetAdditionalSimParams(int lodIdx, Material simMaterial)
        {
        }
    }
}
