// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// persistent foam sim
Shader "Ocean/Shape/Sim/Foam"
{
	Properties {
	}

	Category
	{
		// Base simulation runs first on geometry queue, no blending.
		// Any interactions will additively render later in the transparent queue.
		Tags { "Queue" = "Geometry" }

		SubShader {
			Pass {

				Name "BASE"
				Tags{ "LightMode" = "Always" }

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog

				#include "UnityCG.cginc"
				#include "../../../../Crest/Shaders/OceanLODData.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float4 uv_uv_lastframe : TEXCOORD0;
					float invRes : TEXCOORD1;
				};

				#include "SimHelpers.cginc"

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					float3 world = mul(unity_ObjectToWorld, v.vertex);
					ComputeUVs(world, o.vertex.xy, o.uv_uv_lastframe.zw, o.uv_uv_lastframe.xy, o.invRes);

					return o;
				}

				// respects the gui option to freeze time
				uniform half _FoamFadeRate;
				uniform half _WaveFoamStrength;
				uniform half _WaveFoamCoverage;
				uniform half _ShorelineFoamMaxDepth;
				uniform half _ShorelineFoamStrength;

				half frag(v2f i) : SV_Target
				{
					float4 uv = float4(i.uv_uv_lastframe.xy, 0., 0.);
					float4 uv_lastframe = float4(i.uv_uv_lastframe.zw, 0., 0.);

					// sampler will clamp the uv currently
					half foam = tex2Dlod(_LD_Sampler_Foam_0, uv_lastframe).x;
					//return foam + sin(_Time.w)*.004;
					half2 r = abs(uv_lastframe.xy - 0.5);
					if (max(r.x, r.y) > 0.5 - i.invRes)
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

					// add foam in shallow water
					float signedOceanDepth = tex2Dlod(_LD_Sampler_SeaFloorDepth_1, uv).x + DEPTH_BIAS + disp.y;
					foam += _ShorelineFoamStrength * _SimDeltaTime * saturate(1. - signedOceanDepth / _ShorelineFoamMaxDepth);

					return foam;
				}
				ENDCG
			}
		}
	}
}
