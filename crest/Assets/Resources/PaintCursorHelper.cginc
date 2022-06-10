// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

float3 ProjectToSurface(float2 worldXZ)
{
	float slice0;
	float slice1;
	float lodAlpha;
	PosToSliceIndices(worldXZ, 0.0, _CrestCascadeData[0]._scale, slice0, slice1, lodAlpha);

	const uint si0 = (uint)slice0;
	const uint si1 = si0 + 1;

	const float3 oceanPosScale0 = float3(_CrestCascadeData[si0]._posSnapped, _CrestCascadeData[si0]._scale);
	const float3 oceanPosScale1 = float3(_CrestCascadeData[si1]._posSnapped, _CrestCascadeData[si1]._scale);

	const float4 oceanParams0 = float4(_CrestCascadeData[si0]._texelWidth, _CrestCascadeData[si0]._textureRes, _CrestCascadeData[si0]._weight, _CrestCascadeData[si0]._oneOverTextureRes);
	const float4 oceanParams1 = float4(_CrestCascadeData[si1]._texelWidth, _CrestCascadeData[si1]._textureRes, _CrestCascadeData[si1]._weight, _CrestCascadeData[si1]._oneOverTextureRes);

	float3 displacement = 0.0;

	// Calculate sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
	const float wt_smallerLod = (1. - lodAlpha) * oceanParams0.z;
	const float wt_biggerLod = (1. - wt_smallerLod) * oceanParams1.z;

	CascadeParams cascadeData0 = MakeCascadeParams(oceanPosScale0, oceanParams0);
	CascadeParams cascadeData1 = MakeCascadeParams(oceanPosScale1, oceanParams1);

	half seaLevelOffset = 0.0;
	if (wt_smallerLod > 0.001)
	{
		const float3 uv_slice_smallerLodDisp = WorldToUV(worldXZ, cascadeData0, si0);
		float2 derivs = 0.0;
		float oceanDepth = 0.0;
		SampleSeaDepth(_LD_TexArray_SeaFloorDepth, uv_slice_smallerLodDisp, wt_smallerLod, oceanDepth, seaLevelOffset, cascadeData0, derivs);
	}
	if (wt_biggerLod > 0.001)
	{
		const float3 uv_slice_biggerLodDisp = WorldToUV(worldXZ, cascadeData1, si1);
		float2 derivs = 0.0;
		float oceanDepth = 0.0;
		SampleSeaDepth(_LD_TexArray_SeaFloorDepth, uv_slice_biggerLodDisp, wt_biggerLod, oceanDepth, seaLevelOffset, cascadeData1, derivs);
	}

	displacement.y += seaLevelOffset;

	return displacement + float3(worldXZ.x, _OceanCenterPosWorld.y, worldXZ.y);
}
