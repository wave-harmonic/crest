// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Shape/Heightfield"
{
	Properties
	{
		_Amplitude("Amplitude", float) = 1
		_HorizScale("HorizScale", float) = 1
		_HeightfieldTexture("Heightfield", 2D) = "black" {}
		_HeightfieldPrevTexture("Heightfield Prev", 2D) = "black" {}
	}

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
				//Blend One One
			
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 texcoord : TEXCOORD0;
				};

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );
					float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.texcoord = worldPos.xz;

					//const float texSize = 2. * unity_OrthoParams.x / _ScreenParams.x;
					//if (texSize > 2.)
					//	o.vertex.xy *= 0.;

					//const float targetTexSize = 2.;
					//float r = texSize / targetTexSize;
					//if (texSize < 1. || texSize > 2.) o.vertex.xy *= 0.;
					//if (_ScreenParams.x < 256. && (r < 0.5 || r >= 2.))
					//	o.vertex.xy *= 0.;

					return o;
				}

				uniform float _Amplitude;
				uniform float _MyDeltaTime;
				uniform float _HorizScale;
				uniform sampler2D _HeightfieldTexture;
				uniform sampler2D _HeightfieldPrevTexture;

				float4 frag (v2f i) : SV_Target
				{
					const float texSize = 2. * unity_OrthoParams.x / _ScreenParams.x;
					i.texcoord /= texSize;

					float h = 4. * (tex2D(_HeightfieldTexture, i.texcoord / _HorizScale).y - 0.65); // 0.65 seems to be required to stop the surface oscillating around sea level
					float hprev = 4. * (tex2D(_HeightfieldPrevTexture, i.texcoord / _HorizScale).y - 0.65);
					hprev = h + (hprev - h)*.75;
					return _Amplitude * _MyDeltaTime * _MyDeltaTime * float4(h, hprev, 0., 0.); // / sqrt(texSize);
				}

				ENDCG
			}
		}
	}
}
