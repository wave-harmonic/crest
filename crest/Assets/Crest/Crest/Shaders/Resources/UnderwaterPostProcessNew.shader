// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Underwater/Post Process New"
{
	HLSLINCLUDE
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		#include "../OceanConstants.hlsl"

		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
		float4 _MainTex_TexelSize;

		TEXTURE2D_SAMPLER2D(_Mask, sampler_Mask);
		float4 _Mask_TexelSize;

		TEXTURE2D_SAMPLER2D(_MaskDepthTex, sampler_MaskDepthTex);
		float4 _MaskDepthTex_TexelSize;

		float _OceanHeight;
		float4x4 _InvViewProjection;
		float4x4 _InvViewProjectionRight;
	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

				#pragma vertex Vert
				#pragma fragment Frag

				// TODO - can we use VertUVTransform from StdLib.hlsl within the postprocessing package?
				VaryingsDefault Vert(AttributesDefault v)
				{
					VaryingsDefault o;
					o.vertex = float4(v.vertex.xy, 0.0, 1.0);
					o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);

#if UNITY_UV_STARTS_AT_TOP
					o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif

					o.texcoordStereo = TransformStereoScreenSpaceTex(o.texcoord, 1.0);

					return o;
				}

				half4 Frag(VaryingsDefault input) : SV_Target
				{
					float3 viewWS;
					float farPlanePixelHeight;
					{
						// We calculate these values in the pixel shader as
						// calculating them in the vertex shader results in
						// precision errors.
						const float2 pixelCS = input.texcoord * 2.0 - 1.0;
#if UNITY_SINGLE_PASS_STEREO || UNITY_STEREO_INSTANCING_ENABLED || UNITY_STEREO_MULTIVIEW_ENABLED
						const float4x4 InvViewProjection = unity_StereoEyeIndex == 0 ? _InvViewProjection : _InvViewProjectionRight;
#else
						const float4x4 InvViewProjection = _InvViewProjection;
#endif
						const float4 pixelWS_H = mul(InvViewProjection, float4(pixelCS, 1.0, 1.0));
						const float3 pixelWS = pixelWS_H.xyz / pixelWS_H.w;
						viewWS = _WorldSpaceCameraPos - pixelWS;
						farPlanePixelHeight = pixelWS.y;
					}

#if !_FULL_SCREEN_EFFECT
					const bool isBelowHorizon = (farPlanePixelHeight <= _OceanHeight);
#else
					const bool isBelowHorizon = true;
#endif

					const float2 uvScreenSpace = input.texcoordStereo; // TransformStereoScreenSpaceTex(input.uv, 1.0);
					half3 sceneColour = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvScreenSpace).xyz;

					float sceneZ01 = SAMPLE_TEXTURE2D(_MaskDepthTex, sampler_MaskDepthTex, uvScreenSpace).x;

					float mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.texcoordStereo).x;

					//const bool isBelowHorizon = (farPlanePixelHeight <= _OceanHeight);
					bool isOceanSurface = mask != UNDERWATER_MASK_NO_MASK; // && (sceneZ01 < oceanDepth01);
					bool isUnderwater = mask == UNDERWATER_MASK_WATER_SURFACE_BELOW || (isBelowHorizon && mask != UNDERWATER_MASK_WATER_SURFACE_ABOVE);

//#if _DEBUG_VIEW_OCEAN_MASK
					if (!isOceanSurface)
					{
						return float4(sceneColour * float3(isUnderwater * 0.5, (1.0 - isUnderwater) * 0.5, 1.0), 1.0);
					}
					else
					{
						return float4(sceneColour * float3(mask == UNDERWATER_MASK_WATER_SURFACE_ABOVE, mask == UNDERWATER_MASK_WATER_SURFACE_BELOW, 0.0), 1.0);
					}
//#endif
				}
			ENDHLSL
		}
	}
}
