// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Underwater/Post Process New"
{
	HLSLINCLUDE
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
		float4 _MainTex_TexelSize;
	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

				#pragma vertex VertDefault
				#pragma fragment Frag

				half4 Frag(VaryingsDefault i) : SV_Target
				{
					return 1.5 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
				}
			ENDHLSL
		}
	}
}
