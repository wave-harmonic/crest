// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// This writes straight into the displacement texture and sets the water height to the y value of the geometry.

Shader "Crest/Inputs/Animated Waves/Scale By Factor"
{
	Properties
	{
		[Enum(ColorWriteMask)] _ColorWriteMask("Color Write Mask", Int) = 15
		// Scale the waves. Zero is no waves and one leaves waves untouched.
		_Scale("Scale", Range(0, 1)) = 0.35
		// Feather the edges of the mesh using the texture coordinates. Easiest to understand with a plane.
		[Toggle] _FeatherAtUVExtents("Feather At UV Extents", Float) = 0
		// How far from edge to feather.
		_FeatherWidth("Feather Width", Range(0.001, 0.5)) = 0.1
	}

	SubShader
	{
		Pass
		{
			Blend Zero SrcColor
			ColorMask [_ColorWriteMask]

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma shader_feature_local _FEATHERATUVEXTENTS_ON

			#include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../OceanShaderHelpers.hlsl"

			CBUFFER_START(CrestPerOceanInput)
			float _Weight;
			float3 _DisplacementAtInputPosition;
			float _Scale;
#if _FEATHERATUVEXTENTS_ON
			half _FeatherWidth;
#endif
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
#if _FEATHERATUVEXTENTS_ON
				float2 uv : TEXCOORD0;
#endif
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
#if _FEATHERATUVEXTENTS_ON
				float2 uv : TEXCOORD0;
#endif
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				float3 positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement
				positionWS.xz -= _DisplacementAtInputPosition.xz;
				o.positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));

#if _FEATHERATUVEXTENTS_ON
				o.uv = input.uv;
#endif

				return o;
			}

			half4 Frag( Varyings input ) : SV_Target
			{
				float scale = _Scale;

#if _FEATHERATUVEXTENTS_ON
				scale = lerp(1.0, scale, FeatherWeightFromTextureCoordinates(input.uv, _FeatherWidth));
#endif

				return scale * _Weight;
			}
			ENDCG
		}
	}
}
