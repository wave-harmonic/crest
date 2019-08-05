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

        RenderTexture[] _sources;
        PropertyWrapperCompute _renderSimProperties;

        static int sp_LD_Texture_Target = Shader.PropertyToID("_LD_Texture_Target");

        protected ComputeShader _shader;

        protected abstract string ShaderSim { get; }
        protected abstract int krnl_ShaderSim { get; }

        float _substepDtPrevious = 1f / 60f;

        static int sp_SimDeltaTime = Shader.PropertyToID("_SimDeltaTime");
        static int sp_SimDeltaTimePrev = Shader.PropertyToID("_SimDeltaTimePrev");

        protected override void Start()
        {
            base.Start();

            CreateProperties(OceanRenderer.Instance.CurrentLodCount);
        }

        void CreateProperties(int lodCount)
        {
            _shader = Resources.Load<ComputeShader>(ShaderSim);
            _renderSimProperties = new PropertyWrapperCompute();
        }

        protected override void InitData()
        {
            base.InitData();

            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormat, 0);
            _sources = CreateLodDataTextures(desc, SimName + "_1", NeedToReadWriteTextureData);
        }

        public void ValidateSourceData(bool usePrevTransform)
        {
            var renderDataToValidate = usePrevTransform ?
                OceanRenderer.Instance._lodTransform._renderDataSource
                : OceanRenderer.Instance._lodTransform._renderData;
            int validationFrame = usePrevTransform ? BuildCommandBufferBase._lastUpdateFrame - Time.frameCount : 0;
            foreach (var renderData in renderDataToValidate)
            {
                renderData.Validate(validationFrame, this);
            }
        }

        public void BindSourceOceanParams(IPropertyWrapper properties, bool usePreviousTransform)
        {
            var renderData = usePreviousTransform ?
                OceanRenderer.Instance._lodTransform._renderDataSource
                : OceanRenderer.Instance._lodTransform._renderData;
            BindData(properties, ref renderData, true, true);
        }

        public void BindSourceTexture(IPropertyWrapper properties, int lodIndex)
        {
            BindLodTexture(properties, lodIndex < _sources.Length && lodIndex >= 0 ? (Texture) _sources[lodIndex] : Texture2D.blackTexture, LodIdType.SourceLod);
        }

        public abstract void GetSimSubstepData(float frameDt, out int numSubsteps, out float substepDt);

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            var lodCount = ocean.CurrentLodCount;

            float substepDt;
            int numSubsteps;
            GetSimSubstepData(ocean.DeltaTime, out numSubsteps, out substepDt);

            for (int stepi = 0; stepi < numSubsteps; stepi++)
            {
                SwapRTs(ref _sources, ref _targets);

                _renderSimProperties.Initialise(buf, _shader, krnl_ShaderSim);

                _renderSimProperties.SetFloat(sp_SimDeltaTime, substepDt);
                _renderSimProperties.SetFloat(sp_SimDeltaTimePrev, _substepDtPrevious);

                // compute which lod data we are sampling source data from. if a scale change has happened this can be any lod up or down the chain.
                // this is only valid on the first update step, after that the scale src/target data are in the right places.
                var srcDataIdxChange = ((stepi == 0) ? ScaleDifferencePow2 : 0);

                // only take transform from previous frame on first substep
                var usePreviousFrameTransform = stepi == 0;

                // bind data to slot 0 - previous frame data
                ValidateSourceData(usePreviousFrameTransform);

                buf.SetGlobalFloat(OceanRenderer.sp_LODChange, srcDataIdxChange);

                for(int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    BindSourceOceanParams(_renderSimProperties, usePreviousFrameTransform);
                    BindSourceTexture(_renderSimProperties, lodIdx);
                    SetAdditionalSimParams(_renderSimProperties, lodIdx);

                    _renderSimProperties.SetTexture(
                        sp_LD_Texture_Target,
                        DataTexture(lodIdx)
                    );
                    _renderSimProperties.SetFloat(OceanRenderer.sp_LD_SliceIndex, lodIdx);
                    _renderSimProperties.DispatchShader();
                }

                for (int lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    buf.SetRenderTarget(_targets[lodIdx], _targets[lodIdx].depthBuffer, 0);
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
        protected virtual void SetAdditionalSimParams(IPropertyWrapper simMaterial, int lodIndex)
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
