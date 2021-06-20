// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Underwater/Underwater Effect"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma multi_compile_instancing

			// Use multi_compile because these keywords are copied over from the ocean material. With shader_feature,
			// the keywords would be stripped from builds. Unused shader variants are stripped using a build processor.
			#pragma multi_compile_local __ _SUBSURFACESCATTERING_ON
			#pragma multi_compile_local __ _SUBSURFACESHALLOWCOLOUR_ON
			#pragma multi_compile_local __ _TRANSPARENCY_ON
			#pragma multi_compile_local __ _CAUSTICS_ON
			#pragma multi_compile_local __ _SHADOWS_ON
			#pragma multi_compile_local __ _COMPILESHADERWITHDEBUGINFO_ON

			#pragma multi_compile_local __ CREST_MENISCUS

			#pragma multi_compile_local __ _PROJECTION_PERSPECTIVE _PROJECTION_ORTHOGRAPHIC

			#pragma multi_compile_local __ _FULL_SCREEN_EFFECT
			#pragma multi_compile_local __ _DEBUG_VIEW_OCEAN_MASK

			#if _COMPILESHADERWITHDEBUGINFO_ON
			#pragma enable_d3d11_debug_symbols
			#endif

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanShaderData.hlsl"
			#include "../OceanHelpersNew.hlsl"
			#include "../OceanShaderHelpers.hlsl"
			#include "../FullScreenTriangle.hlsl"

			half3 _AmbientLighting;

			#include "../OceanEmission.hlsl"

			float4x4 _InvViewProjection;
			float4x4 _InvViewProjectionRight;
			float4 _HorizonPosNormal;
			float4 _HorizonPosNormalRight;
			half _DataSliceOffset;

			struct Attributes
			{
				uint id : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 viewWS : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings Vert (Attributes input)
			{
				Varyings output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_OUTPUT(Varyings, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.positionCS = GetFullScreenTriangleVertexPosition(input.id);
				output.uv = GetFullScreenTriangleTexCoord(input.id);

				// Compute world space view vector
				{
					const float2 pixelCS = output.uv * 2.0 - float2(1.0, 1.0);
#if CREST_HANDLE_XR
					const float4x4 InvViewProjection = unity_StereoEyeIndex == 0 ? _InvViewProjection : _InvViewProjectionRight;
#else
					const float4x4 InvViewProjection = _InvViewProjection;
#endif
					const float4 pixelWS_H = mul(InvViewProjection, float4(pixelCS, 1.0, 1.0));
					const float3 pixelWS = pixelWS_H.xyz / pixelWS_H.w;
					output.viewWS = _WorldSpaceCameraPos - pixelWS;
				}

				return output;
			}

			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestCameraColorTexture);
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture);
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestOceanMaskDepthTexture);

			half3 ApplyUnderwaterEffect(half3 sceneColour, const float rawDepth, const float sceneZ, const half3 view, bool isOceanSurface)
			{
				const float3 lightDir = _WorldSpaceLightPos0.xyz;
				const half3 lightCol = _LightColor0;

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
						scatterCol = ScatterColour(_AmbientLighting, depth, _WorldSpaceCameraPos, lightDir, view, shadow, true, true, lightCol, sss, meshScaleLerp, baseCascadeScale, _CrestCascadeData[sliceIndex]);
					}
				}

#if _CAUSTICS_ON
				if (rawDepth != 0.0 && !isOceanSurface)
				{
					ApplyCaustics(view, lightDir, sceneZ, _Normals, true, sceneColour, _CrestCascadeData[sliceIndex], _CrestCascadeData[sliceIndex + 1]);
				}
#endif // _CAUSTICS_ON

				return lerp(sceneColour, scatterCol, saturate(1.0 - exp(-_DepthFogDensity.xyz * sceneZ)));
			}

			fixed4 Frag (Varyings input) : SV_Target
			{
				// We need this when sampling a screenspace texture.
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if !_FULL_SCREEN_EFFECT
				// The horizon line is the intersection between the far plane and the ocean plane. The pos and normal of this
				// intersection line is passed in.
#if CREST_HANDLE_XR
				const float4 horizonPositionNormal = unity_StereoEyeIndex == 0 ? _HorizonPosNormal : _HorizonPosNormalRight;
#else // CREST_HANDLE_XR
				const float4 horizonPositionNormal = _HorizonPosNormal;
#endif // CREST_HANDLE_XR
				const bool isBelowHorizon = dot(input.uv - horizonPositionNormal.xy, horizonPositionNormal.zw) > 0.0;
#else // !_FULL_SCREEN_EFFECT
				const bool isBelowHorizon = true;
#endif // !_FULL_SCREEN_EFFECT

				const float2 uvScreenSpace = UnityStereoTransformScreenSpaceTex(input.uv);

				half3 sceneColour = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestCameraColorTexture, uvScreenSpace).rgb;

				float rawDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, uvScreenSpace).x;

				float mask = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, uvScreenSpace).x;
				const float rawOceanDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskDepthTexture, uvScreenSpace);
				bool isOceanSurface = mask != UNDERWATER_MASK_NO_MASK && (rawDepth < rawOceanDepth);
				bool isUnderwater = mask == UNDERWATER_MASK_WATER_SURFACE_BELOW || (isBelowHorizon && mask != UNDERWATER_MASK_WATER_SURFACE_ABOVE);
				rawDepth = isOceanSurface ? rawOceanDepth : rawDepth;
				const float sceneZ = CrestLinearEyeDepth(rawDepth);

				float wt = 1.0;

#if CREST_MENISCUS
#if !_FULL_SCREEN_EFFECT
				// Render meniscus by checking mask in opposite direction of surface normal.
				// If the sample is different than the current mask, apply meniscus.
				// Skip the meniscus beyond one unit to prevent numerous artefacts.
				if (mask != UNDERWATER_MASK_NO_MASK && sceneZ < 1.0)
				{
					float wt_mul = 0.9;
					// Adding the mask value will flip the UV when mask is below surface.
					// Apply the horizon normal so it works with any orientation.
					float2 dy = (float2(-1.0 + mask, -1.0 + mask) / _ScreenParams.y) * -horizonPositionNormal.zw;
					wt *= (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, uvScreenSpace + dy * 1.0).x != mask) ? wt_mul : 1.0;
					wt *= (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, uvScreenSpace + dy * 2.0).x != mask) ? wt_mul : 1.0;
					wt *= (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, uvScreenSpace + dy * 3.0).x != mask) ? wt_mul : 1.0;
				}
#endif
#endif // CREST_MENISCUS

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
					sceneColour = ApplyUnderwaterEffect(sceneColour, rawDepth, sceneZ, view, isOceanSurface);
				}

				return half4(wt * sceneColour, 1.0);
#endif // _DEBUG_VIEW_OCEAN_MASK
			}
			ENDCG
		}
	}
}
