// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Flow/Fixed Direction"
{
	Properties
	{
		_Speed("Speed", Range(0.0, 10.0)) = 1.0
		_Direction("Direction", Range(0.0, 1.0)) = 0.0
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			float _Speed;
			float _Direction;

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 vel : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.vel = _Speed * float2(cos(_Direction * 6.283185), sin(_Direction * 6.283185));
				return o;
			}
			
			float2 Frag(Varyings input) : SV_Target
			{
				return input.vel;
			}
			ENDCG
		}
	}
}
