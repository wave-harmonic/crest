// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Simulation/Update Shadow"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			// this turns on all the shady stuff (literally - its all deprecated)
			#define SHADOW_COLLECTOR_PASS

			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"

			// To enable external shadows (i.e. clouds, fog, etc.), uncomment the following line and set the path to your shader that provides the shadows
			// the shader include file should provide a function called float EXTERNAL_SHADOW_PASS_FUNC(float3 worldPos, float existingShadow, bool highDetail)
			// return value is the new shadow value (0-1, 0 for full shadow, 1 for no shadow), the worldPos is in world space, existing shadow is the current amount of shadow,
			// and highDetail is an option to provide high or low shadow quality for performance
			// typically this external shadow function would return the min(externalShadow, existingShadow) but this is left up to the implementation
			//#define EXTERNAL_SHADOW_PASS
#if defined(EXTERNAL_SHADOW_PASS)
			#define EXTERNAL_SHADOWS_HIGH_DETAIL false // set to true for high quality external shadows
			#include "../../../../YourAsset/Shaders/ExternalShadowShader.cginc" // change to the path for your shadow shader to include
#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
			};

			struct Varyings
			{
				V2F_SHADOW_COLLECTOR;
				half4 ShadowCoord0_dxdz : TEXCOORD5;
				half4 ShadowCoord1_dxdz : TEXCOORD6;
				half4 ShadowCoord2_dxdz : TEXCOORD7;
				half4 ShadowCoord3_dxdz : TEXCOORD8;
				float4 MainCameraCoords : TEXCOORD9;
#if defined(EXTERNAL_SHADOW_PASS) && defined(EXTERNAL_SHADOW_PASS_FUNC)
				float3 WorldPos			: TEXCOORD10;
#endif
			};

			uniform float3 _CenterPos;
			uniform float3 _Scale;
			uniform float3 _CamPos;
			uniform float3 _CamForward;
			// Settings._jitterDiameterSoft, Settings._jitterDiameterHard, Settings._currentFrameWeightSoft, Settings._currentFrameWeightHard
			uniform float4 _JitterDiameters_CurrentFrameWeights;
			float4x4 _MainCameraProjectionMatrix;

			// noise functions used for jitter
			#include "../GPUNoise/GPUNoise.hlsl"

			// Provides _SimDeltaTime (see comment at this definition)
			float _SimDeltaTime;
			float _SimDeltaTimePrev;

			Varyings Vert(Attributes v)
			{
				Varyings o;

				// the code below is baked out and specialised from C:\Program Files\Unity\Editor\Data\CGIncludes\UnityCG.cginc
				// TRANSFER_SHADOW_COLLECTOR . it needs to be specialised because its rendering a quad from a Blit(), instead
				// of rendering real geometry from a worldspace camera. the world space rendering could probably be set up with
				// some hoop jumping but i guess ill go for this for now.

				o.pos = UnityObjectToClipPos(v.positionOS);

				// world pos from [0,1] quad
				float4 wpos = float4(float3(v.positionOS.x - 0.5, 0.0, v.positionOS.y - 0.5) * _Scale * 4.0 + _CenterPos, 1.0);

				// this could add wave height/disp??
				wpos.y = _OceanCenterPosWorld.y;

#if defined(EXTERNAL_SHADOW_PASS) && defined(EXTERNAL_SHADOW_PASS_FUNC)
				o.WorldPos = wpos.xyz;
#endif

				o._WorldPosViewZ.xyz = wpos.xyz;
				o._WorldPosViewZ.w = dot(wpos.xyz - _CamPos, _CamForward);
				
				o._ShadowCoord0 = mul(unity_WorldToShadow[0], wpos).xyz;
				o._ShadowCoord1 = mul(unity_WorldToShadow[1], wpos).xyz;
				o._ShadowCoord2 = mul(unity_WorldToShadow[2], wpos).xyz;
				o._ShadowCoord3 = mul(unity_WorldToShadow[3], wpos).xyz;

				// working hard to get derivatives for shadow uvs, so that i can jitter the world position in the fragment shader. this
				// enables per-fragment noise (required to avoid wobble), and is required because each cascade has a different scale etc.
				o.ShadowCoord0_dxdz.xy = mul(unity_WorldToShadow[0], wpos + float4(1.0, 0.0, 0.0, 0.0)).xz - o._ShadowCoord0.xz;
				o.ShadowCoord0_dxdz.zw = mul(unity_WorldToShadow[0], wpos + float4(0.0, 0.0, 1.0, 0.0)).xz - o._ShadowCoord0.xz;
				o.ShadowCoord1_dxdz.xy = mul(unity_WorldToShadow[1], wpos + float4(1.0, 0.0, 0.0, 0.0)).xz - o._ShadowCoord1.xz;
				o.ShadowCoord1_dxdz.zw = mul(unity_WorldToShadow[1], wpos + float4(0.0, 0.0, 1.0, 0.0)).xz - o._ShadowCoord1.xz;
				o.ShadowCoord2_dxdz.xy = mul(unity_WorldToShadow[2], wpos + float4(1.0, 0.0, 0.0, 0.0)).xz - o._ShadowCoord2.xz;
				o.ShadowCoord2_dxdz.zw = mul(unity_WorldToShadow[2], wpos + float4(0.0, 0.0, 1.0, 0.0)).xz - o._ShadowCoord2.xz;
				o.ShadowCoord3_dxdz.xy = mul(unity_WorldToShadow[3], wpos + float4(1.0, 0.0, 0.0, 0.0)).xz - o._ShadowCoord3.xz;
				o.ShadowCoord3_dxdz.zw = mul(unity_WorldToShadow[3], wpos + float4(0.0, 0.0, 1.0, 0.0)).xz - o._ShadowCoord3.xz;

				o.MainCameraCoords = mul(_MainCameraProjectionMatrix, wpos);

				return o;
			}

			fixed ComputeShadow(in Varyings input, in float jitterDiameter, in float4 cascadeWeights)
			{
				// Sadface - copy paste all this deprecated code in from Unity.cginc, because the
				// macro has a hardcoded return statement and i need the fade param for blending, and
				// i also added jitter

				if (jitterDiameter > 0.0)
				{
					half2 jitter = jitterDiameter * (hash33(uint3(abs(input._WorldPosViewZ.xz*10.0), _Time.y*120.0)) - 0.5).xy;
					input._ShadowCoord0.xz += input.ShadowCoord0_dxdz.xy * jitter.x + input.ShadowCoord0_dxdz.zw * jitter.y;
					input._ShadowCoord1.xz += input.ShadowCoord1_dxdz.xy * jitter.x + input.ShadowCoord1_dxdz.zw * jitter.y;
					input._ShadowCoord2.xz += input.ShadowCoord2_dxdz.xy * jitter.x + input.ShadowCoord2_dxdz.zw * jitter.y;
					input._ShadowCoord3.xz += input.ShadowCoord3_dxdz.xy * jitter.x + input.ShadowCoord3_dxdz.zw * jitter.y;
				}

				float4 coord = float4(
					input._ShadowCoord0 * cascadeWeights[0] + 
					input._ShadowCoord1 * cascadeWeights[1] + 
					input._ShadowCoord2 * cascadeWeights[2] + 
					input._ShadowCoord3 * cascadeWeights[3], 1);

				SAMPLE_SHADOW_COLLECTOR_SHADOW(coord)

				return shadow;
			}

			fixed2 Frag(Varyings input) : SV_Target
			{
				fixed2 shadow = 0.0;

				// Shadow from last frame - manually implement black border
				float2 uv_lastframe = LD_0_WorldToUV(input._WorldPosViewZ.xz);
				half2 r = abs(uv_lastframe.xy - 0.5);
				if (max(r.x, r.y) < 0.49)
				{
					SampleShadow(_LD_Sampler_Shadow_0, uv_lastframe, 1.0, shadow);
				}

				// Check if the current sample is visible in the main camera (and therefore shadow map can be sampled)
				float3 projected = input.MainCameraCoords.xyz / input.MainCameraCoords.w;
				if (projected.z < 1.0 && abs(projected.x) < 1.0 && abs(projected.z) < 1.0)
				{
					// Sadface - copy paste all this deprecated code in from Unity.cginc, see similar comment above
					float3 fromCenter0 = input._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[0].xyz;
					float3 fromCenter1 = input._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[1].xyz;
					float3 fromCenter2 = input._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[2].xyz;
					float3 fromCenter3 = input._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[3].xyz;
					float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));
					float4 cascadeWeights = float4(distances2 < unity_ShadowSplitSqRadii);
					cascadeWeights.yzw = saturate(cascadeWeights.yzw - cascadeWeights.xyz);
					float sphereDist = distance(input._WorldPosViewZ.xyz, unity_ShadowFadeCenterAndType.xyz);
					half shadowFade = saturate(sphereDist * _LightShadowData.z + _LightShadowData.w);

					fixed2 shadowThisFrame;
					shadowThisFrame.x = ComputeShadow(input, _JitterDiameters_CurrentFrameWeights.x, cascadeWeights);
					shadowThisFrame.y = ComputeShadow(input, _JitterDiameters_CurrentFrameWeights.y, cascadeWeights);

#if defined(EXTERNAL_SHADOW_PASS) && defined(EXTERNAL_SHADOW_PASS_FUNC)
					shadowThisFrame = EXTERNAL_SHADOW_PASS_FUNC(input.WorldPos, shadowThisFrame, EXTERNAL_SHADOWS_HIGH_DETAIL);
#endif

					shadowThisFrame = (fixed2)1.0 - saturate(shadowThisFrame + shadowFade);

					shadow = lerp(shadow, shadowThisFrame, _JitterDiameters_CurrentFrameWeights.zw * _SimDeltaTime * 60.0);
				}

				return shadow;
			}
			ENDCG
		}
	}
}
