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

		// NOTE: SHADER_TARGET_SURFACE_ANALYSIS does not want the semicolon but compiler does for builds.
#if _MASK_MASKED
		UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture)
#ifndef SHADER_TARGET_SURFACE_ANALYSIS
		;
#endif
		UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestWaterVolumeFrontFaceTexture)
#ifndef SHADER_TARGET_SURFACE_ANALYSIS
		;
#endif
		UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestWaterVolumeBackFaceTexture)
#ifndef SHADER_TARGET_SURFACE_ANALYSIS
		;
#endif
#elif _MASK_FILL
		UNITY_DECLARE_SCREENSPACE_TEXTURE(_FillTexture)
#ifndef SHADER_TARGET_SURFACE_ANALYSIS
		;
#endif
#endif // _MASK

		UNITY_DECLARE_TEX2D(_Albedo);
		fixed4 _Color;
		half _Cutoff;

		// NOTE: Do not use discard in either of these methods or incur the following:
		// > Program 'frag_Surface', internal error: argument pulled into unrelated predicate.
#if _MASK_MASKED
		bool Clip(float4 screenPos)
		{
			float2 positionNDC = screenPos.xy / screenPos.w;
			float deviceDepth = screenPos.z / screenPos.w;

			float rawFrontFaceZ = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestWaterVolumeFrontFaceTexture, positionNDC).r;
			if (rawFrontFaceZ < deviceDepth && rawFrontFaceZ > 0.0)
			{
				return true;
			}

			float rawBackFaceZ = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestWaterVolumeBackFaceTexture, positionNDC).r;
			if (rawBackFaceZ > deviceDepth && rawBackFaceZ > 0.0)
			{
				return true;
			}

			return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, positionNDC).r == 0.0;
		}
#endif // _MASK_MASKED

#if _MASK_FILL
		bool Clip(float4 screenPos)
		{
			float2 positionNDC = screenPos.xy / screenPos.w;
			float deviceZ = screenPos.z / screenPos.w;

			return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_FillTexture, positionNDC).r == 0.0;
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

			#include "../../../Crest/Shaders/Helpers/BIRP/Core.hlsl"

			struct Varyings
			{
				V2F_SHADOW_CASTER;
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(appdata_base v)
			{
				Varyings output;
				ZERO_INITIALIZE(Varyings, output);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				TRANSFER_SHADOW_CASTER_NORMALOFFSET(output)
				output.screenPos = ComputeScreenPos(output.pos);
				output.uv = v.texcoord.xy;
				return output;
			}

			float4 frag(Varyings input) : SV_Target
			{
				// We need this when sampling a screenspace texture.
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#ifndef _MASK_NONE
#ifndef _SHADOW_PASS
				if (Clip(input.screenPos))
				{
					discard;
				}
#endif // !_SHADOWS
#endif // _MASKED_ON

				fixed4 c = UNITY_SAMPLE_TEX2D(_Albedo, input.uv) * _Color;
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
			// NOTE: The else is required to avoid the following error:
			// > Program 'frag_Surface', internal error: argument pulled into unrelated predicate.
#if !_MASK_NONE
			if (Clip(input.screenPos))
			{
				discard;
			}
			else
#endif
			{
				fixed4 c = UNITY_SAMPLE_TEX2D(_Albedo, input.uv_Albedo) * _Color;
				clip(c.a - _Cutoff);
				output.Albedo = c.rgb;
				output.Metallic = _Metallic;
				output.Smoothness = _Smoothness;
#if _EMISSION_ON
				output.Emission = c.rgb * UNITY_SAMPLE_TEX2D(_Albedo, input.uv_Albedo).a * _EmissionColor;
#endif
				output.Alpha = c.a;
			}

		}
		ENDCG
	}
	FallBack "Diffuse"
}
