// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Persistent foam sim for shorelines

Shader "Ocean/Shape/Sim/Shoreline Foam"
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
				#include "../../../Crest/Shaders/OceanLODData.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float4 uv_uv_lastframe : TEXCOORD0;
					float invRes : TEXCOORD1;
				};

				#include "Resources/SimHelpers.cginc"

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					float3 world = mul(unity_ObjectToWorld, v.vertex);
					ComputeUVs(world, o.vertex.xy, o.uv_uv_lastframe.zw, o.uv_uv_lastframe.xy, o.invRes);

					return o;
				}

				uniform half _FoamFadeRate;
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

					// add foam in shallow water
					half3 disp = tex2Dlod(_LD_Sampler_AnimatedWaves_1, uv).xyz;
					float signedOceanDepth = tex2Dlod(_LD_Sampler_SeaFloorDepth_1, uv).x + DEPTH_BIAS + disp.y;
					foam += _ShorelineFoamStrength * _SimDeltaTime * saturate(1. - signedOceanDepth / _ShorelineFoamMaxDepth);

					return foam;
				}
				ENDCG
			}
		}
	}
}
