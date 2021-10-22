// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Hidden/Water Boundary Geometry"
{
	SubShader
	{
		CGINCLUDE
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
#if UNITY_UV_STARTS_AT_TOP
			// Flip the geometry otherwise it will be flipped when sampling. Only had to since using temporary RTs :\
			o.positionCS.y *= -1.0;
#endif
			return o;
		}

		half4 Frag(Varyings input) : SV_Target
		{
			return 1.0;
		}
		ENDCG

		Pass
		{
			Name "Boundary Front Faces"
			Cull Back

			CGPROGRAM
			ENDCG
		}

		Pass
		{
			Name "Boundary Back Faces"
			Cull Front

			CGPROGRAM
			ENDCG
		}
	}
}
