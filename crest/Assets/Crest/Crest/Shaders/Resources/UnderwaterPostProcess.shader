// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Underwater/Post Process"
{
	Properties
	{
		// These mirror the same toggles on the ocean material

		[Header(Scattering)]
		[Toggle] _Shadows("Shadowing", Float) = 0

		[Header(Subsurface Scattering)]
		[Toggle] _SubSurfaceScattering("Enable", Float) = 1

		[Header(Shallow Scattering)]
		[Toggle] _SubSurfaceShallowColour("Enable", Float) = 1

		[Header(Transparency)]
		[Toggle] _Transparency("Enable", Float) = 1

		[Header(Caustics)]
		[Toggle] _Caustics("Enable", Float) = 1

		[Header(Underwater)]
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

			#pragma shader_feature _SUBSURFACESCATTERING_ON
			#pragma shader_feature _SUBSURFACESHALLOWCOLOUR_ON
			#pragma shader_feature _TRANSPARENCY_ON
			#pragma shader_feature _CAUSTICS_ON
			#pragma shader_feature _SHADOWS_ON
			#pragma shader_feature _COMPILESHADERWITHDEBUGINFO_ON
			#pragma shader_feature _MENISCUS_ON

			#pragma multi_compile __ _FULL_SCREEN_EFFECT
			#pragma multi_compile __ _DEBUG_VIEW_OCEAN_MASK

			#if _COMPILESHADERWITHDEBUGINFO_ON
			#pragma enable_d3d11_debug_symbols
			#endif

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			#include "../OceanConstants.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanGlobals.hlsl"
			#include "../OceanLODData.hlsl"
			#include "../OceanHelpersNew.hlsl"

			half3 _CrestAmbientLighting;

			#include "../OceanEmission.hlsl"

			float _OceanHeight;
			float4x4 _CrestInvViewProjection;
			float4x4 _CrestInvViewProjectionRight;
			float4 _CrestHorizonPosNormal;
			float4 _CrestHorizonPosNormalRight;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
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
				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.uv = input.uv;

				// Compute world space view vector
				{
					const float2 pixelCS = input.uv * 2 - float2(1.0, 1.0);
#if CREST_HANDLE_XR
					const float4x4 InvViewProjection = unity_StereoEyeIndex == 0 ? _CrestInvViewProjection : _CrestInvViewProjectionRight;
#else
					const float4x4 InvViewProjection = _CrestInvViewProjection;
#endif
					const float4 pixelWS_H = mul(InvViewProjection, float4(pixelCS, 1.0, 1.0));
					const float3 pixelWS = pixelWS_H.xyz / pixelWS_H.w;
					output.viewWS = _WorldSpaceCameraPos - pixelWS;
				}

				return output;
			}

			sampler2D _MainTex;
			sampler2D _CrestOceanMaskTexture;
			sampler2D _CrestOceanMaskDepthTexture;
			// TODO(TRC):Now @optimisation pack opaque information in the underwater mask texture, only enable
			// parts of this based on which underwater features we have enabled.
			sampler2D _CrestGeneralMaskTexture;
			sampler2D _CrestGeneralMaskDepthTexture;

			// In-built Unity textures
			sampler2D _CameraDepthTexture;
			sampler2D _Normals;

			#include "../UnderwaterHelpers.hlsl"

			fixed4 Frag (Varyings input) : SV_Target
			{
				float3 viewWS;
				float farPlanePixelHeight;

#if !_FULL_SCREEN_EFFECT
				// The horizon line is the intersection between the far plane and the ocean plane. The pos and normal of this
				// intersection line is passed in.
#if CREST_HANDLE_XR
				const bool isBelowHorizon = unity_StereoEyeIndex == 0 ?
					dot(input.uv - _CrestHorizonPosNormal.xy, _CrestHorizonPosNormal.zw) > 0.0 :
					dot(input.uv - _CrestHorizonPosNormalRight.xy, _CrestHorizonPosNormalRight.zw) > 0.0;
#else // CREST_HANDLE_XR
				const bool isBelowHorizon = dot(input.uv - _CrestHorizonPosNormal.xy, _CrestHorizonPosNormal.zw) > 0.0;
#endif // CREST_HANDLE_XR
#else // !_FULL_SCREEN_EFFECT
				const bool isBelowHorizon = true;
#endif // !_FULL_SCREEN_EFFECT

				const float2 uvScreenSpace = UnityStereoTransformScreenSpaceTex(input.uv);

				half3 sceneColour = tex2D(_MainTex, uvScreenSpace).rgb;

				float sceneZ01 = tex2D(_CameraDepthTexture, uvScreenSpace).x;

				// We need to have a small amount of depth tolerance to handle the
				// fact that we can have general oceanMask filter which will be rendered in the scene
				// and have their depth in the regular depth buffer.
				const float oceanDepthTolerance = 0.000045;

				float oceanMask = tex2D(_CrestOceanMaskTexture, uvScreenSpace).x;
				float oceanDepth01 = tex2D(_CrestOceanMaskDepthTexture, uvScreenSpace).x;
				bool isUnderwater = oceanMask == UNDERWATER_MASK_WATER_SURFACE_BELOW || (isBelowHorizon  && oceanMask != UNDERWATER_MASK_WATER_SURFACE_ABOVE);

				// If we have a view into underwater through a window, we need to make sure to only apply fog for the distance starting from behind it
				if(isUnderwater)
				{
					const float overrideMask = tex2D(_CrestGeneralMaskTexture, uvScreenSpace).x;
					const float overrideDepth01 = tex2D(_CrestGeneralMaskDepthTexture, uvScreenSpace).x;
					{
						const bool disableWaterBehindOverrideMask = (overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE_BACK && (oceanDepth01 < overrideDepth01));
						oceanDepth01 = disableWaterBehindOverrideMask ? overrideDepth01 : oceanDepth01;
					}

					// TODO(TRC):Now @optimisation pack opaque information in the underwater mask texture, only enable
					// parts of this based on which underwater features we have enabled.
					if(overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE || overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE_FRONT)
					{
						oceanMask = UNDERWATER_MASK_WATER_SURFACE_ABOVE;
					}
					else if(overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE_BACK)
					{
						// We want to disable underwater behind the override surface,
						// but therefore if we are already underwater, we need to ensure that we apply for to the appropriate pixels.
						oceanMask = UNDERWATER_MASK_WATER_SURFACE_BELOW;
					}
					isUnderwater = oceanMask != UNDERWATER_MASK_WATER_SURFACE_ABOVE;
				}

				// Ocean surface check is used avoid drawing caustics on the water or masked surface
				bool disableCaustics = oceanMask != UNDERWATER_MASK_NO_MASK && (sceneZ01 <= (oceanDepth01 + oceanDepthTolerance));

				sceneZ01 = disableCaustics ? oceanDepth01 : sceneZ01;
				const float sceneZ = LinearEyeDepth(sceneZ01);

				float wt = 1.0;


#if _MENISCUS_ON
				// Detect water to no water transitions which happen if oceanMask values on below pixels are less than this oceanMask
				//if (oceanMask <= 1.0)
				{
					// Looks at pixels below this pixel and if there is a transition from above to below, darken the pixel
					// to emulate a meniscus effect. It does a few to get a thicker line than 1 pixel. The line it produces is
					// smooth on the top side and sharp at the bottom. It might be possible to detect where the edge is and do
					// a calculation to get it smooth both above and below, but might be more complex.
					float wt_mul = 0.9;
					float4 dy = float4(0.0, -1.0, -2.0, -3.0) / _ScreenParams.y;
					wt *= (tex2D(_CrestOceanMaskTexture, uvScreenSpace + dy.xy).x > oceanMask) ? wt_mul : 1.0;
					wt *= (tex2D(_CrestOceanMaskTexture, uvScreenSpace + dy.xz).x > oceanMask) ? wt_mul : 1.0;
					wt *= (tex2D(_CrestOceanMaskTexture, uvScreenSpace + dy.xw).x > oceanMask) ? wt_mul : 1.0;
				}
#endif // _MENISCUS_ON

#if _DEBUG_VIEW_OCEAN_MASK
				if(!disableCaustics)
				{
					return float4(sceneColour * float3(isUnderwater * 0.5, (1.0 - isUnderwater) * 0.5, 1.0), 1.0);
				}
				else
				{
					return float4(sceneColour * float3(oceanMask == UNDERWATER_MASK_WATER_SURFACE_ABOVE, oceanMask == UNDERWATER_MASK_WATER_SURFACE_BELOW, 0.0), 1.0);
				}
#else
				if(isUnderwater)
				{
					const half3 view = normalize(input.viewWS);
					sceneColour = ApplyUnderwaterEffect(
						_LD_TexArray_AnimatedWaves,
						_Normals,
						_WorldSpaceCameraPos,
						_CrestAmbientLighting,
						sceneColour,
						sceneZ,
						sceneZ,
						view,
						_DepthFogDensity,
						disableCaustics
					);
				}

				return half4(wt * sceneColour, 1.0);
#endif // _DEBUG_VIEW_OCEAN_MASK
			}
			ENDCG
		}
	}
}
