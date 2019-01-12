// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Ocean/Simulation/Update Shadow"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma enable_d3d11_debug_symbols

			// this turns on all the shady stuff (literally - its all deprecated)
			#define SHADOW_COLLECTOR_PASS

			#include "UnityCG.cginc"
			#include "../../../../Crest/Shaders/OceanLODData.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				V2F_SHADOW_COLLECTOR;

				half4 ShadowCoord0_dxdz : TEXCOORD5;
				half4 ShadowCoord1_dxdz : TEXCOORD6;
				half4 ShadowCoord2_dxdz : TEXCOORD7;
				half4 ShadowCoord3_dxdz : TEXCOORD8;

				float4 MainCameraCoords : TEXCOORD9;
			};

			uniform float3 _CenterPos;
			uniform float3 _Scale;
			uniform float3 _CamPos;
			uniform float3 _CamForward;
			// Settings._jitterDiameterSoft, Settings._jitterDiameterHard, Settings._currentFrameWeightSoft, Settings._currentFrameWeightHard
			uniform float4 _JitterDiameters_CurrentFrameWeights;
			float4x4 _MainCameraProjectionMatrix;

			// noise functions used for jitter
			#include "../../GPUNoise/GPUNoise.hlsl"

			v2f vert (appdata v)
			{
				v2f o;

				// the code below is baked out and specialised from C:\Program Files\Unity\Editor\Data\CGIncludes\UnityCG.cginc
				// TRANSFER_SHADOW_COLLECTOR . it needs to be specialised because its rendering a quad from a Blit(), instead
				// of rendering real geometry from a worldspace camera. the world space rendering could probably be set up with
				// some hoop jumping but i guess ill go for this for now.

				o.pos = UnityObjectToClipPos(v.vertex);

				// world pos from [0,1] quad
				float4 wpos = float4(float3(v.vertex.x - 0.5, 0.0, v.vertex.y - 0.5) * _Scale * 4. + _CenterPos, 1.);

				// this could add wave height/disp??
				wpos.y = _OceanCenterPosWorld.y;

				o._WorldPosViewZ.xyz = wpos.xyz;
				o._WorldPosViewZ.w = dot(wpos.xyz - _CamPos, _CamForward);
				
				o._ShadowCoord0 = mul(unity_WorldToShadow[0], wpos).xyz;
				o._ShadowCoord1 = mul(unity_WorldToShadow[1], wpos).xyz;
				o._ShadowCoord2 = mul(unity_WorldToShadow[2], wpos).xyz;
				o._ShadowCoord3 = mul(unity_WorldToShadow[3], wpos).xyz;

				// working hard to get derivatives for shadow uvs, so that i can jitter the world position in the fragment shader. this
				// enables per-fragment noise (required to avoid wobble), and is required because each cascade has a different scale etc.
				o.ShadowCoord0_dxdz.xy = mul(unity_WorldToShadow[0], wpos + float4(1., 0., 0., 0.)).xz - o._ShadowCoord0.xz;
				o.ShadowCoord0_dxdz.zw = mul(unity_WorldToShadow[0], wpos + float4(0., 0., 1., 0.)).xz - o._ShadowCoord0.xz;
				o.ShadowCoord1_dxdz.xy = mul(unity_WorldToShadow[1], wpos + float4(1., 0., 0., 0.)).xz - o._ShadowCoord1.xz;
				o.ShadowCoord1_dxdz.zw = mul(unity_WorldToShadow[1], wpos + float4(0., 0., 1., 0.)).xz - o._ShadowCoord1.xz;
				o.ShadowCoord2_dxdz.xy = mul(unity_WorldToShadow[2], wpos + float4(1., 0., 0., 0.)).xz - o._ShadowCoord2.xz;
				o.ShadowCoord2_dxdz.zw = mul(unity_WorldToShadow[2], wpos + float4(0., 0., 1., 0.)).xz - o._ShadowCoord2.xz;
				o.ShadowCoord3_dxdz.xy = mul(unity_WorldToShadow[3], wpos + float4(1., 0., 0., 0.)).xz - o._ShadowCoord3.xz;
				o.ShadowCoord3_dxdz.zw = mul(unity_WorldToShadow[3], wpos + float4(0., 0., 1., 0.)).xz - o._ShadowCoord3.xz;

				o.MainCameraCoords = mul(_MainCameraProjectionMatrix, wpos);

				return o;
			}

			fixed ComputeShadow(in v2f i, in float jitterDiameter, in float4 cascadeWeights)
			{
				// Sadface - copy paste all this deprecated code in from Unity.cginc, because the
				// macro has a hardcoded return statement and i need the fade param for blending, and
				// i also added jitter

				if (jitterDiameter > 0.0)
				{
					half2 jitter = jitterDiameter * (hash33(uint3(abs(i._WorldPosViewZ.xz*10.), _Time.y*120.)) - 0.5).xy;
					i._ShadowCoord0.xz += i.ShadowCoord0_dxdz.xy * jitter.x + i.ShadowCoord0_dxdz.zw * jitter.y;
					i._ShadowCoord1.xz += i.ShadowCoord1_dxdz.xy * jitter.x + i.ShadowCoord1_dxdz.zw * jitter.y;
					i._ShadowCoord2.xz += i.ShadowCoord2_dxdz.xy * jitter.x + i.ShadowCoord2_dxdz.zw * jitter.y;
					i._ShadowCoord3.xz += i.ShadowCoord3_dxdz.xy * jitter.x + i.ShadowCoord3_dxdz.zw * jitter.y;
				}

				float4 coord = float4(
					i._ShadowCoord0 * cascadeWeights[0] + 
					i._ShadowCoord1 * cascadeWeights[1] + 
					i._ShadowCoord2 * cascadeWeights[2] + 
					i._ShadowCoord3 * cascadeWeights[3], 1);

				SAMPLE_SHADOW_COLLECTOR_SHADOW(coord)

				return shadow;
			}

			// Provides _SimDeltaTime (see comment at this definition)
			#include "SimHelpers.hlsl"

			fixed2 frag (v2f i) : SV_Target
			{
				fixed2 shadow = 0.;

				// Shadow from last frame - manually implement black border
				float2 uv_lastframe = LD_0_WorldToUV(i._WorldPosViewZ.xz);
				half2 r = abs(uv_lastframe.xy - 0.5);
				if (max(r.x, r.y) < 0.49)
				{
					SampleShadow(_LD_Sampler_Shadow_0, uv_lastframe, 1.0, shadow);
				}

				// Check if the current sample is visible in the main camera (and therefore shadow map can be sampled)
				float3 projected = i.MainCameraCoords.xyz / i.MainCameraCoords.w;
				if (projected.z < 1. && abs(projected.x) < 1. && abs(projected.z) < 1.)
				{
					// Sadface - copy paste all this deprecated code in from Unity.cginc, see similar comment above
					float3 fromCenter0 = i._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[0].xyz;
					float3 fromCenter1 = i._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[1].xyz;
					float3 fromCenter2 = i._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[2].xyz;
					float3 fromCenter3 = i._WorldPosViewZ.xyz - unity_ShadowSplitSpheres[3].xyz;
					float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));
					float4 cascadeWeights = float4(distances2 < unity_ShadowSplitSqRadii);
					cascadeWeights.yzw = saturate(cascadeWeights.yzw - cascadeWeights.xyz);
					float sphereDist = distance(i._WorldPosViewZ.xyz, unity_ShadowFadeCenterAndType.xyz);
					half shadowFade = saturate(sphereDist * _LightShadowData.z + _LightShadowData.w);

					fixed2 shadowThisFrame;
					shadowThisFrame.x = ComputeShadow(i, _JitterDiameters_CurrentFrameWeights.x, cascadeWeights);
					shadowThisFrame.y = ComputeShadow(i, _JitterDiameters_CurrentFrameWeights.y, cascadeWeights);
					shadowThisFrame = (fixed2)1. - saturate(shadowThisFrame + shadowFade);

					shadow = lerp(shadow, shadowThisFrame, _JitterDiameters_CurrentFrameWeights.zw * _SimDeltaTime * 60.);
				}

				return shadow;
			}
			ENDCG
		}
	}
}
