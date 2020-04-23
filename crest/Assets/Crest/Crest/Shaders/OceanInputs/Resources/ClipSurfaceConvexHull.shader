// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders convex hull to the clip surface texture.

Shader "Crest/Inputs/Clip Surface/Convex Hull"
{
	SubShader
	{
		ZWrite Off
		ColorMask R

		Pass
		{
			Cull Front

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanVertHelpers.hlsl"
			#include "../../OceanHelpersNew.hlsl"
			#include "../../OceanHelpersDriven.hlsl"

			CBUFFER_START(CrestPerOceanInput)
			uint _DisplacementSamplingIterations;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
				return o;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				float3 surfacePositionWS = SampleOceanDataDisplacedToWorldPosition
				(
					_LD_TexArray_AnimatedWaves,
					input.positionWS,
					_DisplacementSamplingIterations
				);

				// Move to sea level
				surfacePositionWS.y += _OceanCenterPosWorld.y;

				// Write red if underwater
				if (input.positionWS.y > surfacePositionWS.y)
				{
					clip(-1);
				}
				return float4(1, 0, 0, 1);
			}
			ENDCG
		}

		Pass
		{
			Cull Back

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanVertHelpers.hlsl"
			#include "../../OceanHelpersNew.hlsl"
			#include "../../OceanHelpersDriven.hlsl"

			CBUFFER_START(CrestPerOceanInput)
			uint _DisplacementSamplingIterations;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
				return o;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				float3 surfacePositionWS = SampleOceanDataDisplacedToWorldPosition
				(
					_LD_TexArray_AnimatedWaves,
					input.positionWS,
					_DisplacementSamplingIterations
				);

				// Move to sea level
				surfacePositionWS.y += _OceanCenterPosWorld.y;

				// Write black if underwater
				if (input.positionWS.y > surfacePositionWS.y)
				{
					clip(-1);
				}
				return float4(0, 0, 0, 1);
			}
			ENDCG
		}
	}
}
