// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders ocean depth - signed distance from sea level to sea floor
Shader "Ocean/Ocean Depth"
{
	Properties
	{
	}

	Category
	{
		Tags { "Queue"="Geometry" }

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode" = "Always" }
				Blend Off

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float altitude : TEXCOORD0;
				};

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );

					float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.altitude = worldPos.y;

					return o;
				}

				float4 frag (v2f i) : SV_Target
				{
					const float seaLevel = 0.;
					float depth = seaLevel - i.altitude;
					return (float4)depth;
				}

				ENDCG
			}
		}
	}
}
