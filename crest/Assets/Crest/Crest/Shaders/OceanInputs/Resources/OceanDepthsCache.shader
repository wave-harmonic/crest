﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Draw cached world-space heights into current frame data. If heights are coming from an ODC, then they are in
// object-space and are converted to world-space as the LOD data stores world-space height.

Shader "Crest/Inputs/Depth/Cached Depths"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			// When blending, take highest terrain height
			BlendOp Max
			ColorMask R

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
		
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _HeightOffset;

			CBUFFER_START(CrestPerOceanInput)
			float4 _MainTex_ST;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.position = UnityObjectToClipPos(input.positionOS);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return output;
			}

			float2 Frag(Varyings input) : SV_Target
			{
				return float2(tex2D(_MainTex, input.uv).x + _HeightOffset, 0.0);
			}
			ENDCG
		}
	}
}
