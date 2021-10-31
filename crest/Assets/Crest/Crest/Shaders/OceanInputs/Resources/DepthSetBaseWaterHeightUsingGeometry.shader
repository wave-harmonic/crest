// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// This sets base water height to Y value of geometry.

Shader "Crest/Inputs/Sea Floor Depth/Set Base Water Height Using Geometry"
{
	SubShader
	{
		Pass
		{
			Blend Off

			// Second channel is sea level offset
			ColorMask G

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../../OceanGlobals.hlsl"

			CBUFFER_START(CrestPerOceanInput)
			float _Weight;
			float3 _DisplacementAtInputPosition;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				o.worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement
				o.worldPos.xz -= _DisplacementAtInputPosition.xz;

				o.positionCS = mul(UNITY_MATRIX_VP, float4(o.worldPos, 1.0));

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				// Write displacement to get from sea level of ocean to the y value of this geometry
				const float heightOffset = input.worldPos.y - _OceanCenterPosWorld.y;
				return half4(0.0, heightOffset, 0.0, _Weight);
			}
			ENDCG
		}
	}
}
