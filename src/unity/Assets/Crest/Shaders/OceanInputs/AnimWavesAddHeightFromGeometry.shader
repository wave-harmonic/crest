// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// This adds the height from the geometry. This allows setting the water height to some level for rivers etc, but still
// getting the waves added on top.

Shader "Ocean/Inputs/Animated Waves/Add Water Height From Geometry"
{
	Properties
	{
		[Toggle] _AddHeightsBelowSeaLevel("Add Heights Below Sea Level", Float) = 1.0
	}

 	SubShader
	{
		Tags{ "Queue" = "Transparent" }
		LOD 100
 		Pass
		{
			Blend One One

 			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _ADDHEIGHTSBELOWSEALEVEL_ON

 			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"

 			struct appdata
			{
				float4 vertex : POSITION;
			};

 			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

 			v2f vert (appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				return o;
			}

			uniform float _AddHeightsBelowSeaLevel;

 			half4 frag (v2f i) : SV_Target
			{
				float addHeight = i.worldPos.y - _OceanCenterPosWorld.y;

				#if !_ADDHEIGHTSBELOWSEALEVEL_ON
				addHeight = max(addHeight, 0.);
				#endif // _ADDHEIGHTSBELOWSEALEVEL_ON

				// Write displacement to get from sea level of ocean to the y value of this geometry
				return half4(0., addHeight, 0., 1.);
			}
			ENDCG
		}
	}
}
