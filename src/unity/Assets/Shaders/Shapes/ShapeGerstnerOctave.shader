// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// A single Gerstner Octave
Shader "Ocean/Shape/Gerstner Octave"
{
	Properties
	{
		_Amplitude ("Amplitude", float) = 1
		_Wavelength("Wavelength", range(0,180)) = 100
		_Angle ("Angle", range(-180, 180)) = 0
		_SpeedMul("Speed Mul", range(0, 1)) = 1.0
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
					if( !SamplingIsAppropriate( _Wavelength ) || _Amplitude < 0.0001 )
						o.vertex.xy *= 0.;

					return o;
				}

				// respects the gui option to freeze time
				uniform float _MyTime;
				uniform float _MyDeltaTime;
				uniform float _KinematicWaves;

				uniform float _Choppiness;

				uniform float _Angle;
				uniform float _SpeedMul;

				float4 frag (v2f i) : SV_Target
				{
					// assume deep water for now, but this could read from the water depth texture in the future
					const float WATER_DEPTH = 10000.;
					float C = _SpeedMul * ComputeDriverWaveSpeed(_Wavelength, WATER_DEPTH);

					// direction
					float2 D = float2(cos(PI * _Angle / 180.0), sin(PI * _Angle / 180.0));
					// wave number
					float k = 2. * PI / _Wavelength;

					float2 displacedPos = i.worldPos.xz;
					float2 samplePos = displacedPos;

#define USE_FPI
#ifdef USE_FPI
					// use fixed point iteration to solve for sample position, to compute displacement.
					// this could be written out to a texture and used to displace foam..

					// samplePos + disp(samplePos) = displacedPos
					// error = displacedPos - disp(samplePos)
					// iteration: samplePos += displacedPos - disp(samplePos)
					if (_Choppiness > 0.0001)
					{
						// start search at displaced position
						for (int oct = 0; oct < 5; oct++)
						{
							float x_ = dot(D, samplePos);
							float2 error = displacedPos - (samplePos + _Choppiness * -sin(k*(x_ + C*_MyTime)) * D);
							// move to eliminate error
							samplePos += 0.7 * error;
						}
					}
#endif

					float x = dot(D, samplePos);
					float y = _Amplitude * cos(k*(x + C*_MyTime));

					y *= i.weight.x;

					if( _KinematicWaves == 0. )
					{
						y *= _MyDeltaTime*_MyDeltaTime;
						y *= 0.6;
					}

					return float4(y, 0., 0., 0.);
				}

				ENDCG
			}
		}
	}
}
