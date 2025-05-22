// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Unity.Collections;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Samples water surface shape - displacement, height, normal, velocity.
    /// </summary>
    public class QueryDisplacements : QueryBase, ICollProvider
    {
        readonly int sp_LD_TexArray_AnimatedWaves = Shader.PropertyToID("_LD_TexArray_AnimatedWaves");
        readonly int sp_ResultDisplacements = Shader.PropertyToID("_ResultDisplacements");

        protected override string QueryShaderName => "QueryDisplacements";
        protected override string QueryKernelName => "CSMain";

        protected override void BindInputsAndOutputs(PropertyWrapperComputeStandalone wrapper, ComputeBuffer resultsBuffer)
        {
            LodDataMgrAnimWaves.Bind(wrapper);
            LodDataMgrSeaFloorDepth.Bind(wrapper);

            ShaderProcessQueries.SetTexture(_kernelHandle, sp_LD_TexArray_AnimatedWaves, OceanRenderer.Instance._lodDataAnimWaves.DataTexture);
            ShaderProcessQueries.SetBuffer(_kernelHandle, sp_ResultDisplacements, resultsBuffer);

            ShaderProcessQueries.SetBuffer(_kernelHandle, OceanRenderer.sp_cascadeData, OceanRenderer.Instance._bufCascadeDataTgt);
        }

#if CREST_BURST_QUERY
        public int Query(int i_ownerHash, float i_minSpatialLength, ref NativeArray<Vector3> i_queryPoints, ref NativeArray<float> o_resultHeights, ref NativeArray<Vector3> o_resultNorms, ref NativeArray<Vector3> o_resultVels)
#else
        public int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, float[] o_resultHeights, Vector3[] o_resultNorms, Vector3[] o_resultVels)
#endif
        {
            var result = (int)QueryStatus.OK;

#if CREST_BURST_QUERY
            var useHeight = o_resultHeights.Length > 0;
            var useNormal = o_resultNorms.Length > 0;
            var useVelocity = o_resultVels.Length > 0;
#else
            var useHeight = o_resultHeights?.Length > 0;
            var useNormal = o_resultNorms?.Length > 0;
            var useVelocity = o_resultVels?.Length > 0;
#endif

#if CREST_BURST_QUERY
            if (!UpdateQueryPoints(i_ownerHash, i_minSpatialLength, i_queryPoints, useNormal ? i_queryPoints : default, useNormal))
#else
            if (!UpdateQueryPoints(i_ownerHash, i_minSpatialLength, i_queryPoints, useNormal ? i_queryPoints : null))
#endif
            {
                result |= (int)QueryStatus.PostFailed;
            }

#if CREST_BURST_QUERY
            if (!RetrieveResults(i_ownerHash, default, o_resultHeights, o_resultNorms))
#else
            if (!RetrieveResults(i_ownerHash, null, o_resultHeights, o_resultNorms))
#endif
            {
                result |= (int)QueryStatus.RetrieveFailed;
            }

            if (useVelocity)
            {
                result |= CalculateVelocities(i_ownerHash, o_resultVels);
            }

            return result;
        }
    }
}
