// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Push water under the geometry. Needs to be rendered into all LODs - set Octave Wave length to 0.

Shader "Crest/Inputs/Animated Waves/Push Water Under Convex Hull"
{
	SubShader
	{
		Pass
		{
			BlendOp Min
			Cull Front

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"

			CBUFFER_START(CrestPerOceanInput)
			float _Weight;
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
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				// Write displacement to get from sea level of ocean to the y value of this geometry.

				// Write large XZ components - using min blending so this should not affect them.

				return half4(10000.0, _Weight * (input.worldPos.y - _OceanCenterPosWorld.y), 10000.0, 1.0);
			}
			ENDCG
		}
	}
}
