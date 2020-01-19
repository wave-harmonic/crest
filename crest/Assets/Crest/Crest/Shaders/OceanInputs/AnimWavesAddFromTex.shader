// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Animated Waves/Add From Texture"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Strength( "Strength", float ) = 1
		[Toggle] _HeightsOnly("Heights Only", Float) = 1
		[Toggle] _GenerateDisplacementsFromHeights("Generate Displacements From Heights", Float) = 0
		_GenerateDisplacementStrength("Generate Displacement Strength", Range(0, 10)) = 1
		[Toggle] _SSSFromAlpha("Sub Surface Scattering (SSS) from Alpha", Float) = 0
		_SSSStrength("SSS Strength", Float) = 0.5
	}

	SubShader
	{
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma shader_feature _HEIGHTSONLY_ON
			#pragma shader_feature _SSSFROMALPHA_ON
			#pragma shader_feature _GENERATEDISPLACEMENTSFROMHEIGHTS_ON

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
			float _Strength;
			float _SSSStrength;
			float _GenerateDisplacementStrength;
			float _Weight;

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float2 uv : TEXCOORD1;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				o.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				half3 displacement = 0.0;
				half sss = 0.0;

				half4 texSample = tex2D(_MainTex, input.uv);

#if _HEIGHTSONLY_ON
				displacement.y = texSample.x;

#if _GENERATEDISPLACEMENTSFROMHEIGHTS_ON
				float height_x = tex2D(_MainTex, input.uv + float2(_MainTex_TexelSize.x, 0.0));
				float height_z = tex2D(_MainTex, input.uv + float2(0.0, _MainTex_TexelSize.y));
				displacement.x = _GenerateDisplacementStrength * (height_x - displacement.y) / ddx(input.positionWS.x);
				displacement.z = _GenerateDisplacementStrength * (height_z - displacement.y) / ddy(input.positionWS.z);
#endif
				displacement *= _Strength;
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
