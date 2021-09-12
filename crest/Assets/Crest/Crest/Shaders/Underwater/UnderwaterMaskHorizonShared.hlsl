// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders the ocean horizon line into the mask.

#ifndef CREST_UNDERWATER_MASK_HORIZON_SHARED_INCLUDED
#define CREST_UNDERWATER_MASK_HORIZON_SHARED_INCLUDED

#include "../OceanConstants.hlsl"
#include "../OceanGlobals.hlsl"
#include "../FullScreenTriangle.hlsl"

// Driven by scripting. It is a non-linear converted from a linear 0-1 value.
float _FarPlaneOffset;
float4x4 _InvViewProjection;
float4x4 _InvViewProjectionRight;

struct Attributes
{
	uint id : SV_VertexID;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float3 positionWS : TEXCOORD0;
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes input)
{
	// This will work for all pipelines.
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	output.positionCS = GetFullScreenTriangleVertexPosition(input.id, _FarPlaneOffset);
	float2 uv = GetFullScreenTriangleTexCoord(input.id);

	const float2 pixelCS = uv * 2.0 - float2(1.0, 1.0);
#if CREST_HANDLE_XR
	const float4x4 InvViewProjection = unity_StereoEyeIndex == 0 ? _InvViewProjection : _InvViewProjectionRight;
#else
	const float4x4 InvViewProjection = _InvViewProjection;
#endif
	const float4 pixelWS_H = mul(InvViewProjection, float4(pixelCS, _FarPlaneOffset, 1.0));
	const float3 pixelWS = pixelWS_H.xyz / pixelWS_H.w;

	output.positionWS = pixelWS;

	return output;
}

half4 Frag(Varyings input) : SV_Target
{
	return (half4) input.positionWS.y > _OceanCenterPosWorld.y
		? UNDERWATER_MASK_ABOVE_SURFACE
		: UNDERWATER_MASK_BELOW_SURFACE;
}

#endif // CREST_UNDERWATER_MASK_HORIZON_SHARED_INCLUDED
