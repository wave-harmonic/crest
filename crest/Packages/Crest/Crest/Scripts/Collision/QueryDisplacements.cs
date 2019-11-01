// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Samples water surface shape - displacement, height, normal, velocity.
    /// </summary>
    public class QueryDisplacements : QueryBase, ICollProvider
    {
        readonly static int sp_LD_TexArray_AnimatedWaves = Shader.PropertyToID("_LD_TexArray_AnimatedWaves");
        readonly static int sp_ResultDisplacements = Shader.PropertyToID("_ResultDisplacements");

        protected override string QueryShaderName => "QueryDisplacements";
        protected override string QueryKernelName => "CSMain";

        public static QueryDisplacements Instance { get; private set; }

        protected override void OnEnable()
        {
            Debug.Assert(Instance == null);
            Instance = this;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Instance = null;

            base.OnDisable();
        }

        protected override void BindInputsAndOutputs(PropertyWrapperComputeStandalone wrapper, ComputeBuffer resultsBuffer)
        {
            OceanRenderer.Instance._lodDataAnimWaves.BindResultData(wrapper);
            ShaderProcessQueries.SetTexture(_kernelHandle, sp_LD_TexArray_AnimatedWaves, OceanRenderer.Instance._lodDataAnimWaves.DataTexture);
            ShaderProcessQueries.SetBuffer(_kernelHandle, sp_ResultDisplacements, resultsBuffer);
        }

        public bool GetSamplingData(ref Rect i_displacedSamplingArea, float i_minSpatialLength, SamplingData o_samplingData)
        {
            // Trivial. Will likely remove this in the future if we can deprecate the displacement texture readback stuff.
            o_samplingData._minSpatialLength = i_minSpatialLength;
            return true;
        }

        public void ReturnSamplingData(SamplingData i_data)
        {
            // Mark invalid
            i_data._minSpatialLength = -1f;
        }

        public int Query(int i_ownerHash, SamplingData i_samplingData, Vector3[] i_queryPoints, float[] o_resultHeights, Vector3[] o_resultNorms, Vector3[] o_resultVels)
        {
            var result = (int)QueryStatus.OK;

            if (!UpdateQueryPoints(i_ownerHash, i_samplingData, o_resultNorms != null ? i_queryPoints : null, i_queryPoints))
            {
                result |= (int)QueryStatus.PostFailed;
            }

            if (!RetrieveResults(i_ownerHash, null, o_resultHeights, o_resultNorms))
            {
                result |= (int)QueryStatus.RetrieveFailed;
            }

            if (o_resultVels != null)
            {
                result |= CalculateVelocities(i_ownerHash, i_samplingData, i_queryPoints, o_resultVels);
            }

            return result;
        }

        public bool SampleDisplacement(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement)
        {
            throw new System.NotImplementedException("Not implemented for the Compute collision provider - use the 'Query' functions.");
        }

        public void SampleDisplacementVel(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 o_displacement, out bool o_displacementValid, out Vector3 o_displacementVel, out bool o_velValid)
        {
            throw new System.NotImplementedException("Not implemented for the Compute collision provider - use the 'Query' functions.");
        }

        public bool SampleHeight(ref Vector3 i_worldPos, SamplingData i_samplingData, out float o_height)
        {
            throw new System.NotImplementedException("Not implemented for the Compute collision provider - use the 'Query' functions.");
        }

        public bool SampleNormal(ref Vector3 i_undisplacedWorldPos, SamplingData i_samplingData, out Vector3 o_normal)
        {
            throw new System.NotImplementedException("Not implemented for the Compute collision provider - use the 'Query' functions.");
        }

        public bool ComputeUndisplacedPosition(ref Vector3 i_worldPos, SamplingData i_samplingData, out Vector3 undisplacedWorldPos)
        {
            throw new System.NotImplementedException("Not implemented for the Compute collision provider - use the 'Query' functions.");
        }

        public AvailabilityResult CheckAvailability(ref Vector3 i_worldPos, SamplingData i_samplingData)
        {
            throw new System.NotImplementedException("Not implemented for the Compute collision provider - use the 'Query' functions.");
        }
    }
}
