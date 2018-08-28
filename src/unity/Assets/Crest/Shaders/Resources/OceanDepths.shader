// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders ocean depth - signed distance from sea level to sea floor
Shader "Ocean/Ocean Depth"
{
	Properties
	{
	}

	Category
	{
		Tags { "Queue" = "Geometry" }

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode" = "Always" }
				BlendOp Min

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"
				#include "../OceanLODData.cginc"
		
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

					//depth bias is an optimisation which allows the depth data to be initialised once to 0 without generating foam everywhere.
					o.depth = _OceanCenterPosWorld.y - altitude - DEPTH_BIAS;

					return o;
				}

				half frag (v2f i) : SV_Target
				{
					return i.depth;
				}

				ENDCG
			}
		}
	}
}
