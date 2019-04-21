// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;
using UnityEngine.Rendering;

namespace Crest
{
    /// <summary>
    /// A persistent simulation that moves around with a displacement LOD.
    /// </summary>
    public abstract class LodDataMgrPersistentCompute : LodDataMgr
    {
        protected readonly int MAX_SIM_STEPS = 4;

        RenderTexture[] _sources;
        PropertyWrapperCompute[,] _renderSimPropertyWrapperCompute;
        private ComputeShader _computeShader;
        private int _computeKernel;

        protected abstract string ShaderSim { get; }

        float _substepDtPrevious = 1f / 60f;

        protected override void Start()
        {
            base.Start();

            CreatePropertyWrapperComputes(OceanRenderer.Instance.CurrentLodCount);
        }

        void CreatePropertyWrapperComputes(int lodCount)
        {
            _renderSimPropertyWrapperCompute = new PropertyWrapperCompute[MAX_SIM_STEPS, lodCount];
            // var shader = Shader.Find(ShaderSim);
            _computeShader = Resources.Load<ComputeShader>(ShaderSim);
            _computeKernel = _computeShader.FindKernel(ShaderSim);

            for (int stepi = 0; stepi < MAX_SIM_STEPS; stepi++)
            {
                for (int i = 0; i < lodCount; i++)
                {
                    _renderSimPropertyWrapperCompute[stepi, i] = new PropertyWrapperCompute();
                }
            }
        }

        protected override void InitData()
        {
            base.InitData();

            Debug.Assert(SystemInfo.SupportsRenderTextureFormat(TextureFormat), "The graphics device does not support the render texture format " + TextureFormat.ToString());

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
                _sources[i].enableRandomWrite = true;
            }
        }

        public void BindSourceData(int lodIdx, int shapeSlot, PropertyWrapperCompute properties, bool paramsOnly, bool usePrevTransform)
        {
            var rd = usePrevTransform ?
                OceanRenderer.Instance._lods[lodIdx]._renderDataPrevFrame.Validate(BuildCommandBufferBase._lastUpdateFrame - Time.frameCount, this)
                : OceanRenderer.Instance._lods[lodIdx]._renderData.Validate(0, this);

            BindData(lodIdx, shapeSlot, properties, paramsOnly ? Texture2D.blackTexture : (Texture)_sources[lodIdx], true, ref rd);
        }

        public abstract void GetSimSubstepData(float frameDt, out int numSubsteps, out float substepDt);

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            var lodCount = OceanRenderer.Instance.CurrentLodCount;
            float substepDt;
            int numSubsteps;
            GetSimSubstepData(Time.deltaTime, out numSubsteps, out substepDt);

            for (int stepi = 0; stepi < numSubsteps; stepi++)
            {
                for (var lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    SwapRTs(ref _sources[lodIdx], ref _targets[lodIdx]);
                }

                for (var lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    _renderSimPropertyWrapperCompute[stepi, lodIdx].SetFloat(Shader.PropertyToID("_SimDeltaTime"), substepDt);
                    _renderSimPropertyWrapperCompute[stepi, lodIdx].SetFloat(Shader.PropertyToID("_SimDeltaTimePrev"), _substepDtPrevious);

                    _renderSimPropertyWrapperCompute[stepi, lodIdx].SetFloat(Shader.PropertyToID("_GridSize"), OceanRenderer.Instance._lods[lodIdx]._renderData._texelWidth);

                    // compute which lod data we are sampling source data from. if a scale change has happened this can be any lod up or down the chain.
                    // this is only valid on the first update step, after that the scale src/target data are in the right places.
                    var srcDataIdx = lodIdx + ((stepi == 0) ? ScaleDifferencePow2 : 0);

                    // only take transform from previous frame on first substep
                    var usePreviousFrameTransform = stepi == 0;

                    if (srcDataIdx >= 0 && srcDataIdx < lodCount)
                    {
                        // bind data to slot 0 - previous frame data
                        BindSourceData(srcDataIdx, 0, _renderSimPropertyWrapperCompute[stepi, lodIdx], false, usePreviousFrameTransform);
                    }
                    else
                    {
                        // no source data - bind params only
                        BindSourceData(lodIdx, 0, _renderSimPropertyWrapperCompute[stepi, lodIdx], true, usePreviousFrameTransform);
                    }

                    SetAdditionalSimParams(lodIdx, _renderSimPropertyWrapperCompute[stepi, lodIdx]);

                    {
                        var rt = DataTexture(lodIdx);
                        //buf.SetRenderTarget(rt, rt.depthBuffer);
                        if(!rt.IsCreated())
                        {
                            rt.Create();
                        }
                        buf.SetComputeTextureParam(
                            _computeShader,
                            _computeKernel,
                            "Result",
                            rt
                        );
                    }

                    _renderSimPropertyWrapperCompute[stepi, lodIdx].InitialiseAndDispatchShader(
                        buf,
                        _computeShader,
                        _computeKernel
                    );

                    SubmitDraws(lodIdx, buf);
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
        protected virtual void SetAdditionalSimParams(int lodIdx, PropertyWrapperCompute simPropertyWrapperCompute)
        {
        }

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnReLoadScripts()
        {
            var ocean = FindObjectOfType<OceanRenderer>();
            if (ocean == null) return;
            foreach (var ldp in ocean.GetComponents<LodDataMgrPersistentCompute>())
            {
                // Unity does not serialize multidimensional arrays, or arrays of arrays. It does serialise arrays of objects containing arrays though.
                ldp.CreatePropertyWrapperComputes(ocean.CurrentLodCount);
            }
        }
#endif

    }
}
