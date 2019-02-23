// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Persistent foam sim

Shader "Hidden/Ocean/Simulation/Update Foam"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../../../../Crest/Shaders/OceanLODData.hlsl"

			struct Attributes
			{
				float4 positionCS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 uv_uv_lastframe : TEXCOORD0;
				float2 worldXZ : TEXCOORD1;
			};

			float _SimDeltaTime;
			float _SimDeltaTimePrev;

			Varyings Vert(Attributes input)
			{
				Varyings o;

				o.positionCS = input.positionCS;
				o.uv_uv_lastframe.xy = input.uv;

				// lod data 1 is current frame, compute world pos from quad uv
				o.worldXZ = LD_1_UVToWorld(input.uv);
				o.uv_uv_lastframe.zw = LD_0_WorldToUV(o.worldXZ);

				return o;
			}

			// respects the gui option to freeze time
			half _FoamFadeRate;
			half _WaveFoamStrength;
			half _WaveFoamCoverage;
			half _ShorelineFoamMaxDepth;
			half _ShorelineFoamStrength;

			half Frag(Varyings input) : SV_Target
			{
				float4 uv = float4(input.uv_uv_lastframe.xy, 0., 0.);
				float4 uv_lastframe = float4(input.uv_uv_lastframe.zw, 0., 0.);
				// #if _FLOW_ON
				half4 velocity = half4(tex2Dlod(_LD_Sampler_Flow_1, uv).xy, 0., 0.);
				half foam = tex2Dlod(_LD_Sampler_Foam_0, uv_lastframe
					- ((_SimDeltaTime * _LD_Params_0.w) * velocity)
				).x;
				// #else
				// // sampler will clamp the uv currently
				// half foam = tex2Dlod(_LD_Sampler_Foam_0, uv_lastframe).x;
				// #endif
				half2 r = abs(uv_lastframe.xy - 0.5);
				if (max(r.x, r.y) > 0.5 - _LD_Params_0.w)
				{
					// no border wrap mode for RTs in unity it seems, so make any off-texture reads 0 manually
					foam = 0.;
				}

				// fade
				foam *= max(0.0, 1.0 - _FoamFadeRate * _SimDeltaTime);

				// sample displacement texture and generate foam from it
				const float3 dd = float3(_LD_Params_1.w, 0.0, _LD_Params_1.x);
				half3 s = tex2Dlod(_LD_Sampler_AnimatedWaves_1, uv).xyz;
				half3 sx = tex2Dlod(_LD_Sampler_AnimatedWaves_1, uv + dd.xyyy).xyz;
				half3 sz = tex2Dlod(_LD_Sampler_AnimatedWaves_1, uv + dd.yxyy).xyz;
				float3 disp = s.xyz;
				float3 disp_x = dd.zyy + sx.xyz;
				float3 disp_z = dd.yyz + sz.xyz;
				// The determinant of the displacement Jacobian is a good measure for turbulence:
				// > 1: Stretch
				// < 1: Squash
				// < 0: Overlap
				float4 du = float4(disp_x.xz, disp_z.xz) - disp.xzxz;
				float det = (du.x * du.w - du.y * du.z) / (_LD_Params_1.x * _LD_Params_1.x);
				foam += 5. * _SimDeltaTime * _WaveFoamStrength * saturate(_WaveFoamCoverage - det);

				// add foam in shallow water. use the displaced position to ensure we add foam where world objects are.
				float4 uv_1_displaced = float4(LD_1_WorldToUV(input.worldXZ + disp.xz), 0., 1.);
				float signedOceanDepth = DEPTH_BASELINE - tex2Dlod(_LD_Sampler_SeaFloorDepth_1, uv_1_displaced).x + disp.y;
				foam += _ShorelineFoamStrength * _SimDeltaTime * saturate(1. - signedOceanDepth / _ShorelineFoamMaxDepth);

				return foam;
			}
			ENDCG
		}
	}
}
