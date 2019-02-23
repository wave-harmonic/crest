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
        [SerializeField]
        protected SimSettingsBase _settings;
        public override void UseSettings(SimSettingsBase settings) { _settings = settings; }

        protected readonly int MAX_SIM_STEPS = 4;

        RenderTexture[,] _sources;
        Material[,] _renderSimMaterial;

        protected abstract string ShaderSim { get; }

        float _substepDtPrevious = 1f / 60f;

        protected override void Start()
        {
            base.Start();

            var lodCount = OceanRenderer.Instance.CurrentLodCount;
            _renderSimMaterial = new Material[MAX_SIM_STEPS, lodCount];
            var shader = Shader.Find(ShaderSim);
            for (var stepi = 0; stepi < MAX_SIM_STEPS; stepi++)
            {
                for (var lodi = 0; lodi < lodCount; lodi++)
                {
                    _renderSimMaterial[stepi, lodi] = new Material(shader);
                }
            }
        }

        protected override void InitData()
        {
            base.InitData();

            var resolution = OceanRenderer.Instance.LodDataResolution;

            _sources = new RenderTexture[OceanRenderer.Instance.CurrentLodCount, NumDataTextures];
            for (var datai = 0; datai < NumDataTextures; datai++)
            {
                var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormats[datai], 0);

                for (var lodi = 0; lodi < OceanRenderer.Instance.CurrentLodCount; lodi++)
                {
                    _sources[lodi, datai] = new RenderTexture(desc);
                    _sources[lodi, datai].wrapMode = TextureWrapMode.Clamp;
                    _sources[lodi, datai].antiAliasing = 1;
                    _sources[lodi, datai].filterMode = FilterMode.Bilinear;
                    _sources[lodi, datai].anisoLevel = 0;
                    _sources[lodi, datai].useMipMap = false;
                    _sources[lodi, datai].name = SimName + datai + "_" + lodi + "_1";
                }
            }
        }

        public void BindSourceData(int lodIdx, int dataIdx, int shapeSlot, Material properties, bool paramsOnly, bool usePrevTransform)
        {
            _pwMat._target = properties;

            var rd = usePrevTransform ?
                OceanRenderer.Instance._lods[lodIdx]._renderDataPrevFrame.Validate(BuildCommandBufferBase._lastUpdateFrame - Time.frameCount, this)
                : OceanRenderer.Instance._lods[lodIdx]._renderData.Validate(0, this);

            BindData(lodIdx, dataIdx, shapeSlot, _pwMat, paramsOnly ? Texture2D.blackTexture : (Texture)_sources[lodIdx, dataIdx], true, ref rd);
            _pwMat._target = null;
        }

        protected abstract int GetNumSubsteps(float dt);

        public override void BuildCommandBuffer(OceanRenderer ocean, CommandBuffer buf)
        {
            base.BuildCommandBuffer(ocean, buf);

            var lodCount = OceanRenderer.Instance.CurrentLodCount;
            var steps = GetNumSubsteps(Time.deltaTime);
            var substepDt = Time.deltaTime / steps;

            for (int stepi = 0; stepi < steps; stepi++)
            {
                for (var lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    for (var dataIdx = 0; dataIdx < NumDataTextures; dataIdx++)
                    {
                        SwapRTs(ref _sources[lodIdx, dataIdx], ref _targets[lodIdx, dataIdx]);
                    }
                }

                for (var lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    _renderSimMaterial[stepi, lodIdx].SetFloat("_SimDeltaTime", substepDt);
                    _renderSimMaterial[stepi, lodIdx].SetFloat("_SimDeltaTimePrev", _substepDtPrevious);

                    _renderSimMaterial[stepi, lodIdx].SetFloat("_GridSize", OceanRenderer.Instance._lods[lodIdx]._renderData._texelWidth);

                    // compute which lod data we are sampling source data from. if a scale change has happened this can be any lod up or down the chain.
                    // this is only valid on the first update step, after that the scale src/target data are in the right places.
                    var srcDataIdx = lodIdx + ((stepi == 0) ? ScaleDifferencePow2 : 0);

                    // only take transform from previous frame on first substep
                    var usePreviousFrameTransform = stepi == 0;

                    if (srcDataIdx >= 0 && srcDataIdx < lodCount)
                    {
                        // bind data to slot 0 - previous frame data
                        for (var dataIdx = 0; dataIdx < NumDataTextures; dataIdx++)
                        {
                            BindSourceData(srcDataIdx, dataIdx, 0, _renderSimMaterial[stepi, lodIdx], false, usePreviousFrameTransform);
                        }
                    }
                    else
                    {
                        // no source data - bind params only
                        for (var dataIdx = 0; dataIdx < NumDataTextures; dataIdx++)
                        {
                            BindSourceData(lodIdx, dataIdx, 0, _renderSimMaterial[stepi, lodIdx], true, usePreviousFrameTransform);
                        }
                    }

                    SetAdditionalSimParams(lodIdx, _renderSimMaterial[stepi, lodIdx]);

                    if (NumDataTextures == 1)
                    {
                        var rt = DataTexture(lodIdx, 0);
                        buf.SetRenderTarget(rt, rt.depthBuffer);
                    }
                    else
                    {
                        var rt0 = DataTexture(lodIdx, 0);
                        var rt1 = DataTexture(lodIdx, 1);
                        buf.SetRenderTarget(new RenderTargetIdentifier[] { rt0, rt1 }, rt0.depthBuffer);
                    }

                    buf.DrawMesh(FullScreenQuad(), Matrix4x4.identity, _renderSimMaterial[stepi, lodIdx]);

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
        protected virtual void SetAdditionalSimParams(int lodIdx, Material simMaterial)
        {
        }

        static Mesh s_fullScreenQuad;
        static Mesh FullScreenQuad()
        {
            if (s_fullScreenQuad != null) return s_fullScreenQuad;

            s_fullScreenQuad = new Mesh();
            s_fullScreenQuad.name = "FullScreenQuad";
            s_fullScreenQuad.vertices = new Vector3[]
            {
                new Vector3(-1f, -1f, 0.1f),
                new Vector3(-1f,  1f, 0.1f),
                new Vector3( 1f,  1f, 0.1f),
                new Vector3( 1f, -1f, 0.1f),
            };
            s_fullScreenQuad.uv = new Vector2[]
            {
                Vector2.up,
                Vector2.zero,
                Vector2.right,
                Vector2.one,
            };

            s_fullScreenQuad.SetIndices(new int[]
            {
                0, 2, 1, 0, 3, 2
            }, MeshTopology.Triangles, 0);

            return s_fullScreenQuad;
        }
    }
}
