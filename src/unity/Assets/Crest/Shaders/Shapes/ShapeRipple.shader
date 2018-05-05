// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Shape/Ripple"
{
	Properties
	{
		_Amp("_Amp", range(-5,5)) = 1
		_Octaves("_Ocatves", range(1.0,40.0)) = 10.0
	}

	Category
	{
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

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 worldPos : TEXCOORD0;
					float3 localPos : TEXCOORD1;
				};

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );
					o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
					o.localPos = v.vertex.xyz;
					return o;
				}

				uniform float _Amp = 1.0;
				uniform float _Octaves = 10.0;

				float wave(float x, float k, float c, float t)
				{
					float X = x - c * t;
					return sin(k * X) * exp(-X * X);
				}

				// c.f. https://www.shadertoy.com/view/MtBSDG (1d), https://www.shadertoy.com/view/lt2XWy (2d)
				float dispersion2d(float2 v, float t) // testing FabriceNeyret2's waves dispersion function (source: https://www.shadertoy.com/view/MtBSDG)
				{
					float r = length(v);
					float sum = 0.0;
					for (float k = 1.0; k <= _Octaves; k++)
					{
						//sum += wave(abs(r), k, sqrt(k), t) / k; // dispersion for capillary waves (original)
						//sum += wave(r, k, sqrt(k), t) / k + wave(-r, k, sqrt(k), t) / k; // dispersion for capillary waves (no-"kink"-tweak)
						sum += wave(abs(r), k, 1.0 / sqrt(k), t) / k;// dispersion for gravity waves (original)
						//sum += wave(r, k, 1.0 / sqrt(k), t) / k + wave(-r, k, 1.0 / sqrt(k), t) / k;// dispersion for gravity waves (no-"kink"-tweak)
					}
    
					//return v.x < 0.0 ? n * sum / d : sum; // comparing variant *1 with variant *n/d looks rather similar in this scale
					return sum; // NOTE: using the simple 1d variant instead of the correct 2d variant here (looks similar in this scale)
				}


				float3 frag (v2f i) : SV_Target
				{
					// from -1 to 1
					float2 localPos = 2.*i.localPos;
	
					float life = 20.;
					float t = fmod(_Time.w, life);
	
					float2 scaledPos = 10. * localPos;
	
					float y = _Amp * dispersion2d( scaledPos, t );
	
					// gradually ramp down over time. exp is very agressive, smoothstep keeps the energy at the beginning.
					// need to be careful with this as it is hiding the shape, probably best to turn this off while experimenting.
					y *= smoothstep(life,0.,t);// exp(-.1*t);
	
					// chillax around the origin (prob needs a retweak)
					y *= smoothstep( 0., 1.5, length(scaledPos) );
	
					return float3( 0., y, 0. );
				}

				ENDCG
			}
		}
	}
}
