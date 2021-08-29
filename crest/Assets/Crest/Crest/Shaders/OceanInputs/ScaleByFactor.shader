// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// 0-1 scaling of existing ocean data using multiplicative blending.

Shader "Crest/Inputs/All/Scale By Factor"
{
	Properties
	{
		// Scale the ocean data. Zero is no data and one leaves data untouched.
		_Scale("Scale", Range(0, 1)) = 0.35

		// Use the texture instead of the scale value.
		[Toggle] _Texture("Apply Texture", Float) = 0
		_MainTex("Texture", 2D) = "black" {}

		// Inverts the scale value.
		[Toggle] _Invert("Invert", Float) = 0

		[Header(Feather)]
		// Feather the edges of the mesh using the texture coordinates. Easiest to understand with a plane.
		[Toggle] _FeatherAtUVExtents("Feather At UV Extents", Float) = 0
		// How far from edge to feather.
		_FeatherWidth("Feather Width", Range(0.001, 0.5)) = 0.1
	}

	SubShader
	{
		Pass
		{
			// Multiply
			Blend Zero SrcColor

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma shader_feature_local _TEXTURE_ON
			#pragma shader_feature_local _INVERT_ON
			#pragma shader_feature_local _FEATHERATUVEXTENTS_ON

			#include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanHelpersNew.hlsl"

#if defined(_TEXTURE_ON) || defined(_FEATHERATUVEXTENTS_ON)
#define _NEED_UVS
#endif

#if _TEXTURE_ON
			sampler2D _MainTex;
#endif

			CBUFFER_START(CrestPerOceanInput)
			float _Weight;
			float3 _DisplacementAtInputPosition;
			float _Scale;
#if _FEATHERATUVEXTENTS_ON
			half _FeatherWidth;
#endif
#if _TEXTURE_ON
			float4 _MainTex_ST;
#endif
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
#ifdef _NEED_UVS
				float2 uv : TEXCOORD0;
#endif
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
#ifdef _NEED_UVS
				float2 uv : TEXCOORD0;
#endif
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				float3 positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement.
				positionWS.xz -= _DisplacementAtInputPosition.xz;
				o.positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));

#ifdef _NEED_UVS
				o.uv = input.uv;
#endif

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
#if _TEXTURE_ON
				float scale = tex2D(_MainTex, input.uv).r;
#else
				float scale = _Scale;
#endif

#if _INVERT_ON
				scale = 1.0 - scale;
#endif

#if _FEATHERATUVEXTENTS_ON
				scale = lerp(1.0, scale, FeatherWeightFromUV(input.uv, _FeatherWidth));
#endif

				return scale * _Weight;
			}
			ENDCG
		}
	}
}
