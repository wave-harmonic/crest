// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Clip Surface"
{
 	SubShader
	{
 		Pass
		{
			Blend Off
			ColorMask G

 			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

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

 			half4 Frag(Varyings input) : SV_Target
			{
				return half4(0.0, 1.0, 0.0, 0.0);
			}
			ENDCG
		}
	}
}
