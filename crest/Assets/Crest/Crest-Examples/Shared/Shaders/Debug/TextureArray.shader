// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders a specific slice of a 2D Texture Array
// https://docs.unity3d.com/Manual/SL-TextureArrays.html
Shader "Hidden/Crest/Debug/TextureArray"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Off
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma require 2darray

			#include "UnityCG.cginc"

			struct v2f
			{
				float3 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			UNITY_DECLARE_TEX2DARRAY(_MainTex);
			uint _Depth;

			v2f vert (float4 vertex : POSITION, float3 uv : TEXCOORD0)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_P, vertex);
				o.uv = float3(uv.xy, _Depth);
				return o;
			}

			half4 frag (v2f i) : SV_TARGET
			{
				// Brighten textures so they match previous solution (4)
				return UNITY_SAMPLE_TEX2DARRAY(_MainTex, i.uv) * 4;
			}
			ENDCG
		}
	}
}
