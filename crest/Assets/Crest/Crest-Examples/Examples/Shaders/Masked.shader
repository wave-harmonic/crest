// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Examples/Masked"
{
	Properties
	{
		_Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[Toggle] _Emission("Emission", Float) = 0.0
		[HDR] _EmissionColor("Color", Color) = (0.0, 0.0, 0.0)
		_Albedo("Albedo (RGB)", 2D) = "white" {}
		_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
		_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[KeywordEnum(None, Masked, Fill)] _Mask("Masked", Float) = 1.0
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }

		CGINCLUDE
		#pragma shader_feature_local _MASK_NONE _MASK_MASKED _MASK_FILL

#if _MASK_MASKED
		UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture)
		UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestWaterVolumeFrontFaceTexture)
		UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestWaterVolumeBackFaceTexture)
#elif _MASK_FILL
		UNITY_DECLARE_SCREENSPACE_TEXTURE(_FillTexture)
#endif

		sampler2D _Albedo;
		fixed4 _Color;
		half _Cutoff;

#if _MASK_MASKED
		void Clip(float4 screenPos)
		{
			float2 positionNDC = screenPos.xy / screenPos.w;
			float deviceDepth = screenPos.z / screenPos.w;

			float rawFrontFaceZ = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestWaterVolumeFrontFaceTexture, positionNDC);
			if (rawFrontFaceZ > 0.0 && rawFrontFaceZ < deviceDepth)
			{
				discard;
			}

			float rawBackFaceZ = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestWaterVolumeBackFaceTexture, positionNDC);
			if (rawBackFaceZ > 0.0 && rawBackFaceZ > deviceDepth)
			{
				discard;
			}

			float mask = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, positionNDC);
			if (mask == 0.0)
			{
				discard;
			}
		}
#endif // _MASK_MASKED

#if _MASK_FILL
		void Clip(float4 screenPos)
		{
			float2 positionNDC = screenPos.xy / screenPos.w;
			float deviceZ = screenPos.z / screenPos.w;

			if (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_FillTexture, positionNDC).r == 0.0)
			{
				discard;
			}
		}
#endif // _MASK_FILL
		ENDCG

		Pass
		{
			// Required as we are modifying geometry in the fragment shader. Depth for CameraDepthTexture will come from
			// this pass.
			Name "Shadow Caster"
			Tags { "LightMode"="ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_shadowcaster

			#pragma multi_compile _ _SHADOW_PASS

			#include "UnityCG.cginc"

			struct Varyings
			{
				V2F_SHADOW_CASTER;
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
			};

			Varyings vert(appdata_base v)
			{
				Varyings output;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(output)
				output.screenPos = ComputeScreenPos(output.pos);
				output.uv = v.texcoord.xy;
				return output;
			}

			float4 frag(Varyings input) : SV_Target
			{
#ifndef _MASK_NONE
#ifndef _SHADOW_PASS
				Clip(input.screenPos);
#endif // !_SHADOWS
#endif // _MASKED_ON

				fixed4 c = tex2D(_Albedo, input.uv) * _Color;
				clip(c.a - _Cutoff);
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}

		CGPROGRAM
		#pragma surface Surface Standard fullforwardshadows

		#pragma target 5.0
		#pragma shader_feature_local _EMISSION_ON

		struct Input
		{
			float2 uv_Albedo;
			float4 screenPos;
		};

		half _Smoothness;
		half _Metallic;
		fixed4 _EmissionColor;

		void Surface(Input input, inout SurfaceOutputStandard output)
		{
#if !_MASK_NONE
			Clip(input.screenPos);
#endif

			fixed4 c = tex2D(_Albedo, input.uv_Albedo) * _Color;
			clip(c.a - _Cutoff);
			output.Albedo = c.rgb;
			output.Metallic = _Metallic;
			output.Smoothness = _Smoothness;
#if _EMISSION_ON
			output.Emission = c.rgb * tex2D(_Albedo, input.uv_Albedo).a * _EmissionColor;
#endif
			output.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
