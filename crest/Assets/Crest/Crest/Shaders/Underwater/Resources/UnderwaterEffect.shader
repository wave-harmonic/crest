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
	#pragma multi_compile_local __ _DEBUG_VIEW_OCEAN_MASK
	#pragma multi_compile_local __ _DEBUG_VIEW_STENCIL

	#include "UnityCG.cginc"
	#include "Lighting.cginc"

	#include "../../Helpers/BIRP/Core.hlsl"
	#include "../../Helpers/BIRP/InputsDriven.hlsl"
	#include "../../FullScreenTriangle.hlsl"
	#include "../../Helpers/BIRP/Lighting.hlsl"

	// Variable downstream as URP XR has issues.
	#define _CameraForward unity_CameraToWorld._m02_m12_m22

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
			// Both "__" and "_FULL_SCREEN_EFFECT" are fullscreen triangles. The latter only denotes an optimisation of
			// whether to skip the horizon calculation.
			#pragma multi_compile_local __ _FULL_SCREEN_EFFECT

			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}

		Pass
		{
			// Only adds fog to the front face and in effect anything behind it.
			Name "Volume: Front Face (2D)"
			Cull Back
			ZTest LEqual

			HLSLPROGRAM
			#define CREST_WATER_VOLUME 1
			#define CREST_WATER_VOLUME_FRONT_FACE 1
			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}

		Pass
		{
			// Only adds fog to the front face and in effect anything behind it.
			Name "Volume: Front Face (3D)"
			Cull Back
			ZTest LEqual

			HLSLPROGRAM
			#define CREST_WATER_VOLUME 1
			#define CREST_WATER_VOLUME_HAS_BACKFACE 1
			#define CREST_WATER_VOLUME_FRONT_FACE 1
			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}

		Pass
		{
			// Only adds fog to the front face and in effect anything behind it.
			Name "Volume: Front Face (Fly-Through)"
			Cull Back
			ZTest LEqual

			Stencil
			{
				// Must match k_StencilValueVolume in:
				// Scripts/Underwater/UnderwaterRenderer.Mask.cs
				Ref 5
				Comp Always
				Pass Replace
				ZFail IncrSat
			}

			HLSLPROGRAM
			#define CREST_WATER_VOLUME 1
			#define CREST_WATER_VOLUME_HAS_BACKFACE 1
			#define CREST_WATER_VOLUME_FRONT_FACE 1
			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}

		Pass
		{
			// Back face will only render if view is within the volume and there is no scene in front. It will only add
			// fog to the back face (and in effect anything behind it). No caustics.
			Name "Volume: Back Face"
			Cull Front
			ZTest LEqual

			Stencil
			{
				// Must match k_StencilValueVolume in:
				// Scripts/Underwater/UnderwaterRenderer.Mask.cs
				Ref 5
				Comp NotEqual
				Pass Replace
				ZFail IncrSat
			}

			HLSLPROGRAM
			#define CREST_WATER_VOLUME 1
			#define CREST_WATER_VOLUME_BACK_FACE 1
			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}

		Pass
		{
			// When inside a volume, this pass will render to the scene within the volume.
			Name "Volume: Scene (Full Screen)"
			Cull Back
			ZTest Always
			Stencil
			{
				// We want to render over the scene that's inside the volume, but not over already fogged areas. It will
				// handle all of the scene within the geometry once the camera is within the volume.
				// 0 = Outside of geometry as neither face passes have touched it.
				// 1 = Only back face z failed which means scene is in front of back face but not front face.
				// 2 = Both front and back face z failed which means outside geometry.
				Ref 1
				Comp Equal
				Pass Replace
			}

			HLSLPROGRAM
			#define CREST_WATER_VOLUME_FULL_SCREEN 1
			#include "../UnderwaterEffect.hlsl"
			ENDHLSL
		}
	}
}
