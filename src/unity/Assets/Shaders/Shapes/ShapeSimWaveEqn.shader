
// This code originated from Tomasz Dobrowolski's work
// https://www.shadertoy.com/view/Xsd3DB
// http://polycu.be/edit/?h=W2L7zN

// Creative Commons Attribution-ShareAlike (CC BY-SA)
// https://creativecommons.org/licenses/by-sa/4.0/

// solve 2D wave equation
Shader "Ocean/Shape/Sim/2D Wave Equation"
{
	Properties
	{
	}

	Category
	{
		Tags { "Queue"="Geometry" }

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode" = "Always" }

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"
				#define PI 3.141592653

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 worldPos : TEXCOORD0;
					float2 clipPos : TEXCOORD1;
				};

				bool SamplingIsAdequate( float minWavelengthInShape )
				{
					return true;
				}

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );
					o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;

					o.clipPos = o.vertex.xy;
					o.clipPos.y = -o.clipPos.y;
					o.clipPos = 0.5*o.clipPos + 0.5;

					// if wavelength is too small, kill this quad so that it doesnt render any shape
					if( !SamplingIsAdequate( 0.0 ) )
						o.vertex.xy *= 0.;

					return o;
				}

				// respects the gui option to freeze time
				uniform float _MyTime;

				uniform sampler2D _WavePPTSource;
				uniform sampler2D _WavePPTSource_Prev;

				float4 frag (v2f i) : SV_Target
				{
					i.worldPos.y = 0.;

					float2 q = i.clipPos;
					float3 e = float3(float2(1., 1.) / _ScreenParams.xy, 0.);

					float p11 = tex2D( _WavePPTSource_Prev, q ).y;
					float p10 = tex2D(_WavePPTSource, q - e.zy).y;
					float p01 = tex2D(_WavePPTSource, q - e.xz).y;
					float p21 = tex2D(_WavePPTSource, q + e.xz).y;
					float p12 = tex2D(_WavePPTSource, q + e.zy).y;

					// The actual propagation:
					float d = ((p10 + p01 + p21 + p12) / 2. - p11);
					// Damping
					d *= .99;

					if( frac( _Time.w / 12. ) < 0.05 )
					{
						d = 40.*smoothstep( 33., 30., length( i.worldPos-float3(15.,0.,10.) ) );
					}

					//d *= smoothstep( 10.9, 11., length( i.worldPos + float3(22., 0., 18.) ) );

					return float4( 0., d, 0., 1. );
				}

				ENDCG
			}
		}
	}
}
