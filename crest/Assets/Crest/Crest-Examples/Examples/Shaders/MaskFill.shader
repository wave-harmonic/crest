// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Examples/Mask Fill"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		[Toggle] _Masked("Masked", Float) = 1.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		CGINCLUDE
		#pragma shader_feature_local _MASKED_ON

		UNITY_DECLARE_SCREENSPACE_TEXTURE(_FillTexture)

		void Clip(float4 screenPos)
		{
			float2 positionNDC = screenPos.xy / screenPos.w;
			float deviceZ = screenPos.z / screenPos.w;

			if (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_FillTexture, positionNDC).r == 0.0)
			{
				discard;
			}
		}
		ENDCG

		Pass
		{
			// Required as we are modifying geometry in the fragment shader. Depth for CameraDepthTexture will come from
			// this pass.
			Name "Shadow Caster"
			Tags { "LightMode"="ShadowCaster" }

			CGPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment

			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			struct Varyings
			{
				V2F_SHADOW_CASTER;
				float4 screenPos : TEXCOORD0;
			};

			Varyings Vertex(appdata_base v)
			{
				Varyings output;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(output)
				output.screenPos = ComputeScreenPos(output.pos);
				return output;
			}

			float4 Fragment(Varyings input) : SV_Target
			{
#if _MASKED_ON
				Clip(input.screenPos);
#endif
				SHADOW_CASTER_FRAGMENT(input)
			}
			ENDCG
		}

		CGPROGRAM
		#pragma surface Surface Standard fullforwardshadows
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input
		{
			float2 uv_MainTex;
			float4 screenPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void Surface(Input input, inout SurfaceOutputStandard output)
		{
#if _MASKED_ON
			Clip(input.screenPos);
#endif

			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, input.uv_MainTex) * _Color;
			output.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			output.Metallic = _Metallic;
			output.Smoothness = _Glossiness;
			output.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
