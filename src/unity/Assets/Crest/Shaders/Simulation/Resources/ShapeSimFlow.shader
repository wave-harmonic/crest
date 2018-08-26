// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// persistent flow sim
Shader "Ocean/Shape/Sim/Flow"
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
					float3 world : TEXCOORD2;
				};

				#include "SimHelpers.cginc"

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					float3 world = mul(unity_ObjectToWorld, v.vertex);
					ComputeUVs(world, o.vertex.xy, o.uv_uv_lastframe.zw, o.uv_uv_lastframe.xy, o.invRes);
					o.world = world;

					return o;
				}

				uniform half _FlowSpeed;

				half2 frag(v2f i) : SV_Target
				{
					float2 flow;
					flow.x = -i.world.z;
					flow.y = i.world.x;
					return normalize(flow) * _FlowSpeed;
				}
				ENDCG
			}
		}
	}
}
