Shader "Ocean/ShadowUpdate"
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
			#include "../../../../Crest/Shaders/OceanLODData.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				V2F_SHADOW_COLLECTOR;
			};

			uniform float3 _CenterPos;
			uniform float3 _Scale;
			uniform float3 _CamPos;
			uniform float3 _CamForward;

			v2f vert (appdata v)
			{
				v2f o;

				// the code below is baked out and specialised from C:\Program Files\Unity\Editor\Data\CGIncludes\UnityCG.cginc
				// TRANSFER_SHADOW_COLLECTOR

				o.pos = UnityObjectToClipPos(v.vertex);

				// world pos from [0,1] quad
				float4 wpos = float4(float3(v.vertex.x - 0.5, 0.0, v.vertex.y - 0.5) * _Scale.xzy * 4. + _CenterPos, 1.);

				// TODO maybe wave height/disp??
				wpos.y = _OceanCenterPosWorld.y;

				o._WorldPosViewZ.xyz = wpos.xyz;
				o._WorldPosViewZ.w = dot(wpos.xyz - _CamPos, _CamForward);

				o._ShadowCoord0 = mul(unity_WorldToShadow[0], wpos).xyz;
				o._ShadowCoord1 = mul(unity_WorldToShadow[1], wpos).xyz;
				o._ShadowCoord2 = mul(unity_WorldToShadow[2], wpos).xyz;
				o._ShadowCoord3 = mul(unity_WorldToShadow[3], wpos).xyz;

				return o;
			}

			// sadface - override macro used within SHADOW_COLLECTOR_FRAGMENT
			#undef COMPUTE_SHADOW_COLLECTOR_SHADOW
			#define COMPUTE_SHADOW_COLLECTOR_SHADOW(i, weights, shadowFade) \
				float4 coord = float4(i._ShadowCoord0 * weights[0] + i._ShadowCoord1 * weights[1] + i._ShadowCoord2 * weights[2] + i._ShadowCoord3 * weights[3], 1); \
				SAMPLE_SHADOW_COLLECTOR_SHADOW(coord) \
				float res; \
				res = saturate(shadow);

			float ComputeShadow(v2f i, out float o_shadowFade)
			{
				SHADOW_COLLECTOR_FRAGMENT(i);

				o_shadowFade = shadowFade;

				return res;
			}

			float frag (v2f i) : SV_Target
			{
				float2 uv_lastframe = LD_0_WorldToUV(i._WorldPosViewZ.xz);

				half lastShadow = 0.;
				SampleShadow(_LD_Sampler_Shadow_0, uv_lastframe, 1.0, lastShadow);

				float shadowFade;
				float result = ComputeShadow(i, shadowFade).x;

				float amountOfThisFrame = .02;
				return lerp(lastShadow, 1. - result, amountOfThisFrame * (1. - shadowFade));
			}
			ENDCG
		}
	}
}
