// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Flow/Add Flow Map"
{
	Properties
	{
		_FlowMap("Flow Map", 2D) = "white" {}
		_Strength( "Strength", float ) = 1
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" }
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			sampler2D _FlowMap;

			CBUFFER_START(CrestPerOceanInput)
			float4 _FlowMap_ST;
			float _Strength;
			float3 _DisplacementAtInputPosition;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				float3 worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement
				worldPos.xz -= _DisplacementAtInputPosition.xz;
				o.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
				
				o.uv = TRANSFORM_TEX(input.uv, _FlowMap);
				return o;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				return float4((tex2D(_FlowMap, input.uv).xy - 0.5) * _Strength, 0.0, 0.0);
			}

			ENDCG
		}
	}
}
