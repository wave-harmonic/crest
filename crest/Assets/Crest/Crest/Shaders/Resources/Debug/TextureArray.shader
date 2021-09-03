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
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma require 2darray

			#include "UnityCG.cginc"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 uv : TEXCOORD0;
			};

			UNITY_DECLARE_TEX2DARRAY(_MainTex);
			uint _Depth;
			float _Scale;
			float _Bias;

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.uv = float3(input.uv.xy, _Depth);
				return o;
			}

			half4 Frag(Varyings input) : SV_TARGET
			{
				return _Scale * UNITY_SAMPLE_TEX2DARRAY(_MainTex, input.uv) + _Bias;
			}
			ENDCG
		}
	}
}
