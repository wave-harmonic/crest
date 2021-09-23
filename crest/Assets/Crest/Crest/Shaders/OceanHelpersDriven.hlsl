// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Helpers that use 'driven' data - i.e. has backing C# to set shader parameters like the slice index and etc. An
// example of this setup code can be found in OceanChunkRenderer.OnWillRenderObject().

#ifndef CREST_OCEAN_HELPERS_DRIVEN_H
#define CREST_OCEAN_HELPERS_DRIVEN_H

// Used to get the world position of the ocean surface from the world position
half4 SampleOceanDataAtWorldPosition(in Texture2DArray i_oceanData, in const float3 i_positionWS)
{
	// Sample ocean data textures - always lerp between 2 scales, so sample two textures

	const float meshScaleLerp = _CrestPerCascadeInstanceData[_LD_SliceIndex]._meshScaleLerp;
	float lodAlpha = ComputeLodAlpha(i_positionWS, meshScaleLerp, _CrestCascadeData[0]);

	// Sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
	float wt_smallerLod = (1.0 - lodAlpha) * _CrestCascadeData[_LD_SliceIndex]._weight;
	float wt_biggerLod = (1.0 - wt_smallerLod) * _CrestCascadeData[_LD_SliceIndex + 1]._weight;

	// Sample data textures
	half4 result = 0.0;
	if (wt_smallerLod > 0.001)
	{
		float3 uv_slice = WorldToUV(i_positionWS.xz, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);
		result += wt_smallerLod * i_oceanData.SampleLevel(LODData_linear_clamp_sampler, uv_slice, 0.0);
	}
	if (wt_biggerLod > 0.001)
	{
		float3 uv_slice = WorldToUV(i_positionWS.xz, _CrestCascadeData[_LD_SliceIndex + 1], _LD_SliceIndex + 1);
		result += wt_biggerLod * i_oceanData.SampleLevel(LODData_linear_clamp_sampler, uv_slice, 0.0);
	}

	return result;
}

float3 ComputeDisplacement(float2 undispPos, float minSlice, const float baseScale)
{
	uint slice0, slice1;
	float lodAlpha;
	PosToSliceIndices(undispPos, minSlice, baseScale, slice0, slice1, lodAlpha);

	const float3 uv0 = WorldToUV(undispPos, _CrestCascadeData[slice0], slice0);
	const float3 uv1 = WorldToUV(undispPos, _CrestCascadeData[slice1], slice1);

	const float wt_0 = (1. - lodAlpha) * _CrestCascadeData[slice0]._weight;
	const float wt_1 = (1. - wt_0) * _CrestCascadeData[slice1]._weight;

	return
		wt_0 * _LD_TexArray_AnimatedWaves.SampleLevel(LODData_linear_clamp_sampler, uv0, 0).xyz +
		wt_1 * _LD_TexArray_AnimatedWaves.SampleLevel(LODData_linear_clamp_sampler, uv1, 0).xyz;
}

// Used to get the world position of the ocean surface from the world position by using fixed-point iteration
float3 SampleOceanDataDisplacedToWorldPosition(in const Texture2DArray i_oceanData, in const float3 i_positionWS, in const uint i_iterations)
{
	float3 undisplacedPosition = InvertDisplacement(i_oceanData, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex, i_positionWS, i_iterations);
	// return SampleOceanDataAtWorldPosition(i_oceanData, undisplacedPosition);

	const float minSlice = clamp(_LD_SliceIndex, 0.0, _SliceCount - 2.0);
	const float baseScale = _CrestCascadeData[0]._scale;
	float2 undispPos = i_positionWS.xz;
	for (uint i = 0; i < i_iterations; i++)
	{
		float3 displacement = ComputeDisplacement(undispPos, minSlice, baseScale);
		float2 error = (undispPos + displacement.xz) - i_positionWS.xz;
		undispPos -= error;
	}
	return ComputeDisplacement(undispPos, minSlice, baseScale);
}

#endif // CREST_OCEAN_HELPERS_H
