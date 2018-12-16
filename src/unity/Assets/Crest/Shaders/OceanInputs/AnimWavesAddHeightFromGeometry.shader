// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// This adds the height from the geometry. This allows setting the water height to some level for rivers etc, but still
// getting the waves added on top.

Shader "Ocean/Inputs/Animated Waves/Add Water Height From Geometry"
{
	Properties
	{
		[Enum(BlendOp)] _BlendOp("Blend Op", Int) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc("Src Blend Mode", Int) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeTgt("Tgt Blend Mode", Int) = 1
		[Enum(ColorWriteMask)] _ColorWriteMask("Color Write Mask", Int) = 15
	}

 	SubShader
	{
		Tags{ "Queue" = "Transparent" }

 		Pass
		{
			BlendOp [_BlendOp]
			Blend [_BlendModeSrc] [_BlendModeTgt]
			ColorMask [_ColorWriteMask]

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
				float addHeight = i.worldPos.y - _OceanCenterPosWorld.y;
				return half4(0., addHeight, 0., 1.);
			}
			ENDCG
		}
	}
}
