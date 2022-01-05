// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#include "../ShaderLibrary/Common.hlsl"

#include "../OceanGlobals.hlsl"
#include "../OceanInputsDriven.hlsl"
#include "../OceanShaderData.hlsl"
#include "../OceanHelpersNew.hlsl"
#include "../OceanShaderHelpers.hlsl"
#include "../OceanEmission.hlsl"

#include "../../Helpers/WaterVolume.hlsl"

TEXTURE2D_X(_CrestCameraColorTexture);
TEXTURE2D_X(_CrestOceanMaskTexture);
TEXTURE2D_X(_CrestOceanMaskDepthTexture);

#include "UnderwaterEffectShared.hlsl"

struct Attributes
{
#if CREST_WATER_VOLUME
	float3 positionOS : POSITION;
#else
	uint id : SV_VertexID;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
#if CREST_WATER_VOLUME
	float4 screenPosition : TEXCOORD0;
#else
	float2 uv : TEXCOORD0;
#endif
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert (Attributes input)
{
	Varyings output;
	ZERO_INITIALIZE(Varyings, output);
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if CREST_WATER_VOLUME
	// Use actual geometry instead of full screen triangle.
	output.positionCS = TransformObjectToHClip(input.positionOS);
	output.screenPosition = ComputeScreenPos(output.positionCS);
#else
	output.positionCS = GetFullScreenTriangleVertexPosition(input.id);
	output.uv = GetFullScreenTriangleTexCoord(input.id);
#endif

	return output;
}

real4 Frag (Varyings input) : SV_Target
{
	// We need this when sampling a screenspace texture.
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if CREST_WATER_VOLUME
	float2 uv = input.screenPosition.xy / input.screenPosition.w;
#else
	float2 uv = input.uv;
#endif

	const int2 positionSS = input.positionCS.xy;
	half3 sceneColour = LOAD_TEXTURE2D_X(_CrestCameraColorTexture, positionSS).rgb;
	float rawDepth = LOAD_TEXTURE2D_X(_CameraDepthTexture, positionSS).r;
	const float mask = LOAD_TEXTURE2D_X(_CrestOceanMaskTexture, positionSS).r;
	const float rawOceanDepth = LOAD_TEXTURE2D_X(_CrestOceanMaskDepthTexture, positionSS).r;

#if _DEBUG_VIEW_STENCIL
	return DebugRenderStencil(sceneColour);
#endif

	bool isOceanSurface; bool isUnderwater; float sceneZ;
	GetOceanSurfaceAndUnderwaterData(input.positionCS, positionSS, rawOceanDepth, mask, rawDepth, isOceanSurface, isUnderwater, sceneZ, 0.0);

	float fogDistance = sceneZ;
	float meniscusDepth = 0.0;
#if CREST_WATER_VOLUME
	ApplyWaterVolumeToUnderwaterFogAndMeniscus(input.positionCS, fogDistance, meniscusDepth);
#endif

#if _DEBUG_VIEW_OCEAN_MASK
	return DebugRenderOceanMask(isOceanSurface, isUnderwater, mask, sceneColour);
#endif

	if (isUnderwater)
	{
		// Position needs to be reconstructed in the fragment shader to avoid precision issues as per
		// Unity's lead. Fixes caustics stuttering when far from zero.
		const float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
		const half3 view = normalize(_WorldSpaceCameraPos - positionWS);
		float3 scenePos = _WorldSpaceCameraPos - view * sceneZ / dot(_CameraForward, -view);
		const Light lightMain = GetMainLight();
		const real3 lightDir = lightMain.direction;
		const real3 lightCol = lightMain.color;
		sceneColour = ApplyUnderwaterEffect(positionSS, scenePos, sceneColour, lightCol, lightDir, rawDepth, sceneZ, fogDistance, view, isOceanSurface);
	}

	float wt = ComputeMeniscusWeight(positionSS, mask, _HorizonNormal, meniscusDepth);

	return half4(wt * sceneColour, 1.0);
}
