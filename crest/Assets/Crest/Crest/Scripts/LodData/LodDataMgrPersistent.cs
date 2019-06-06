// Crest Ocean System

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
        protected override bool NeedToReadWriteTextureData { get { return true; } }

        protected readonly int MAX_SIM_STEPS = 4;

        RenderTexture _sources;
        PropertyWrapperCompute[,] _renderSimProperties;

        static int sp_LD_TexArray_Target = Shader.PropertyToID("_LD_TexArray_Target");

        protected ComputeShader _shader;

        protected abstract string ShaderSim { get; }
        protected abstract int krnl_ShaderSim { get; }

        float _substepDtPrevious = 1f / 60f;

        static int sp_SimDeltaTime = Shader.PropertyToID("_SimDeltaTime");
        static int sp_SimDeltaTimePrev = Shader.PropertyToID("_SimDeltaTimePrev");
        static int sp_GridSize = Shader.PropertyToID("_GridSize");

        protected override void Start()
        {
            base.Start();

            CreateProperties(OceanRenderer.Instance.CurrentLodCount);
        }

        void CreateProperties(int lodCount)
        {
            _shader = Resources.Load<ComputeShader>(ShaderSim);
            _renderSimProperties = new PropertyWrapperCompute[MAX_SIM_STEPS, lodCount];
            for (int stepi = 0; stepi < MAX_SIM_STEPS; stepi++)
            {
                for (int i = 0; i < lodCount; i++)
                {
                    _renderSimProperties[stepi, i] = new PropertyWrapperCompute();
                }
            }
        }

        protected override void InitData()
        {
            base.InitData();

            Debug.Assert(SystemInfo.SupportsRenderTextureFormat(TextureFormat), "The graphics device does not support the render texture format " + TextureFormat.ToString());

            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormat, 0);


            _sources = new RenderTexture(desc);
            _sources.wrapMode = TextureWrapMode.Clamp;
            _sources.antiAliasing = 1;
            _sources.filterMode = FilterMode.Bilinear;
            _sources.anisoLevel = 0;
            _sources.useMipMap = false;
            _sources.name = SimName;
            _sources.dimension = TextureDimension.Tex2DArray;
            _sources.volumeDepth = OceanRenderer.Instance.CurrentLodCount;
            _sources.enableRandomWrite = NeedToReadWriteTextureData;

        }

        public void BindSourceData(IPropertyWrapper properties, bool paramsOnly, bool usePrevTransform, bool prevFrame = false)
        {
            //TODO(MRT): Call Validate to make sure things work here.
            // var renderData = usePrevTransform ?
            //     LodTransform._staticRenderDataPrevFrame.Validate(BuildCommandBufferBase._lastUpdateFrame - Time.frameCount, this)
            //     : LodTransform._staticRenderData.Validate(0, this);


            var renderData = usePrevTransform ?
                LodTransform._staticRenderDataPrevFrame
                : LodTransform._staticRenderData;

            BindData(properties, paramsOnly ? TextureArray.Black : (Texture) _sources, true, ref renderData, prevFrame);
        }

        public abstract void GetSimSubstepData(float frameDt, out int numSubsteps, out float substepDt);

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            var lodCount = OceanRenderer.Instance.CurrentLodCount;
            float substepDt;
            int numSubsteps;
            GetSimSubstepData(Time.deltaTime, out numSubsteps, out substepDt);
            if(!_sources.IsCreated())
            {
                _sources.Create();
            }
            if(!_targets.IsCreated())
            {
                _targets.Create();
            }

            for (int stepi = 0; stepi < numSubsteps; stepi++)
            {

                SwapRTs(ref _sources, ref _targets);

                for (var lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    _renderSimProperties[stepi, lodIdx].Initialise(buf, _shader, krnl_ShaderSim);

                    _renderSimProperties[stepi, lodIdx].SetFloat(sp_SimDeltaTime, substepDt);
                    _renderSimProperties[stepi, lodIdx].SetFloat(sp_SimDeltaTimePrev, _substepDtPrevious);

                    _renderSimProperties[stepi, lodIdx].SetFloat(sp_GridSize, OceanRenderer.Instance._lods[lodIdx]._renderData._texelWidth);

                    // compute which lod data we are sampling source data from. if a scale change has happened this can be any lod up or down the chain.
                    // this is only valid on the first update step, after that the scale src/target data are in the right places.
                    var srcDataIdx = lodIdx + ((stepi == 0) ? ScaleDifferencePow2 : 0);

                    // only take transform from previous frame on first substep
                    var usePreviousFrameTransform = stepi == 0;

                    if (srcDataIdx >= 0 && srcDataIdx < lodCount)
                    {
                        // bind data to slot 0 - previous frame data
                        BindSourceData(_renderSimProperties[stepi, lodIdx], false, usePreviousFrameTransform, true);
                    }
                    else
                    {
                        // no source data - bind params only
                        BindSourceData(_renderSimProperties[stepi, lodIdx], true, usePreviousFrameTransform, true);
                    }

                    SetAdditionalSimParams(lodIdx, _renderSimProperties[stepi, lodIdx]);

                    buf.SetRenderTarget(_targets, _targets.depthBuffer, 0, CubemapFace.Unknown, lodIdx);

                    buf.SetGlobalFloat("_LD_SLICE_Index_ThisLod", lodIdx);
                    // TODO(MRT): Set correct LOD for frame
                    buf.SetGlobalFloat("_LD_SLICE_Index_ThisLod_PrevFrame", srcDataIdx);

                    _renderSimProperties[stepi, lodIdx].SetTexture(
                        sp_LD_TexArray_Target,
                        DataTexture
                    );

                    _renderSimProperties[stepi, lodIdx].DispatchShader();
                }

                _substepDtPrevious = substepDt;
            }

            // any post-sim steps. the dyn waves updates the copy sim material, which the anim wave will later use to copy in
            // the dyn waves results.
            for (var lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
            {
                BuildCommandBufferInternal(lodIdx);
            }
        }

        protected virtual bool BuildCommandBufferInternal(int lodIdx)
        {
            return true;
        }

        /// <summary>
        /// Set any sim-specific shader params.
        /// </summary>
        protected virtual void SetAdditionalSimParams(int lodIdx, IPropertyWrapper simMaterial)
        {
        }

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        protected static void OnReLoadScripts()
        {
            var ocean = FindObjectOfType<OceanRenderer>();
            if (ocean == null) return;
            foreach (var ldp in ocean.GetComponents<LodDataMgrPersistent>())
            {
                // Unity does not serialize multidimensional arrays, or arrays of arrays. It does serialise arrays of objects containing arrays though.
                ldp.CreateProperties(ocean.CurrentLodCount);
            }
        }
#endif

    }
}
