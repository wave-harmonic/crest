// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using Unity.Collections;
using UnityEngine;

namespace Crest
{
    /// <summary>
    /// Samples horizontal motion of water volume
    /// </summary>
    public class QueryFlow : QueryBase, IFlowProvider
    {
        readonly int sp_LD_TexArray_Flow = Shader.PropertyToID("_LD_TexArray_Flow");
        readonly int sp_ResultFlows = Shader.PropertyToID("_ResultFlows");

        protected override string QueryShaderName => "QueryFlow";
        protected override string QueryKernelName => "CSMain";

        protected override void BindInputsAndOutputs(PropertyWrapperComputeStandalone wrapper, ComputeBuffer resultsBuffer)
        {
            LodDataMgrFlow.Bind(wrapper);
            ShaderProcessQueries.SetBuffer(_kernelHandle, sp_ResultFlows, resultsBuffer);
        }

#if CREST_BURST_QUERY
        public int Query(int i_ownerHash, float i_minSpatialLength, ref NativeArray<Vector3> i_queryPoints, ref NativeArray<Vector3> o_resultFlows)
        {
            return Query(i_ownerHash, i_minSpatialLength, ref i_queryPoints, ref o_resultFlows, ref QueryHelper.s_Skip, ref QueryHelper.s_Skip);
        }
#else
        public int Query(int i_ownerHash, float i_minSpatialLength, Vector3[] i_queryPoints, Vector3[] o_resultFlows)
        {
            return Query(i_ownerHash, i_minSpatialLength, i_queryPoints, o_resultFlows, null, null);
        }
#endif
    }
}
