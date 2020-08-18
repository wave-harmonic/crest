// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Flow/Whirlpool"
{
	SubShader
	{
		Pass
		{
			Tags { "DisableBatching" = "True" }
			Blend One One

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD1;
			};

			float _EyeRadiusProportion;
			float _MaxSpeed;

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.uv = input.uv;
				return o;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				float2 flow = float2(0.0, 0.0);

				float2 pointToCenter = (float2(0.5, 0.5) - input.uv) * 2.0;
				float  distToCenter2 = dot(pointToCenter, pointToCenter);

				if (distToCenter2 < 1.0 && distToCenter2 > _EyeRadiusProportion * _EyeRadiusProportion)
				{
					float distToCenter = sqrt(distToCenter2);

					float centerProp = 1.0 - (distToCenter - _EyeRadiusProportion) / (1.0 - _EyeRadiusProportion);
					pointToCenter /= distToCenter;

					// Whirlpool 'swirlyness', can vary from 0 - 1
					const float swirl = 0.6;

					// Dynamically calculate current value of velocity field
					flow = _MaxSpeed * centerProp * normalize(
						swirl * centerProp * float2(-pointToCenter.y, pointToCenter.x) +
						(swirl - 1.0) * (centerProp - 1.0) * pointToCenter
					);
				}

				return float4(flow, 0.0, 0.0);
			}
			ENDCG
		}
	}
}
