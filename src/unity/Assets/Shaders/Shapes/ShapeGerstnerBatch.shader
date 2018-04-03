// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// A batch of Gerstner components
Shader "Ocean/Shape/Gerstner Batch"
{
	Properties
	{
		// This is purely for convenience - it makes the value appear in material section of the inspector and is useful for debugging.
		_NumInBatch("_NumInBatch", float) = 0
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

				#define TWOPI 6.283185

				struct appdata_t {
					float4 vertex : POSITION;
					half color : COLOR0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 worldPos_wt : TEXCOORD0;
				};

				// IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatched.cs, both must be updated together!
				#define BATCH_SIZE 32

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = UnityObjectToClipPos( v.vertex );
					o.worldPos_wt.xy = mul( unity_ObjectToWorld, v.vertex ).xz;
					o.worldPos_wt.z = v.color.x;
					return o;
				}

				// respects the gui option to freeze time
				uniform float _MyTime;
				uniform half _Chop;
				uniform half _Wavelengths[BATCH_SIZE];
				uniform half _Amplitudes[BATCH_SIZE];
				uniform half _Angles[BATCH_SIZE];
				uniform half _Phases[BATCH_SIZE];

				half3 frag (v2f i) : SV_Target
				{
					const half minWavelength = MinWavelengthForCurrentOrthoCamera();
			
					half3 result = (half3)0.;

					// unrolling this loop once helped SM Issue Utilization and some other stats, but the GPU time is already very low so leaving this for now
					for (int j = 0; j < BATCH_SIZE; j++)
					{
						if (_Wavelengths[j] == 0.)
							break;

						// weight
						half wt = ComputeSortedShapeWeight(_Wavelengths[j], minWavelength);
						// wave speed
						half C = ComputeWaveSpeed(_Wavelengths[j]);
						// direction
						half2 D = half2(cos(_Angles[j]), sin(_Angles[j]));
						// wave number
						half k = TWOPI / _Wavelengths[j];
						// spatial location
						half x = dot(D, i.worldPos_wt.xy);

						half3 result_i = wt * _Amplitudes[j];
						result_i.y *= cos(k*(x + C*_MyTime) + _Phases[j]);
						result_i.xz *= -_Chop * D * sin(k*(x + C * _MyTime) + _Phases[j]);
						result += result_i;
					}

					return i.worldPos_wt.z * result;
				}

				ENDCG
			}
		}
	}
}
