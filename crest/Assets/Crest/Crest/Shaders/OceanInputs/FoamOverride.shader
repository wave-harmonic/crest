// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders the geometry to the foam texture and sets foam data to provided value.

Shader "Crest/Inputs/Foam/Override Foam"
{
	Properties
	{
		_FoamValue("Foam Value", Range(0.0, 1.0)) = 1.0
	}

	SubShader
	{
		// Base simulation runs on the Geometry queue, before this shader.
		Tags { "Queue" = "Transparent" }

		Pass
		{
			Blend Off
			ZWrite Off
			ColorMask R

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			half _FoamValue;

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

				float3 worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement.
				worldPos.xz -= _DisplacementAtInputPosition.xz;

				o.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return _FoamValue;
			}
			ENDCG
		}
	}
}
