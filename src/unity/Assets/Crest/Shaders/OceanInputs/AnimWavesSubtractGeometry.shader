// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Push water under the geometry. Needs to be rendered into all LODs - set Octave Wave length to 0.

Shader "Ocean/Inputs/Animated Waves/Subtract Geometry"
{
	Properties
	{
	}

 	SubShader
	{
		Tags{ "Queue" = "Transparent" }
		LOD 100
 		Pass
		{
			BlendOp Min
			Cull Front

 			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

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

 			half4 frag (v2f i) : SV_Target
			{
				// Write displacement to get from sea level of ocean to the y value of this geometry
				return half4(1000., i.worldPos.y - _OceanCenterPosWorld.y, 1000., 1.);
			}
			ENDCG
		}
	}
}
