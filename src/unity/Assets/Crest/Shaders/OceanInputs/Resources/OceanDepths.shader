// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders ocean depth - signed distance from sea level to sea floor
Shader "Crest/Inputs/Depth/Ocean Depth From Geometry"
{
	SubShader
	{
		Pass
		{
			BlendOp Max

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
				float depth : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);

				float altitude = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).y;

				// Depth is altitude above 1000m below sea level. This is because '0' needs to signify deep water.
				// I originally used a simple bias in the depth texture but it would still produce shallow water outside
				// the biggest LOD texture where the depth would evaluate to 0 in the ocean vert shader, so i've transformed
				// 0 to mean deep below the surface.
				o.depth = altitude - (_OceanCenterPosWorld.y - CREST_OCEAN_DEPTH_BASELINE);

				return o;
			}

			float Frag(Varyings input) : SV_Target
			{
				return input.depth;
			}
			ENDCG
		}
	}
}
