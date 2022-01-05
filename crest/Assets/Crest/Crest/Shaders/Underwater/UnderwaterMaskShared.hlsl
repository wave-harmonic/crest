// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_UNDERWATER_MASK_SHARED_INCLUDED
#define CREST_UNDERWATER_MASK_SHARED_INCLUDED

#include "../OceanConstants.hlsl"
#include "../OceanInputsDriven.hlsl"
#include "../OceanGlobals.hlsl"
#include "../OceanHelpersNew.hlsl"
#include "../OceanVertHelpers.hlsl"
#include "../OceanShaderHelpers.hlsl"

#include "../Helpers/WaterVolume.hlsl"

struct Attributes
{
	// The old unity macros require this name and type.
	float4 vertex : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	UNITY_VERTEX_OUTPUT_STEREO
};

// Hack - due to SV_IsFrontFace occasionally coming through as true for backfaces,
// add a param here that forces ocean to be in undrwater state. I think the root
// cause here might be imprecision or numerical issues at ocean tile boundaries, although
// i'm not sure why cracks are not visible in this case.
float _ForceUnderwater;

Varyings Vert(Attributes v)
{
	// This will work for all pipelines.
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	const CascadeParams cascadeData0 = _CrestCascadeData[_LD_SliceIndex];
	const CascadeParams cascadeData1 = _CrestCascadeData[_LD_SliceIndex + 1];
	const PerCascadeInstanceData instanceData = _CrestPerCascadeInstanceData[_LD_SliceIndex];

	float3 worldPos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1.0)).xyz;

#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
	worldPos.xz += _WorldSpaceCameraPos.xz;
#endif

	// Vertex snapping and lod transition
	float lodAlpha;
	const float meshScaleLerp = instanceData._meshScaleLerp;
	const float gridSize = instanceData._geoGridWidth;
	SnapAndTransitionVertLayout(meshScaleLerp, cascadeData0, gridSize, worldPos, lodAlpha);

	{
		// Scale up by small "epsilon" to solve numerical issues. Expand slightly about tile center.
		// :OceanGridPrecisionErrors
		float2 tileCenterXZ = UNITY_MATRIX_M._m03_m23;
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
		tileCenterXZ += _WorldSpaceCameraPos.xz;
#endif
		const float2 cameraPositionXZ = abs(_WorldSpaceCameraPos.xz);
		// Scale "epsilon" by distance from zero. There is an issue where overlaps can cause SV_IsFrontFace
		// to be flipped (needs to be investigated). Gaps look bad from above surface, and overlaps look bad
		// from below surface. We want to close gaps without introducing overlaps. A fixed "epsilon" will
		// either not solve gaps at large distances or introduce too many overlaps at small distances. Even
		// with scaling, there are still unsolvable overlaps underwater (especially at large distances).
		// 100,000 (0.00001) is the maximum position before Unity warns the user of precision issues.
		worldPos.xz = lerp(tileCenterXZ, worldPos.xz, lerp(1.0, 1.01, max(cameraPositionXZ.x, cameraPositionXZ.y) * 0.00001));
	}

	// Calculate sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
	const float wt_smallerLod = (1.0 - lodAlpha) * cascadeData0._weight;
	const float wt_biggerLod = (1.0 - wt_smallerLod) * cascadeData1._weight;
	// Sample displacement textures, add results to current world pos / normal / foam
	const float2 positionWS_XZ_before = worldPos.xz;

	// Data that needs to be sampled at the undisplaced position
	if (wt_smallerLod > 0.001)
	{
		half sss = 0.0;
		const float3 uv_slice_smallerLod = WorldToUV(positionWS_XZ_before, cascadeData0, _LD_SliceIndex);
		SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice_smallerLod, wt_smallerLod, worldPos, sss);
	}
	if (wt_biggerLod > 0.001)
	{
		half sss = 0.0;
		const float3 uv_slice_biggerLod = WorldToUV(positionWS_XZ_before, cascadeData1, _LD_SliceIndex + 1);
		SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice_biggerLod, wt_biggerLod, worldPos, sss);
	}

	// Data that needs to be sampled at the displaced position.
	half seaLevelOffset = 0.0;
	if (wt_smallerLod > 0.0001)
	{
		const float3 uv_slice_smallerLodDisp = WorldToUV(worldPos.xz, cascadeData0, _LD_SliceIndex);
		SampleSeaLevelOffset(_LD_TexArray_SeaFloorDepth, uv_slice_smallerLodDisp, wt_smallerLod, seaLevelOffset);
	}
	if (wt_biggerLod > 0.0001)
	{
		const float3 uv_slice_biggerLodDisp = WorldToUV(worldPos.xz, cascadeData1, _LD_SliceIndex + 1);
		SampleSeaLevelOffset(_LD_TexArray_SeaFloorDepth, uv_slice_biggerLodDisp, wt_biggerLod, seaLevelOffset);
	}

	worldPos.y += seaLevelOffset;

#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
	worldPos.xz -= _WorldSpaceCameraPos.xz;
#endif

	output.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

	return output;
}

half4 Frag(const Varyings input, const bool i_isFrontFace : SV_IsFrontFace) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if CREST_WATER_VOLUME
	ApplyVolumeToOceanMask(input.positionCS);
#endif

	// @MSAAOutlineFix:
	// The edge of the ocean surface at the near plane will be MSAA'd leaving a noticeable edge. By rendering the mask
	// with a slightly further near plane, it exposes the edge to having the underwater fog applied which is much nicer.
	if (_CrestDepthTextureOffset > 0 && CrestLinearEyeDepth(input.positionCS.z) < (_ProjectionParams.y + 0.001))
	{
		discard;
	}

	if (IsUnderwater(i_isFrontFace, _ForceUnderwater))
	{
		return (half4)UNDERWATER_MASK_BELOW_SURFACE;
	}
	else
	{
		return (half4)UNDERWATER_MASK_ABOVE_SURFACE;
	}
}

#endif // CREST_UNDERWATER_MASK_SHARED_INCLUDED
