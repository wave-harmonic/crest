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
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"
				#include "../../../Crest/Shaders/MultiscaleShape.hlsl"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD1;
				};

				uniform float _EyeRadiusProportion;
				uniform float _MaxSpeed;

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );
					o.uv = v.texcoord;

					return o;
				}

				float2 frag (v2f i) : SV_Target
				{
					float2 col = float2(0, 0);
					float2 uv_from_cent = (i.uv - float2(.5, .5)) * 2.;

					float r       = _EyeRadiusProportion;
					const float R = 1;
					float2 o      = float2(0, 0);
					float  s      = .2;            // whirlpool 'swirlyness', can vary from 0 - 1
					float2 p      = uv_from_cent;
					float  V      = _MaxSpeed;

					float2 PtO  =       o - p;
					float  lPtO = length(PtO);

					if(lPtO >= R) {
						col = float2(0,0);
					} else if (lPtO <= r) {
						col = float2(0,0);
					} else {
						float c = 1.0 - ((lPtO - r) / (R - r));
						// dynamically calvulate current value of velocity field
						// (TODO: Make this a texture lookup?)
						float2 v = V * c * normalize(
							(s * c * normalize(float2(-PtO.y, PtO.x))) +
							((s - 1.0) * (c - 1.0) * normalize(PtO))
						);
						col = v;
					}
					return col;
				}

				ENDCG
			}
		}
	}
}
