// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// A single Gerstner Octave
Shader "Ocean/Shape/Gerstner Octave"
{
	Properties
	{
		_Amplitude ("Amplitude", float) = 1
		_Wavelength("Wavelength", range(0,180)) = 100
		_Angle ("Angle", range(-180, 180)) = 0
	}

	Category
	{
		Tags{ "Queue" = "Transparent" }

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
				#include "MultiscaleShape.cginc"

				#define PI 3.141592653

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
					float3 color : COLOR0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 worldPos : TEXCOORD0;
					float3 weight : COLOR0;
				};

				uniform float _Wavelength;
				uniform float _Amplitude;

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );
					o.worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
					o.weight = v.color;

					// if wavelength is too small, kill this quad so that it doesnt render any shape
					float shapeWt;
					if (!SamplingIsAppropriate_Gerstner(_Wavelength, shapeWt) || _Amplitude < 0.0001)
						o.vertex.xy *= 0.;

					o.weight *= shapeWt;

					return o;
				}

				// respects the gui option to freeze time
				uniform float _MyTime;
				uniform float _Chop;
				uniform float _Angle;
				uniform float _Phase;

				float3 frag (v2f i) : SV_Target
				{
					float C = ComputeWaveSpeed( _Wavelength );

					// direction
					float2 D = float2(cos(PI * _Angle / 180.0), sin(PI * _Angle / 180.0));
					// wave number
					float k = 2. * PI / _Wavelength;

					float3 result;

					float x = dot(D, i.worldPos.xz);
					result.y = _Amplitude * cos(k*(x + C*_MyTime) + _Phase);
					result.xz = -_Chop * D * _Amplitude * sin(k*(x + C * _MyTime) + _Phase);

					result *= i.weight.x;

					return result;
				}

				ENDCG
			}
		}
	}
}
