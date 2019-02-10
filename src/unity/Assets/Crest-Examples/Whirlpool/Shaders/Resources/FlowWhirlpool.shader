// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Inputs/Flow/Whirlpool"
{
	Category
	{
		// base simulation runs on the Geometry queue, before this shader.
		// this shader adds interaction forces on top of the simulation result.
		Tags { "Queue"="Transparent" }

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode" = "Always" }
				Blend One One

				CGPROGRAM
				#pragma vertex Vert
				#pragma fragment Frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"

				struct Attributes
				{
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct Varyings
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD1;
				};

				float _EyeRadiusProportion;
				float _MaxSpeed;

				Varyings Vert(Attributes input)
				{
					Varyings o;
					o.vertex = UnityObjectToClipPos(input.vertex);
					o.uv = input.texcoord;

					return o;
				}

				float2 Frag(Varyings input) : SV_Target
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

					return flow;
				}

				ENDCG
			}
		}
	}
}
