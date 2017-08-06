
// This code originated from Tomasz Dobrowolski's work
// https://www.shadertoy.com/view/Xsd3DB
// http://polycu.be/edit/?h=W2L7zN

// Creative Commons Attribution-ShareAlike (CC BY-SA)
// https://creativecommons.org/licenses/by-sa/4.0/

// A single Gerstner Octave
Shader "Ocean/Shape/Sim/2D Wave Equation"
{
	Properties
	{
		_Amplitude ("Amplitude", float) = 1
		_Wavelength("Wavelength", float) = 100
		_Angle ("Angle", range(-180, 180)) = 0
		_Speed ("Speed", float) = 10
		_Steepness ("Steepness", range(0, 5)) = 0.1
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
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 worldPos : TEXCOORD0;
					float2 texcoord : TEXCOORD1;
				};

				bool SamplingIsAdequate( float minWavelengthInShape )
				{
					return true;
				}

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );
					o.texcoord = v.texcoord;
					o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;

					// if wavelength is too small, kill this quad so that it doesnt render any shape
					if( !SamplingIsAdequate( 0.0 ) )
						o.vertex.xy *= 0.;

					return o;
				}

				// respects the gui option to freeze time
				uniform float _MyTime;

				uniform float _Amplitude;
				uniform float _Angle;
				uniform float _Speed;
				uniform float _Steepness;
				uniform sampler2D _WavePPTSource;
				uniform sampler2D _WavePPTSource_1;

				float4 frag (v2f i) : SV_Target
				{
					i.worldPos.y = 0.;

					float2 q = i.texcoord;

					float3 e = float3(float2(1., 1.) / _ScreenParams.xy, 0.);

					float4 c = tex2D(_WavePPTSource_1, q);

					float p11 = c.x;

					float p10 = tex2D(_WavePPTSource, q - e.zy).x;
					float p01 = tex2D(_WavePPTSource, q - e.xz).x;
					float p21 = tex2D(_WavePPTSource, q + e.xz).x;
					float p12 = tex2D(_WavePPTSource, q + e.zy).x;

					float d = 0.;

					// The actual propagation:
					d += -(p11 - .5)*2. + (p10 + p01 + p21 + p12 - 2.);
					d *= .99; // damping
					d = d*.5 + .5;

					if( frac( _Time.w / 12. ) < 0.05 )
					{
						d = smoothstep( 33., 30., length( i.worldPos-float3(15.,0.,10.) ) );
					}

					return float4( d, 0, 0, 0 );
				}

				ENDCG
			}
		}
	}
}
