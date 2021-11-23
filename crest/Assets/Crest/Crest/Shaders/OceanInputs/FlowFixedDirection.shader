// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Flow/Fixed Direction"
{
	Properties
	{
		_Speed("Speed", Range(0.0, 10.0)) = 1.0
		_Direction("Direction", Range(0.0, 1.0)) = 0.0
		[Toggle] _FeatherAtUVExtents("Feather At UV Extents", Float) = 0
		_FeatherWidth("Feather Width", Range(0.001, 0.5)) = 0.1
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma shader_feature_local _FEATHERATUVEXTENTS_ON

			#include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanHelpersNew.hlsl"

			CBUFFER_START(CrestPerOceanInput)
			float _Speed;
			float _Direction;
			float3 _DisplacementAtInputPosition;
			half _FeatherWidth;
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
				float2 vel : TEXCOORD0;
#if _FEATHERATUVEXTENTS_ON
				float2 uv : TEXCOORD1;
#endif
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				float3 worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement
				worldPos.xz -= _DisplacementAtInputPosition.xz;
				o.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

				o.vel = _Speed * float2(cos(_Direction * 6.283185), sin(_Direction * 6.283185));

#if _FEATHERATUVEXTENTS_ON
				o.uv = input.uv;
#endif

				return o;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				float2 flow = input.vel;

#if _FEATHERATUVEXTENTS_ON
				flow *= FeatherWeightFromUV(input.uv, _FeatherWidth);
#endif
				return float4(flow, 0.0, 0.0);
			}
			ENDCG
		}
	}
}
