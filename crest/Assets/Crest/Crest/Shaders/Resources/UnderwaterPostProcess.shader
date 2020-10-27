// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Modelled after the ScreenSpaceReflections shader in the post-process static V2
// as we need to use Unity's lighting structures

Shader "Crest/Underwater/Post Process Stack"
{
	Properties
	{
		// Add a meniscus to the boundary between water and air
		[Toggle] _Meniscus("Meniscus", float) = 1

		[Header(Debug Options)]
		[Toggle] _CompileShaderWithDebugInfo("Compile Shader With Debug Info (D3D11)", Float) = 0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			// Use multi_compile because these keywords are copied over from the ocean material. With shader_feature,
			// the keywords would be stripped from builds. Unused shader variants are stripped using a build processor.
			#pragma multi_compile_local __ _SUBSURFACESCATTERING_ON
			#pragma multi_compile_local __ _SUBSURFACESHALLOWCOLOUR_ON
			#pragma multi_compile_local __ _TRANSPARENCY_ON
			#pragma multi_compile_local __ _CAUSTICS_ON
			#pragma multi_compile_local __ _SHADOWS_ON
			#pragma multi_compile_local __ _COMPILESHADERWITHDEBUGINFO_ON

			#pragma shader_feature_local _MENISCUS_ON

			#pragma multi_compile_local __ _FULL_SCREEN_EFFECT
			#pragma multi_compile_local __ _DEBUG_VIEW_OCEAN_MASK

			#if _COMPILESHADERWITHDEBUGINFO_ON
			#pragma enable_d3d11_debug_symbols
			#endif

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			#include "../OceanConstants.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanGlobals.hlsl"
			#include "../OceanHelpersNew.hlsl"
			#include "../OceanHelpersNew.hlsl"

			half3 _AmbientLighting;

			// In-built Unity textures
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraDepthTexture);
			sampler2D _Normals;

			#include "../OceanEmission.hlsl"

			float _OceanHeight;
			float4x4 _InvViewProjection;
			float4x4 _InvViewProjectionRight;
			float4 _HorizonPosNormal;
			float4 _HorizonPosNormalRight;
			half _DataSliceOffset;

			uint _StereoEyeIndex;

			// Ported from StdLib, we can't include it as it'll conflict with internal Unity includes
			struct Attributes
			{
				float3 vertex : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 viewWS : TEXCOORD1;
			};

			Varyings Vert (Attributes input)
			{
				Varyings output;

				output.positionCS = float4(input.vertex.xy, 0.0, 1.0);
				output.uv = (input.vertex.xy + 1.0) * 0.5;
#if UNITY_UV_STARTS_AT_TOP
				output.uv = output.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif

				// Compute world space view vector
				{
					const float2 pixelCS = output.uv * 2 - float2(1.0, 1.0);
#if CREST_HANDLE_XR
					const float4x4 InvViewProjection = _StereoEyeIndex == 0 ? _InvViewProjection : _InvViewProjectionRight;
#else
					const float4x4 InvViewProjection = _InvViewProjection;
#endif
					const float4 pixelWS_H = mul(InvViewProjection, float4(pixelCS, 1.0, 1.0));
					const float3 pixelWS = pixelWS_H.xyz / pixelWS_H.w;
					output.viewWS = _WorldSpaceCameraPos - pixelWS;
				}

				return output;
			}

			sampler2D _MainTex;

			// STEREO_INSTANCING_ON is used exclusively in the post-processing stack.
#if STEREO_INSTANCING_ON
			UNITY_DECLARE_TEX2DARRAY(_CrestOceanMaskTexture);
			UNITY_DECLARE_TEX2DARRAY(_CrestOceanMaskDepthTexture);
#else
			sampler2D _CrestOceanMaskTexture;
			sampler2D _CrestOceanMaskDepthTexture;
#endif

			half3 ApplyUnderwaterEffect(half3 sceneColour, const float sceneZ01, const half3 view, bool isOceanSurface)
			{
				const float sceneZ = LinearEyeDepth(sceneZ01);
				const float3 lightDir = _WorldSpaceLightPos0.xyz;

				half3 scatterCol = 0.0;
				int sliceIndex = clamp(_DataSliceOffset, 0, _SliceCount - 2);
				{
					float3 dummy;
					half sss = 0.0;
					// Offset slice so that we dont get high freq detail. But never use last lod as this has crossfading.
					const float3 uv_slice = WorldToUV(_WorldSpaceCameraPos.xz, _CrestCascadeData[sliceIndex], sliceIndex);
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice, 1.0, dummy, sss);

					// depth and shadow are computed in ScatterColour when underwater==true, using the LOD1 texture.
					const float depth = 0.0;
					const half shadow = 1.0;
					{
						const float meshScaleLerp = _CrestPerCascadeInstanceData[sliceIndex]._meshScaleLerp;
						const float baseCascadeScale = _CrestCascadeData[0]._scale;
						scatterCol = ScatterColour(_AmbientLighting, depth, _WorldSpaceCameraPos, lightDir, view, shadow, true, true, sss, meshScaleLerp, baseCascadeScale, _CrestCascadeData[sliceIndex]);
					}
				}

#if _CAUSTICS_ON
				if (sceneZ01 != 0.0 && !isOceanSurface)
				{
					ApplyCaustics(view, lightDir, sceneZ, _Normals, true, sceneColour, _CrestCascadeData[sliceIndex], _CrestCascadeData[sliceIndex + 1]);
				}
#endif // _CAUSTICS_ON

				return lerp(sceneColour, scatterCol, saturate(1.0 - exp(-_DepthFogDensity.xyz * sceneZ)));
			}

			fixed4 Frag (Varyings input) : SV_Target
			{
				float3 viewWS;
				float farPlanePixelHeight;

#if !_FULL_SCREEN_EFFECT
				// The horizon line is the intersection between the far plane and the ocean plane. The pos and normal of this
				// intersection line is passed in.
#if CREST_HANDLE_XR
				const bool isBelowHorizon = _StereoEyeIndex == 0 ?
					dot(input.uv - _HorizonPosNormal.xy, _HorizonPosNormal.zw) > 0.0 :
					dot(input.uv - _HorizonPosNormalRight.xy, _HorizonPosNormalRight.zw) > 0.0;
#else // CREST_HANDLE_XR
				const bool isBelowHorizon = dot(input.uv - _HorizonPosNormal.xy, _HorizonPosNormal.zw) > 0.0;
#endif // CREST_HANDLE_XR
#else // !_FULL_SCREEN_EFFECT
				const bool isBelowHorizon = true;
#endif // !_FULL_SCREEN_EFFECT

				// STEREO_INSTANCING_ON is used exclusively in the post-processing stack.
#if STEREO_INSTANCING_ON
				const float3 uvScreenSpace = float3(UnityStereoTransformScreenSpaceTex(input.uv), _StereoEyeIndex);
#else
				const float2 uvScreenSpace = UnityStereoTransformScreenSpaceTex(input.uv);
#endif

				half3 sceneColour = tex2D(_MainTex, uvScreenSpace).rgb;

				float sceneZ01 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, uvScreenSpace).x;
				// STEREO_INSTANCING_ON is used exclusively in the post-processing stack.
