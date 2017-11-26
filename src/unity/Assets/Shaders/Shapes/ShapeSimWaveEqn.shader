
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

				float4 frag (v2f i) : SV_Target
				{
					i.worldPos.y = 0.;

					float2 q = i.uv;
					float3 e = float3(float2(1., 1.) / _ScreenParams.xy, 0.);

					float2 ft_ftm = tex2D(_WavePPTSource, q).xy;
					float ft = ft_ftm.x; // t - current value before update
					float ftm = ft_ftm.y; // t minus - previous value
					float fxm = tex2D(_WavePPTSource, q - e.xz).x; // x minus
					float fym = tex2D(_WavePPTSource, q - e.zy).x; // y minus
					float fxp = tex2D(_WavePPTSource, q + e.xz).x; // x plus
					float fyp = tex2D(_WavePPTSource, q + e.zy).x; // y plus

					// The actual propagation:
					float c = .35;
					float ftp = c*c*(fxm + fxp + fym + fyp - 4.*ft) - ftm + 2.*ft;

					// open boundary condition, from: http://hplgit.github.io/wavebc/doc/pub/._wavebc_cyborg002.html
					// dudt + c*dudx = 0
					// (ftp - ft)   +   c*(ft-fxm) = 0.
					if (q.x + e.x >= 1.) ftp = -c*(ft - fxm) + ft;
					if (q.y + e.y >= 1.) ftp = -c*(ft - fym) + ft;
					if (q.x - e.x <= 0.) ftp = c*(fxp - ft) + ft;
					if (q.y - e.y <= 0.) ftp = c*(fyp - ft) + ft;

					// Damping
					ftp *= .99;

					if( frac( _Time.w / 24. ) < 0.05 )
					{
						float s = .4 * sqrt(abs(ddx(i.worldPos.x)));
						ftp = 40.*smoothstep( s*90., s*5., length( i.worldPos-0.*float3(15.,0.,10.) ) );
					}

					return float4( ftp, ft, ftm, 1. );
				}

				ENDCG
			}
		}
	}
}
