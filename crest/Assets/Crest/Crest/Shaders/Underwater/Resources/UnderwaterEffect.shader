// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Underwater/Underwater Effect"
{
	HLSLINCLUDE
	#pragma vertex Vert
	#pragma fragment Frag

	// #pragma enable_d3d11_debug_symbols

	// Use multi_compile because these keywords are copied over from the ocean material. With shader_feature,
	// the keywords would be stripped from builds. Unused shader variants are stripped using a build processor.
	#pragma multi_compile_local __ _SUBSURFACESCATTERING_ON
	#pragma multi_compile_local __ _SUBSURFACESHALLOWCOLOUR_ON
	#pragma multi_compile_local __ _CAUSTICS_ON
	#pragma multi_compile_local __ _SHADOWS_ON
	#pragma multi_compile_local __ _PROJECTION_PERSPECTIVE _PROJECTION_ORTHOGRAPHIC

	#pragma multi_compile_local __ CREST_MENISCUS
	// Both "__" and "_FULL_SCREEN_EFFECT" are fullscreen triangles. The latter only denotes an optimisation of
	// whether to skip the horizon calculation.
	#pragma multi_compile_local __ _FULL_SCREEN_EFFECT CREST_BOUNDARY_2D CREST_BOUNDARY_3D CREST_BOUNDARY_VOLUME
	#pragma multi_compile_local __ _DEBUG_VIEW_OCEAN_MASK
	ENDHLSL

	SubShader
	{
		// These will be "Off" for fullscreen.
		Cull [_CullMode]
		ZTest [_ZTest]
		ZWrite Off

		Pass
		{
			HLSLPROGRAM
			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}
	}
}
