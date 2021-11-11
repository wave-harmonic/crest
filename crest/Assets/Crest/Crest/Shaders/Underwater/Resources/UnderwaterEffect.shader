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
	#pragma multi_compile_local __ _FULL_SCREEN_EFFECT
	#pragma multi_compile_local __ _DEBUG_VIEW_OCEAN_MASK
	#pragma multi_compile_local __ _DEBUG_VIEW_STENCIL
	ENDHLSL

	SubShader
	{
		ZWrite Off

		Pass
		{
			Name "Full Screen"
			Cull Off
			ZTest Always

			HLSLPROGRAM
			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}

		Pass
		{
			//
			Name "Boundary: Front Faces"
			Cull Back
			ZTest LEqual

			Stencil
			{
				Ref 5
				Comp Always
				Pass Replace
				ZFail IncrSat
			}

			HLSLPROGRAM
			#define CREST_BOUNDARY 1
			#define CREST_BOUNDARY_HAS_BACKFACE 1
			#define CREST_BOUNDARY_FRONT_FACE 1
			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}

		Pass
		{
			// Back face will only render if view is within the volume and there is no scene in front.
			Name "Boundary: Back Faces"
			Cull Front
			ZTest LEqual

			Stencil
			{
				Ref 5
				Comp NotEqual
				Pass Replace
				ZFail IncrSat
			}

			HLSLPROGRAM
			#define CREST_BOUNDARY 1
			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}

		Pass
		{
			// When inside a volume, this pass will render to the scene within the volume.
			Name "Boundary: Full Screen"
			Cull Back
			ZTest Always
			Stencil
			{
				Ref 1
				Comp Equal
				Pass Replace
			}

			HLSLPROGRAM
			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}
	}
}
