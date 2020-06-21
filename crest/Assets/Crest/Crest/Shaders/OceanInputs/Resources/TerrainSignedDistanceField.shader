// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders ocean depth - signed distance from sea level to sea floor
Shader "Crest/Inputs/Depth/Initialise Signed Distance Field From Geometry"
{
	SubShader
	{
		Pass
		{
			BlendOp Min

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			#include "../../OceanGlobals.hlsl"

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float depth : TEXCOORD0;
				float2 worldPosXZ : TEXCOORD1;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = UnityObjectToClipPos(input.positionOS);

				float3 worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));

				output.depth = _OceanCenterPosWorld.y - worldPos.y;
				output.worldPosXZ = worldPos.xz;


				return output;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				float depth    = input.depth;
				float2 position = depth <= 0.0 ? input.worldPosXZ : float2(0.0, 0.0);
				return float4(depth, 0.0, position.x, position.y);
			}
			ENDCG
		}
	}
}
