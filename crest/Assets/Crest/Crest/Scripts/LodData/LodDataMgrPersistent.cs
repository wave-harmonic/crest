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
        protected readonly int MAX_SIM_STEPS = 4;

        RenderTexture _sources;
        PropertyWrapperMaterial[,] _renderSimMaterial;

        protected abstract string ShaderSim { get; }

        float _substepDtPrevious = 1f / 60f;

        static int sp_SimDeltaTime = Shader.PropertyToID("_SimDeltaTime");
        static int sp_SimDeltaTimePrev = Shader.PropertyToID("_SimDeltaTimePrev");
        static int sp_GridSize = Shader.PropertyToID("_GridSize");

        protected override void Start()
        {
            base.Start();

            CreateMaterials(OceanRenderer.Instance.CurrentLodCount);
        }

        void CreateMaterials(int lodCount)
        {
            _renderSimMaterial = new PropertyWrapperMaterial[MAX_SIM_STEPS, lodCount];
            var shader = Shader.Find(ShaderSim);
            for (int stepi = 0; stepi < MAX_SIM_STEPS; stepi++)
            {
                for (int i = 0; i < lodCount; i++)
                {
                    _renderSimMaterial[stepi, i] = new PropertyWrapperMaterial(shader);
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

        }

        public void BindSourceData(int lodIdx, int shapeSlot, PropertyWrapperMaterial properties, bool paramsOnly, bool usePrevTransform)
        {
            var rd = usePrevTransform ?
                OceanRenderer.Instance._lods[lodIdx]._renderDataPrevFrame.Validate(BuildCommandBufferBase._lastUpdateFrame - Time.frameCount, this)
                : OceanRenderer.Instance._lods[lodIdx]._renderData.Validate(0, this);

            BindData(lodIdx, shapeSlot, properties, paramsOnly ? Texture2D.blackTexture : (Texture) _sources, true, ref rd);
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

                SwapRTs(ref _sources, ref _targets);

                for (var lodIdx = lodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    _renderSimMaterial[stepi, lodIdx].SetFloat(sp_SimDeltaTime, substepDt);
                    _renderSimMaterial[stepi, lodIdx].SetFloat(sp_SimDeltaTimePrev, _substepDtPrevious);

                    _renderSimMaterial[stepi, lodIdx].SetFloat(sp_GridSize, OceanRenderer.Instance._lods[lodIdx]._renderData._texelWidth);

                    // compute which lod data we are sampling source data from. if a scale change has happened this can be any lod up or down the chain.
                    // this is only valid on the first update step, after that the scale src/target data are in the right places.
                    var srcDataIdx = lodIdx + ((stepi == 0) ? ScaleDifferencePow2 : 0);

                    // only take transform from previous frame on first substep
                    var usePreviousFrameTransform = stepi == 0;

                    if (srcDataIdx >= 0 && srcDataIdx < lodCount)
                    {
                        // bind data to slot 0 - previous frame data
                        BindSourceData(srcDataIdx, 0, _renderSimMaterial[stepi, lodIdx], false, usePreviousFrameTransform);
                    }
                    else
                    {
                        // no source data - bind params only
                        BindSourceData(lodIdx, 0, _renderSimMaterial[stepi, lodIdx], true, usePreviousFrameTransform);
                    }

                    SetAdditionalSimParams(lodIdx, _renderSimMaterial[stepi, lodIdx]);

                    {
                        buf.SetRenderTarget(_targets, _targets.depthBuffer, 0, CubemapFace.Unknown, lodIdx);
                    }

                    buf.SetGlobalFloat("_LD_SLICE_Index_0", lodIdx);

                    buf.DrawMesh(FullScreenQuad(), Matrix4x4.identity, _renderSimMaterial[stepi, lodIdx].material);

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
        protected virtual void SetAdditionalSimParams(int lodIdx, PropertyWrapperMaterial simMaterial)
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

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnReLoadScripts()
        {
            var ocean = FindObjectOfType<OceanRenderer>();
            if (ocean == null) return;
            foreach (var ldp in ocean.GetComponents<LodDataMgrPersistent>())
            {
                // Unity does not serialize multidimensional arrays, or arrays of arrays. It does serialise arrays of objects containing arrays though.
                ldp.CreateMaterials(ocean.CurrentLodCount);
            }
        }
#endif

    }
}
