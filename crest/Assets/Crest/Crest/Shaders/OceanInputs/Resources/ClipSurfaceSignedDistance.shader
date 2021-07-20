// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders a signed distance shape into the clip surface texture.

Shader "Crest/Inputs/Clip Surface/Signed Distance"
{
	SubShader
	{
		ZWrite Off
		ColorMask R
		BlendOp Max

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
			float _SignedDistanceSphere;
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

			// Taken from:
			// https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
			float signedDistanceSphere(float3 position, float radius)
			{
				return length(position) - radius;
			}

			Varyings Vert(Attributes input)
			{
				Varyings output;

				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));

				return output;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				float3 surfacePositionWS = SampleOceanDataDisplacedToWorldPosition
				(
					_LD_TexArray_AnimatedWaves,
					input.positionWS,
					_DisplacementSamplingIterations
				);

				// Move to sea level.
				surfacePositionWS.y += _OceanCenterPosWorld.y;

				// We only need the height as clip surface is sampled at the displaced position in the ocean shader.
				float3 positionWS = input.positionWS;
				positionWS.y -= surfacePositionWS.y;

				// Align position with input position.
				float4 objectOrigin = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0));
				positionWS.xz -= objectOrigin.xz;

				float signedDistance = signedDistanceSphere(positionWS, _SignedDistanceSphere);

				// Bring data to the zero to one range.
				signedDistance += 0.5;

				return float4(1.0 - signedDistance, 0.0, 0.0, 1.0);
			}
			ENDCG
		}
	}
}
