// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Simulation/Combine Animated Wave LODs"
{
	SubShader
	{
		// No culling or depth
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma multi_compile __ _DYNAMIC_WAVE_SIM_ON
			#pragma multi_compile __ _FLOW_ON

			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"

			float _HorizDisplace;
			float _DisplaceClamp;
			float _CrestTime;

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.uv = input.uv;
				return o;
			}

#include "ShapeCombineFunction.hlsl"

			half4 Frag(Varyings input) : SV_Target
			{
				// go from uv out to world for the current shape texture
				const float2 worldPosXZ = LD_0_UVToWorld(input.uv);

				// sample the shape 1 texture at this world pos
				const float2 uv_1 = LD_1_WorldToUV(worldPosXZ);

				return ShapeCombineFunction(input.uv, uv_1, worldPosXZ);
			}
			ENDCG
		}
	}
}
