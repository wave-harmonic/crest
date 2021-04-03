// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Animated Waves/Add From Texture"
{
	Properties
	{
		_MainTex("Texture", 2D) = "black" {}
		_Strength( "Strength", float ) = 1
		[Toggle] _HeightsOnly("Heights Only", Float) = 1
	}

	SubShader
	{
		// base simulation runs on the Geometry queue, before this shader.
		// this shader adds interaction forces on top of the simulation result.
		Tags { "Queue" = "Transparent" }
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma shader_feature_local _HEIGHTSONLY_ON

			#include "UnityCG.cginc"

			sampler2D _MainTex;

			CBUFFER_START(CrestPerOceanInput)
			float4 _MainTex_ST;
			float _Strength;
			float _Weight;
			float3 _DisplacementAtInputPosition;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				float3 worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement
				worldPos.xz -= _DisplacementAtInputPosition.xz;
				o.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
				
				o.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				half3 texSample = tex2D(_MainTex, input.uv).xyz;

				half3 displacement = (half3)0.0;
#if _HEIGHTSONLY_ON
				displacement.y = texSample.x * _Strength;
#else
				displacement.xyz = texSample * _Strength;
#endif

				return _Weight * half4(displacement, 0.0);
			}

			ENDCG
		}
	}
}
