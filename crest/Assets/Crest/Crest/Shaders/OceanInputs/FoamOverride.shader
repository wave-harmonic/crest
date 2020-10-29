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
				o.positionCS = UnityObjectToClipPos(input.positionOS);
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
