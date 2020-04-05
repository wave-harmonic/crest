// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Ocean LOD data - data, samplers and functions associated with LODs

// Conversions for world space from/to UV space. All these should *not* be clamped otherwise they'll break fullscreen triangles.
float2 LD_WorldToUV(in float2 i_samplePos, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return (i_samplePos - i_centerPos) / (i_texelSize * i_res) + 0.5;
}

float3 WorldToUV(in float2 i_samplePos, in uint i_sliceIndex) {
	const float2 result = LD_WorldToUV(
		i_samplePos,
		_LD_Pos_Scale[i_sliceIndex].xy,
		_LD_Params[i_sliceIndex].y,
		_LD_Params[i_sliceIndex].x
	);
	return float3(result, i_sliceIndex);
}

float3 WorldToUV_BiggerLod(in float2 i_samplePos, in uint i_sliceIndex_BiggerLod) {
	const float2 result = LD_WorldToUV(
		i_samplePos, _LD_Pos_Scale[i_sliceIndex_BiggerLod].xy,
		_LD_Params[i_sliceIndex_BiggerLod].y,
		_LD_Params[i_sliceIndex_BiggerLod].x
	);
	return float3(result, i_sliceIndex_BiggerLod);
}

float3 WorldToUV_Source(in float2 i_samplePos, in uint i_sliceIndex_Source) {
	const float2 result = LD_WorldToUV(
		i_samplePos,
		_LD_Pos_Scale_Source[i_sliceIndex_Source].xy,
		_LD_Params_Source[i_sliceIndex_Source].y,
		_LD_Params_Source[i_sliceIndex_Source].x
	);
	return float3(result, i_sliceIndex_Source);
}


float2 LD_UVToWorld(in float2 i_uv, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return i_texelSize * i_res * (i_uv - 0.5) + i_centerPos;
}

float2 UVToWorld(in float2 i_uv, in float i_sliceIndex) { return LD_UVToWorld(i_uv, _LD_Pos_Scale[i_sliceIndex].xy, _LD_Params[i_sliceIndex].y, _LD_Params[i_sliceIndex].x); }

// Shortcuts if _LD_SliceIndex is set
float3 WorldToUV(in float2 i_samplePos) { return WorldToUV(i_samplePos, _LD_SliceIndex); }
float3 WorldToUV_BiggerLod(in float2 i_samplePos) { return WorldToUV_BiggerLod(i_samplePos, _LD_SliceIndex + 1); }
float2 UVToWorld(in float2 i_uv) { return UVToWorld(i_uv, _LD_SliceIndex); }
