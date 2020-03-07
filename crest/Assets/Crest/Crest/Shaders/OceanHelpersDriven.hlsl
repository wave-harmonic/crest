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

	float lodAlpha = ComputeLodAlpha(i_positionWS, _InstanceData.x, _LD_Pos_Scale[0]);

	// Sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
	float wt_smallerLod = (1.0 - lodAlpha) * _LD_Params[_LD_SliceIndex].z;
	float wt_biggerLod = (1.0 - wt_smallerLod) * _LD_Params[_LD_SliceIndex + 1].z;

	// Sample data textures
	half4 result = 0.0;
	if (wt_smallerLod > 0.001)
	{
		float3 uv_slice = WorldToUV(i_positionWS.xz, _LD_Pos_Scale[_LD_SliceIndex], _LD_Params[_LD_SliceIndex], _LD_SliceIndex);
		result += wt_smallerLod * i_oceanData.SampleLevel(LODData_linear_clamp_sampler, uv_slice, 0.0);
	}
	if (wt_biggerLod > 0.001)
	{
		float3 uv_slice = WorldToUV(i_positionWS.xz, _LD_Pos_Scale[_LD_SliceIndex + 1], _LD_Params[_LD_SliceIndex + 1], _LD_SliceIndex + 1);
		result += wt_biggerLod * i_oceanData.SampleLevel(LODData_linear_clamp_sampler, uv_slice, 0.0);
	}

	return result;
}

float3 SampleOceanDataAtWorldPosition
(
	in const Texture2DArray i_oceanData,
	in const float2 i_positionXZWS,
	in const float i_minSlice
)
{
	uint slice0, slice1;
	float lodAlpha;
	PosToSliceIndices(i_positionXZWS, _MeshScaleLerp, i_minSlice, slice0, slice1, lodAlpha);

	const float wt_smallerLod = (1. - lodAlpha) * _LD_Params[slice0].z;
	const float wt_biggerLod = (1. - wt_smallerLod) * _LD_Params[slice1].z;

	float3 result = 0.0;

	if (wt_smallerLod > 0.001)
	{
		float3 uv_slice = WorldToUV(i_positionXZWS, _LD_Pos_Scale[slice0], _LD_Params[slice0], slice0);
		result += wt_smallerLod * i_oceanData.SampleLevel(LODData_linear_clamp_sampler, uv_slice, 0).xyz;
	}
	if (wt_biggerLod > 0.001)
	{
		float3 uv_slice = WorldToUV(i_positionXZWS, _LD_Pos_Scale[slice1], _LD_Params[slice1], slice1);
		result += wt_biggerLod * i_oceanData.SampleLevel(LODData_linear_clamp_sampler, uv_slice, 0).xyz;
	}

	return result;
}

float3 SampleOceanDataAtWorldPosition
(
	in const Texture2DArray i_oceanData,
	in const float3 i_positionWS,
	in const float i_minSlice,
	in const uint i_iterations
)
{
	const float2 queryPositionXZ = i_positionWS.xz;
	const float minGridSize = i_minSlice;

	const float gridSizeSlice0 = _LD_Params[0].x;
	const float minSlice = clamp(floor(log2(minGridSize / gridSizeSlice0)), 0.0, _SliceCount - 1.0);

	float2 undisplacedPosition = queryPositionXZ;
	for (uint i = 0; i < i_iterations; i++)
	{
		const float3 displacement = SampleOceanDataAtWorldPosition(i_oceanData, undisplacedPosition, minSlice);
		const float2 error = (undisplacedPosition + displacement.xz) - queryPositionXZ;
		undisplacedPosition -= error;
	}

	return SampleOceanDataAtWorldPosition(i_oceanData, undisplacedPosition, minSlice);
}

#endif // CREST_OCEAN_HELPERS_H