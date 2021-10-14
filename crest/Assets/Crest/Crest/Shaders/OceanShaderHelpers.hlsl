// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Helpers that will only be used for shaders (eg depth, lighting etc).

#ifndef CREST_OCEAN_SHADER_HELPERS_H
#define CREST_OCEAN_SHADER_HELPERS_H

// Unity does not define these.
#define SAMPLE_DEPTH_TEXTURE_X(textureName, samplerName, coord2) SAMPLE_TEXTURE2D_X(textureName, samplerName, coord2).r
#define LOAD_DEPTH_TEXTURE_X(textureName, coord2) LOAD_TEXTURE2D_X(textureName, coord2).r

// Sample depth macros for all pipelines. Use macros as HDRP depth is a mipchain which can change according to:
// com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl
#if defined(SHADERGRAPH_SAMPLE_SCENE_DEPTH)
#define CREST_SAMPLE_SCENE_DEPTH(uv) SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv)
#else
#define CREST_SAMPLE_SCENE_DEPTH(uv) SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv)
#define CREST_SAMPLE_SCENE_DEPTH_X(uv) SAMPLE_DEPTH_TEXTURE_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv)
#endif

#define CREST_MULTISAMPLE_DEPTH(texture, uv, depth) CrestMultiSampleDepth(texture, sampler##texture, texture##_TexelSize.xy, uv, _CrestDepthTextureOffset, depth)
#define CREST_MULTISAMPLE_SCENE_DEPTH(uv, depth) CREST_MULTISAMPLE_DEPTH(_CameraDepthTexture, uv, depth)
#define CREST_MULTILOAD_DEPTH(texture, uv, depth) CrestMultiLoadDepth(texture, uv, _CrestDepthTextureOffset, depth)
#define CREST_MULTILOAD_SCENE_DEPTH(uv, depth) CREST_MULTILOAD_DEPTH(_CameraDepthTexture, uv, depth)

#if UNITY_REVERSED_Z
#define CREST_DEPTH_COMPARE(depth1, depth2) min(depth1, depth2)
#else
#define CREST_DEPTH_COMPARE(depth1, depth2) max(depth1, depth2)
#endif

// Same as LinearEyeDepth except supports orthographic projection. Use projection keywords to restrict support to either
// of these modes as an optimisation.
float CrestLinearEyeDepth(const float i_rawDepth)
{
#if !defined(_PROJECTION_ORTHOGRAPHIC)
	// Handles UNITY_REVERSED_Z for us.
	float perspective = LinearEyeDepth(i_rawDepth);
#endif // _PROJECTION

#if !defined(_PROJECTION_PERSPECTIVE)
	// Orthographic Depth taken and modified from:
	// https://github.com/keijiro/DepthInverseProjection/blob/master/Assets/InverseProjection/Resources/InverseProjection.shader
	float near = _ProjectionParams.y;
	float far  = _ProjectionParams.z;
	float isOrthographic = unity_OrthoParams.w;

#if defined(UNITY_REVERSED_Z)
	float orthographic = lerp(far, near, i_rawDepth);
#else
	float orthographic = lerp(near, far, i_rawDepth);
#endif // UNITY_REVERSED_Z
#endif // _PROJECTION

#if defined(_PROJECTION_ORTHOGRAPHIC)
	return orthographic;
#elif defined(_PROJECTION_PERSPECTIVE)
	return perspective;
#else
	// If a shader does not have the projection enumeration, then assume they want to support both projection modes.
	return lerp(perspective, orthographic, isOrthographic);
#endif // _PROJECTION
}

// Works for all pipelines.
float CrestMultiSampleDepth
(
	const TEXTURE2D_X(i_texture),
	const SAMPLER(i_sampler),
	const float2 i_texelSize,
	const float2 i_positionNDC,
	const int i_offset,
	const float i_rawDepth
)
{
	float rawDepth = i_rawDepth;

	if (i_offset > 0)
	{
		float2 texelSize = i_texelSize.xy;
		int3 offset = int3(-i_offset, 0, i_offset);
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, SAMPLE_DEPTH_TEXTURE_X(i_texture, i_sampler, i_positionNDC + offset.xy * i_texelSize));
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, SAMPLE_DEPTH_TEXTURE_X(i_texture, i_sampler, i_positionNDC + offset.yx * i_texelSize));
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, SAMPLE_DEPTH_TEXTURE_X(i_texture, i_sampler, i_positionNDC + offset.yz * i_texelSize));
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, SAMPLE_DEPTH_TEXTURE_X(i_texture, i_sampler, i_positionNDC + offset.zy * i_texelSize));
	}

	return rawDepth;
}

float CrestMultiLoadDepth(TEXTURE2D_X(i_texture), const uint2 i_positionSS, const int i_offset, const float i_rawDepth)
{
	float rawDepth = i_rawDepth;

	if (i_offset > 0)
	{
		int3 offset = int3(-i_offset, 0, i_offset);
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, LOAD_DEPTH_TEXTURE_X(i_texture, i_positionSS + offset.xy));
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, LOAD_DEPTH_TEXTURE_X(i_texture, i_positionSS + offset.yx));
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, LOAD_DEPTH_TEXTURE_X(i_texture, i_positionSS + offset.yz));
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, LOAD_DEPTH_TEXTURE_X(i_texture, i_positionSS + offset.zy));
	}

	return rawDepth;
}

#endif // CREST_OCEAN_SHADER_HELPERS_H
