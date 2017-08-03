// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

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
				Blend One One
			
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

				float4 frag (v2f i) : SV_Target
				{
					i.worldPos.y = 0.;
					float4 uv = float4(i.worldPos.xz / 64.0, 0.0, 0.0);
					float3 disp = tex2Dlod( _WavePPTSource, uv ).xyz;
					disp.xz = (float2)0.;
					disp.y += sin( length( i.worldPos.xz ) - _Time.w );// / (4. + length( i.worldPos.xz )) );

					return float4(disp, 1.0);
				}

				ENDCG
			}
		}
	}
}
