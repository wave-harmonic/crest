// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Registers a custom input to the clip surface simulation. Attach this to GameObjects that you want to use to
    /// clip the surface of the ocean.
    /// </summary>
    public class RegisterClipSurfaceInput : RegisterLodDataInput<LodDataMgrClipSurface>
    {
        bool _enabled = true;
        public override bool Enabled => _enabled;

        [Header("Convex Hull Options")]

        [Tooltip("Prevents inputs from cancelling each other out when aligned vertically. It is imperfect so custom logic might be needed for your use case.")]
        [SerializeField] bool _disableClipSurfaceWhenTooFarFromSurface = false;

        [Tooltip("Large, choppy waves require higher iterations to have accurate holes.")]
        [SerializeField] uint _animatedWavesDisplacementSamplingIterations = 4;

        public override float Wavelength(OceanRenderer ocean) => 0f;

        protected override Color GizmoColor => new Color(0f, 1f, 1f, 0.5f);

        protected override string ShaderPrefix => "Crest/Inputs/Clip Surface";

        // The clip surface samples at the displaced position in the ocean shader, so the displacement correction is not needed.
        protected override bool FollowHorizontalMotion => true;

        PropertyWrapperMPB _mpb;
        SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();

        static int sp_DisplacementSamplingIterations = Shader.PropertyToID("_DisplacementSamplingIterations");

        private void LateUpdate()
        {
            // Prevents possible conflicts since overlapping doesn't work for every case.
            if (_disableClipSurfaceWhenTooFarFromSurface)
            {
                var position = transform.position;
                _sampleHeightHelper.Init(position, 0f);

                if (_sampleHeightHelper.Sample(out float waterHeight))
                {
                    position.y = waterHeight;
                    _enabled = Mathf.Abs(_renderer.bounds.ClosestPoint(position).y - waterHeight) < 1;
                }
            }
            else
            {
                _enabled = true;
            }

            // find which lod this object is overlapping
            var rect = new Rect(transform.position.x, transform.position.z, 0f, 0f);
            // TODO - tricky. see todo below.
            var lodIdx = LodDataMgrAnimWaves.SuggestDataLOD(OceanRenderer.AnyInstance, rect);

            if (lodIdx > -1)
            {
                if (_mpb == null)
                {
                    _mpb = new PropertyWrapperMPB();
                }

                _renderer.GetPropertyBlock(_mpb.materialPropertyBlock);

                // todo - hmm this is tricky :/ - clip data does things related to the current lod. cant use material property block then.
                // move onto command buffer?
                var lodDataAnimWaves = OceanRenderer.AnyInstance._lodDataAnimWaves;
                _mpb.SetInt(LodDataMgr.sp_LD_SliceIndex, lodIdx);
                _mpb.SetInt(sp_DisplacementSamplingIterations, (int)_animatedWavesDisplacementSamplingIterations);
                lodDataAnimWaves.BindResultData(_mpb);

                // todo - @daleeidd is any of this needed for the clip data?

                //// blend LOD 0 shape in/out to avoid pop, if the ocean might scale up later (it is smaller than its maximum scale)
                //bool needToBlendOutShape = lodIdx == 0 && OceanRenderer.Instance.ScaleCouldIncrease;
                //float meshScaleLerp = needToBlendOutShape ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 0f;

                //// blend furthest normals scale in/out to avoid pop, if scale could reduce
                //var lodCount = OceanRenderer.Instance.CurrentLodCount;
                //bool needToBlendOutNormals = lodIdx == lodCount - 1 && OceanRenderer.Instance.ScaleCouldDecrease;
                //float farNormalsWeight = needToBlendOutNormals ? OceanRenderer.Instance.ViewerAltitudeLevelAlpha : 1f;
                //_mpb.SetVector(OceanChunkRenderer.sp_InstanceData, new Vector3(meshScaleLerp, farNormalsWeight, lodIdx));

                _renderer.SetPropertyBlock(_mpb.materialPropertyBlock);
            }
        }
    }
}
