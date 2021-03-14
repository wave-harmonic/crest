// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Foam/Add From Vert Colours"
{
	Properties
	{
		_Strength("Strength", float) = 1
	}

	SubShader
	{
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			CBUFFER_START(CrestPerOceanInput)
			float _Strength;
			float3 _DisplacementAtInputPosition;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
				float4 col : COLOR0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 col : COLOR0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				float3 worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement
				worldPos.xz -= _DisplacementAtInputPosition.xz;
				o.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
				
				o.col = input.col;

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return _Strength * input.col.x;
			}
			ENDCG
		}
	}
}