#if STEREO_INSTANCING_ON
				float mask = UNITY_SAMPLE_TEX2DARRAY(_CrestOceanMaskTexture, uvScreenSpace).x;
				const float oceanDepth01 = UNITY_SAMPLE_TEX2DARRAY(_CrestOceanMaskDepthTexture, uvScreenSpace);
#else
				float mask = tex2D(_CrestOceanMaskTexture, uvScreenSpace).x;
				const float oceanDepth01 = tex2D(_CrestOceanMaskDepthTexture, uvScreenSpace);
#endif
				bool isOceanSurface = mask != UNDERWATER_MASK_NO_MASK && (sceneZ01 < oceanDepth01);
				bool isUnderwater = mask == UNDERWATER_MASK_WATER_SURFACE_BELOW || (isBelowHorizon && mask != UNDERWATER_MASK_WATER_SURFACE_ABOVE);
				sceneZ01 = isOceanSurface ? oceanDepth01 : sceneZ01;

				float wt = 1.0;

#if _MENISCUS_ON
				// Detect water to no water transitions which happen if mask values on below pixels are less than this mask
				//if (mask <= 1.0)
				{
					// Looks at pixels below this pixel and if there is a transition from above to below, darken the pixel
					// to emulate a meniscus effect. It does a few to get a thicker line than 1 pixel. The line it produces is
					// smooth on the top side and sharp at the bottom. It might be possible to detect where the edge is and do
					// a calculation to get it smooth both above and below, but might be more complex.
					float wt_mul = 0.9;
					float4 dy = float4(0.0, -1.0, -2.0, -3.0) / _ScreenParams.y;
					wt *= (tex2D(_CrestOceanMaskTexture, uvScreenSpace + dy.xy).x > mask) ? wt_mul : 1.0;
					wt *= (tex2D(_CrestOceanMaskTexture, uvScreenSpace + dy.xz).x > mask) ? wt_mul : 1.0;
					wt *= (tex2D(_CrestOceanMaskTexture, uvScreenSpace + dy.xw).x > mask) ? wt_mul : 1.0;
				}
#endif // _MENISCUS_ON

#if _DEBUG_VIEW_OCEAN_MASK
				if (!isOceanSurface)
				{
					return float4(sceneColour * float3(isUnderwater * 0.5, (1.0 - isUnderwater) * 0.5, 1.0), 1.0);
				}
				else
				{
					return float4(sceneColour * float3(mask == UNDERWATER_MASK_WATER_SURFACE_ABOVE, mask == UNDERWATER_MASK_WATER_SURFACE_BELOW, 0.0), 1.0);
				}
#else
				if (isUnderwater)
				{
					const half3 view = normalize(input.viewWS);
					sceneColour = ApplyUnderwaterEffect(sceneColour, sceneZ01, view, isOceanSurface);
				}

				return half4(wt * sceneColour, 1.0);
#endif // _DEBUG_VIEW_OCEAN_MASK
			}
			ENDCG
		}
	}
}
