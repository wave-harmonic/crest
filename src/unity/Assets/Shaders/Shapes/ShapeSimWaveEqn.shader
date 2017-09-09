
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
					float2 uv : TEXCOORD1;
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

					o.uv = o.vertex.xy;
					o.uv.y = -o.uv.y;
					o.uv = 0.5*o.uv + 0.5;

					// if wavelength is too small, kill this quad so that it doesnt render any shape
					if( !SamplingIsAdequate( 0.0 ) )
						o.vertex.xy *= 0.;

					return o;
				}

				// respects the gui option to freeze time
				uniform float _MyTime;

				uniform sampler2D _WavePPTSource;
				uniform sampler2D _WavePPTSourceNextScale;
				uniform sampler2D _WavePPTSourcePrevScale;
				
				uniform sampler2D _WavePPTSource_Prev;

				uniform float _IsSmallestScale = 0.0;
				bool IsSmallestScale() { return _IsSmallestScale == 1.0; }

				float SampleCurrentWaveHeight( float2 uv )
				{
					uv -= 0.5;

					float2 sgn = sign( uv );

					uv *= sgn;

					bool outside = uv.x > 0.5 || uv.y > 0.5;
					bool inside = !IsSmallestScale() && uv.x < 0.25 && uv.y < 0.25;

					if( outside )
					{
						uv *= 0.5 * sgn;
						uv += 0.5;

						return tex2D( _WavePPTSourceNextScale, uv ).y;
					}

					if( inside )
					{
						uv *= 2.0 * sgn;
						uv += 0.5;

						return tex2D( _WavePPTSourcePrevScale, uv ).y;
					}

					uv *= sgn;
					uv += 0.5;

					return tex2D( _WavePPTSource, uv ).y;
				}

				float4 frag (v2f i) : SV_Target
				{
					i.worldPos.y = 0.;

					float2 q = i.uv;
					float3 e = float3(float2(1., 1.) / _ScreenParams.xy, 0.);

					if( !IsSmallestScale() )
					{
						float2 uv = abs( i.uv - 0.5 );
						if( uv.x < 0.125 && uv.y < 0.125 )
							return (float4)0.;
					}

					float p11 = tex2D( _WavePPTSource_Prev, q ).y;
					float p10 = SampleCurrentWaveHeight( q - e.zy );
					float p01 = SampleCurrentWaveHeight( q - e.xz );
					float p21 = SampleCurrentWaveHeight( q + e.xz );
					float p12 = SampleCurrentWaveHeight( q + e.zy );

					// The actual propagation:
					float d = ((p10 + p01 + p21 + p12) / 2. - p11);
					// Damping
					d *= .99;

					if( frac( _Time.w / 12. ) < 0.05 )
					{
						float s = .4;
						d = 80.*smoothstep( s*33., s*30., length( i.worldPos-0.*float3(15.,0.,10.) ) );
					}

					//d *= smoothstep( 10.9, 11., length( i.worldPos + float3(22., 0., 18.) ) );

					return float4( 0., d, 0., 1. );
				}

				ENDCG
			}
		}
	}
}
