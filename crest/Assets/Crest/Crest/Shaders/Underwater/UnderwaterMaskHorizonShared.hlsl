// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders the ocean horizon line into the mask.

#ifndef CREST_UNDERWATER_MASK_HORIZON_SHARED_INCLUDED
#define CREST_UNDERWATER_MASK_HORIZON_SHARED_INCLUDED

#include "../OceanConstants.hlsl"
#include "../OceanGlobals.hlsl"

// Driven by scripting. It is a non-linear converted from a linear 0-1 value.
float _FarPlaneOffset;

struct Attributes
{
	uint id : SV_VertexID;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes input)
{
	// This will work for all pipelines.
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	output.positionCS = GetFullScreenTriangleVertexPosition(input.id, _FarPlaneOffset);
	output.uv = GetFullScreenTriangleTexCoord(input.id);

	return output;
}

half4 Frag(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	float3 positionWS = ComputeWorldSpacePosition(input.uv, _FarPlaneOffset, UNITY_MATRIX_I_VP);
	return (half4) positionWS.y > _OceanCenterPosWorld.y
		? UNDERWATER_MASK_ABOVE_SURFACE
		: UNDERWATER_MASK_BELOW_SURFACE;
}

#endif // CREST_UNDERWATER_MASK_HORIZON_SHARED_INCLUDED
