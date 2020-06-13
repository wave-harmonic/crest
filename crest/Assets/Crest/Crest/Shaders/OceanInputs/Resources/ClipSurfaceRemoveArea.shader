// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders the geometry to the clip surface texture.

Shader "Crest/Inputs/Clip Surface/Remove Area"
{
	SubShader
	{
		Tags { "Queue" = "Geometry" }

		Pass
		{
			Blend Off
			ZWrite Off
			ColorMask R

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			CBUFFER_START(CrestPerOceanInput)
			float3 _DisplacementAtInputPosition;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				float3 positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement
				positionWS.xz -= _DisplacementAtInputPosition.xz;
				o.positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return 1.0;
			}
			ENDCG
		}
	}
}
