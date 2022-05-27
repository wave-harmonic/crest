// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// NOTE: It is important that everything has a Crest prefix to avoid possible conflicts.

#ifndef CREST_UNDERWATER_EFFECT_INCLUDES_INCLUDED
#define CREST_UNDERWATER_EFFECT_INCLUDES_INCLUDED

// Surface Shader Analysis determines what inputs Unity will generate (ie the magic of surface shaders). But the
// analyzer only understands "DX9 style HLSL syntax" so no constant parameters. SHADER_TARGET_SURFACE_ANALYSIS is
// defined which can be used for workarounds. It was easiest to provide a dummy function for the only "public" function,
// thus excluding all of our other code and includes from the analyzer. Source:
// https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/6a63f93bc1f20ce6cd47f981c7494e8328915621/CGIncludes/HLSLSupport.cginc#L7-L11
#ifdef SHADER_TARGET_SURFACE_ANALYSIS
// Must update signature to match implementation below. Make sure to use everything or will be excluded by compiler. Use
// "+=" instead of "=" or color will come out incorrectly.
bool CrestApplyUnderwaterFog(float2 b, float3 c, float d, float e, inout half3 a) { a.rgb += b.x + b.y + c.x + c.y + c.z + d + e; return false; }
#else // SHADER_TARGET_SURFACE_ANALYSIS

UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture);
UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestWaterVolumeFrontFaceTexture);
UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestWaterVolumeBackFaceTexture);

half3 _CrestDiffuse;
half3 _CrestDiffuseGrazing;

#if CREST_SHADOWS_ON
half3 _CrestDiffuseShadow;
#endif

#if CREST_SUBSURFACESCATTERING_ON
half3 _CrestSubSurfaceColour;
half _CrestSubSurfaceBase;
half _CrestSubSurfaceSun;
half _CrestSubSurfaceSunFallOff;
#endif

half3 _CrestAmbientLighting;
half4 _CrestDepthFogDensity;

#include "../OceanConstants.hlsl"
#include "../OceanGlobals.hlsl"
#include "../OceanInputsDriven.hlsl"
#include "../OceanShaderHelpers.hlsl"
#include "../OceanLightingHelpers.hlsl"

half3 CrestScatterColour
(
	const half3 i_ambientLighting,
	const half3 i_lightCol,
	const half3 i_lightDir,
	const half3 i_view,
	const float i_shadow
)
{
	// Base colour.
	float v = abs(i_view.y);
	half3 col = lerp(_CrestDiffuse, _CrestDiffuseGrazing, 1. - pow(v, 1.0));

#if CREST_SHADOWS_ON
	col = lerp(_CrestDiffuseShadow, col, i_shadow);
#endif

#if CREST_SUBSURFACESCATTERING_ON
	{
		col *= i_ambientLighting;

		// Approximate subsurface scattering - add light when surface faces viewer. Use geometry normal - don't need high freqs.
		half towardsSun = pow(max(0., dot(i_lightDir, -i_view)), _CrestSubSurfaceSunFallOff);
		half3 subsurface = (_CrestSubSurfaceBase + _CrestSubSurfaceSun * towardsSun) * _CrestSubSurfaceColour.rgb * i_lightCol * i_shadow;
		col += subsurface;
	}
#endif // CREST_SUBSURFACESCATTERING_ON

	return col;
}

// Taken from: OceanHelpersNew.hlsl
float3 CrestWorldToUV(in float2 i_samplePos, in CascadeParams i_cascadeParams, in float i_sliceIndex)
{
	float2 uv = (i_samplePos - i_cascadeParams._posSnapped) / (i_cascadeParams._texelWidth * i_cascadeParams._textureRes) + 0.5;
	return float3(uv, i_sliceIndex);
}

bool CrestApplyUnderwaterFog(const float2 positionNDC, const float3 positionWS, float deviceDepth, half multiplier, inout half3 color)
{
#if CREST_WATER_VOLUME
	// No fog before volume.
	float rawFrontFaceZ = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestWaterVolumeFrontFaceTexture, positionNDC).r;
	if (rawFrontFaceZ > 0.0 && rawFrontFaceZ < deviceDepth)
	{
		return false;
	}

#if CREST_WATER_VOLUME_2D
	// No fog if plane is not in view. If we wanted to be consistent with the underwater shader, we should also check
	// this for non fly-through volumes too, but being inside a non fly-through volume is undefined behaviour so we can
	// save a variant.
	if (rawFrontFaceZ == 0.0)
	{
		return false;
	}
#endif // CREST_WATER_VOLUME_2D
#endif // CREST_WATER_VOLUME

	half mask = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, positionNDC).r;
	if (mask >= CREST_MASK_NO_FOG)
	{
		return false;
	}

	half3 lightColor = _LightColor0.rgb;
	float3 lightDirection = WaveHarmonic::Crest::WorldSpaceLightDir(positionWS);
	half3 view =  normalize(_WorldSpaceCameraPos - positionWS);

	// Get the largest distance.
	float rawFogDistance = deviceDepth;
#if CREST_WATER_VOLUME_HAS_BACKFACE
	// Use the closest of the two.
	float rawBackFaceZ = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestWaterVolumeBackFaceTexture, positionNDC).r;
	rawFogDistance = max(rawFogDistance, rawBackFaceZ);
#endif

	float fogDistance = CrestLinearEyeDepth(rawFogDistance);

#if CREST_WATER_VOLUME
#if CREST_WATER_VOLUME_HAS_BACKFACE
	if (rawFrontFaceZ > 0.0)
#endif
	{
		fogDistance -= CrestLinearEyeDepth(rawFrontFaceZ);
	}
#endif

	half shadow = 1.0;
#if CREST_SHADOWS_ON
	{
		// Offset slice so that we do not get high frequency detail. But never use last lod as this has crossfading.
		int sliceIndex = clamp(_CrestDataSliceOffset, 0, _SliceCount - 2);
		const float3 uv = CrestWorldToUV(_WorldSpaceCameraPos.xz, _CrestCascadeData[sliceIndex], sliceIndex);
		// Camera should be at center of LOD system so no need for blending (alpha, weights, etc).
		shadow = _LD_TexArray_Shadow.SampleLevel(LODData_linear_clamp_sampler, uv, 0.0).x;
		shadow = saturate(1.0 - shadow);
	}
#endif // CREST_SHADOWS_ON

	half3 scatterColor = CrestScatterColour
	(
		_CrestAmbientLighting,
		lightColor,
		lightDirection,
		view,
		shadow
	);

	color = lerp(color, scatterColor, saturate(1.0 - exp(-_CrestDepthFogDensity.xyz * fogDistance)) * multiplier);
	return true;
}

#endif // !SHADER_TARGET_SURFACE_ANALYSIS
#endif // CREST_UNDERWATER_EFFECT_INCLUDES_INCLUDED
