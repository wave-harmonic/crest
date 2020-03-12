// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders the geometry to the clip surface texture.

Shader "Crest/Inputs/Clip Surface/Remove Area"
{
	Properties
	{
		[Toggle] _Invert("Invert", Float) = 0
	}

	SubShader
	{
		Pass
		{
			Blend Off
			ZWrite Off
			ColorMask R

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma shader_feature _INVERT_ON

			#include "UnityCG.cginc"

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

			half Frag(Varyings input) : SV_Target
			{
#if _INVERT_ON
				return 0.0;
#else
				return 1.0;
#endif
			}
			ENDCG
		}
	}
}
