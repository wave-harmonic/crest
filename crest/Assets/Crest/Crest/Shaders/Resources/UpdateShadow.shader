// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Soft shadow term is red, hard shadow term is green.

Shader "Hidden/Crest/Simulation/Update Shadow"
{
	SubShader
	{
		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma multi_compile_shadowcollector

			// #pragma enable_d3d11_debug_symbols

			#define CREST_SAMPLE_SHADOW_HARD

			#include "UnityCG.cginc"

			#include "../FullScreenTriangle.hlsl"
			#include "../Helpers/BIRP/ScreenSpaceShadows.hlsl"
			#include "../ShaderLibrary/UpdateShadow.hlsl"

			half CrestSampleShadows(const float4 i_positionWS)
			{
				// NOTE: "Shadow Projection > Close Fit" can still produce artefacts when away from caster, but this
				// appears to be an improvement over the compute shader.
				float4 shadowCoord = GET_SHADOW_COORDINATES(i_positionWS);
				half shadows = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, shadowCoord);
				shadows = lerp(_LightShadowData.r, 1.0, shadows);

				return shadows;
			}

			half CrestComputeShadowFade(const float4 i_positionWS)
			{
				float z = dot(i_positionWS.xyz - _WorldSpaceCameraPos.xyz, unity_CameraToWorld._m02_m12_m22);
				float fadeDistance = UnityComputeShadowFadeDistance(i_positionWS.xyz, z);
				float fade = UnityComputeShadowFade(fadeDistance);
				return fade;
			}
			ENDHLSL
		}
	}
}
