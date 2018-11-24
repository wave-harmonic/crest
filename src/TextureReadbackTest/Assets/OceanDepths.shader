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
				//BlendOp Min

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"

				#define SEA_LEVEL 0.
				#define DEPTH_BIAS 100.

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					half depth : TEXCOORD0;
				};

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );

					half altitude = mul(unity_ObjectToWorld, v.vertex).y;
					//altitude += 75. * sin(0.5*_Time.w);

					for (int i = 0; i < 200000; i++)
					{
						altitude += 5.*sin(_Time.w + 0.01*(float)i) / 200000.;
						altitude += 5.*cos(_Time.w + 0.01*(float)i) / 200000.;
						altitude += 5.*log(_Time.w + 0.01*(float)i) / 200000.;
						altitude += 5.*sin(_Time.w + 0.02*(float)i) / 200000.;
						altitude += 5.*cos(_Time.w + 0.03*(float)i) / 200000.;
						altitude += 5.*log(_Time.w + 0.04*(float)i) / 200000.;
					}
					//depth bias is an optimisation which allows the depth data to be initialised once to 0 without generating foam everywhere.
					o.depth = altitude;// / 100.;
					//o.depth = SEA_LEVEL - altitude - DEPTH_BIAS;

					return o;
				}

				half4 frag (v2f i) : SV_Target
				{
					return half4(10.*cos(_Time.w),i.depth,10.*sin(_Time.w),1.);
				}

				ENDCG
			}
		}
	}
}
