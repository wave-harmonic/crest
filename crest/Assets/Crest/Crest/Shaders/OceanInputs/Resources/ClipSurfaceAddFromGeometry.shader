// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders the geometry to the clip surface texture.

Shader "Crest/Inputs/Clip Surface/Add From Geometry"
{
	SubShader
	{
		Pass
		{
			Blend One One
			Cull Off
			ColorMask RG

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../../OceanLODData.hlsl"

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 depth : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				float heightWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).y;

				// It would be better if we had the surface height.
				float difference = heightWS - _OceanCenterPosWorld.y;

				// We are bookending values since waves can only be so high. 5 is temporary.
				if (heightWS > _OceanCenterPosWorld.y)
				{
					o.depth = float2(min(difference, 5) / 5, 0);
				}
				else if (heightWS < _OceanCenterPosWorld.y)
				{
					difference = -difference;
					o.depth = float2(0, min(difference, 5) / 5);
				}

				return o;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				return float4(input.depth.x, input.depth.y, 0, 1);
			}
			ENDCG
		}
	}
}
