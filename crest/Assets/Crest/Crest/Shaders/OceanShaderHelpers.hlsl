// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Helpers that will only be used for shaders (eg depth, lighting etc).

#ifndef CREST_OCEAN_SHADER_HELPERS_H
#define CREST_OCEAN_SHADER_HELPERS_H

// Sample depth macros for all pipelines. Use macros as HDRP depth is a mipchain which can change according to:
// com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl
#if defined(SHADERGRAPH_SAMPLE_SCENE_DEPTH)
#define CREST_SAMPLE_SCENE_DEPTH(coordinates) SHADERGRAPH_SAMPLE_SCENE_DEPTH(coordinates)
#elif defined(TEXTURE2D_X)
#define CREST_SAMPLE_SCENE_DEPTH(coordinates) SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, coordinates)
#elif defined(SAMPLE_DEPTH_TEXTURE)
#define CREST_SAMPLE_SCENE_DEPTH(coordinates) SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, coordinates)
#endif

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
float CrestMultiSampleSceneDepth(const float i_rawDepth, const float2 i_positionNDC)
{
	float rawDepth = i_rawDepth;

	if (_CrestDepthTextureOffset > 0)
	{
		// We could use screen size instead.
		float2 texelSize = _CameraDepthTexture_TexelSize.xy;
		int3 offset = int3(-_CrestDepthTextureOffset, 0, _CrestDepthTextureOffset);

		rawDepth = CREST_DEPTH_COMPARE(rawDepth, CREST_SAMPLE_SCENE_DEPTH(i_positionNDC + offset.xy * texelSize));
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, CREST_SAMPLE_SCENE_DEPTH(i_positionNDC + offset.yx * texelSize));
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, CREST_SAMPLE_SCENE_DEPTH(i_positionNDC + offset.yz * texelSize));
		rawDepth = CREST_DEPTH_COMPARE(rawDepth, CREST_SAMPLE_SCENE_DEPTH(i_positionNDC + offset.zy * texelSize));
	}

	return rawDepth;
}

#endif // CREST_OCEAN_SHADER_HELPERS_H
