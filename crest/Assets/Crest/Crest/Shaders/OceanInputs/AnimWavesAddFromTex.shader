// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Animated Waves/Add From Texture"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Strength( "Strength", float ) = 1
		[Toggle] _HeightsOnly("Heights Only", Float) = 1
		[Toggle] _SSSFromAlpha("Sub Surface Scattering (SSS) from Alpha", Float) = 0
		_SSSStrength("SSS Strength", Float) = 0.5
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

			#pragma shader_feature _HEIGHTSONLY_ON
			#pragma shader_feature _SSSFROMALPHA_ON

			#include "UnityCG.cginc"

			sampler2D _MainTex;

			CBUFFER_START(CrestPerOceanInput)
			float4 _MainTex_ST;
			float _Strength;
			float _SSSStrength;
			float _Weight;
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
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				half3 displacement = 0.0;
				half sss = 0.0;

				half4 texSample = tex2D(_MainTex, input.uv);

#if _HEIGHTSONLY_ON
				displacement.y = texSample.x * _Strength;
#else
				displacement.xyz = texSample.xyz * _Strength;
#endif

#if _SSSFROMALPHA_ON
				sss = texSample.x * _SSSStrength;
#endif

				return _Weight * half4(displacement, sss);
			}

			ENDCG
		}
	}
}
