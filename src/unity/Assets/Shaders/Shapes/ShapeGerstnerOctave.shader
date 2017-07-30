// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// A single Gerstner Octave
Shader "Ocean/Shape/Gerstner Octave"
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

				uniform float _TexelsPerWave;
				uniform float _Wavelength;

				bool SamplingIsAdequate( float minWavelengthInShape )
				{
					const float cameraWidth = 2. * unity_OrthoParams.x;
					const float renderTargetRes = _ScreenParams.x;
					const float texSize = cameraWidth / renderTargetRes;
					const float minWavelength = texSize * _TexelsPerWave;
					return minWavelengthInShape > minWavelength;
				}

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );
					o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;

					// if wavelength is too small, kill this quad so that it doesnt render any shape
					if( !SamplingIsAdequate( _Wavelength ) )
						o.vertex.xy *= 0.;

					return o;
				}

				// respects the gui option to freeze time
				uniform float _MyTime;

				uniform float _Amplitude;
				uniform float _Angle;
				uniform float _Speed;
				uniform float _Steepness;

				float4 frag (v2f i) : SV_Target
				{
					i.worldPos.y = 0.;

					float2 dir = float2(cos(PI * _Angle / 180.0), sin(PI * _Angle / 180.0));
					float s = dot(dir, i.worldPos.xz) + _Speed * _MyTime * 0.05;
					float phi = s / _Wavelength;

					float3 disp;
					disp.xz = _Steepness * cos(phi * PI * 2.) * dir;
					disp.y = sin(phi * PI * 2.);

					return float4(_Amplitude * disp, 1.0 );
				}

				ENDCG
			}
		}
	}
}
