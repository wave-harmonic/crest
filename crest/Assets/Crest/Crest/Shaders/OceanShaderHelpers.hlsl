// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Helpers that will only be used when rendering to a screen (eg depth, lighting etc).

#ifndef CREST_OCEAN_SHADER_HELPERS_H
#define CREST_OCEAN_SHADER_HELPERS_H

float CrestLinearEyeDepth(const float i_rawDepth)
{
#if defined(_PROJECTION_BOTH) || defined(_PROJECTION_PERSPECTIVE)
	// Handles UNITY_REVERSED_Z for us.
	float perspective = LinearEyeDepth(i_rawDepth);
#endif // _PROJECTION

#if defined(_PROJECTION_BOTH) || defined(_PROJECTION_ORTHOGRAPHIC)
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

#if defined(_PROJECTION_BOTH)
	return lerp(perspective, orthographic, isOrthographic);
#elif defined(_PROJECTION_ORTHOGRAPHIC)
	return orthographic;
#elif defined(_PROJECTION_PERSPECTIVE)
	return perspective;
#else
	// If a shader does not have the projection enumeration, then assume they want perspective.
	return LinearEyeDepth(i_rawDepth);
#endif // _PROJECTION
}

#endif // CREST_OCEAN_SHADER_HELPERS_H
