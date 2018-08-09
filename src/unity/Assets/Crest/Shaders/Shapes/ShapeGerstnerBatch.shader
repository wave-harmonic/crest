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
				#include "../OceanLODData.cginc"

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
				uniform half _Chop;
				uniform half _Wavelengths[BATCH_SIZE];
				uniform half _Amplitudes[BATCH_SIZE];
				uniform half _Angles[BATCH_SIZE];
				uniform half _Phases[BATCH_SIZE];

				half4 frag (v2f i) : SV_Target
				{
					const half minWavelength = MinWavelengthForCurrentOrthoCamera();
			
					// sample ocean depth (this render target should 1:1 match depth texture, so UVs are trivial)
					half depth = tex2D(_WD_OceanDepth_Sampler_0, i.vertex.xy / _ScreenParams.xy).x + DEPTH_BIAS;
					half3 result = (half3)0.;

					// unrolling this loop once helped SM Issue Utilization and some other stats, but the GPU time is already very low so leaving this for now
					for (int j = 0; j < BATCH_SIZE; j++)
					{
						if (_Wavelengths[j] == 0.)
							break;

						// weight
						half wt = ComputeSortedShapeWeight(_Wavelengths[j], minWavelength);

						// attenuate waves based on ocean depth. if depth is greater than 0.5*wavelength, water is considered Deep and wave is
						// unaffected. if depth is less than this, wave velocity decreases. waves will then bunch up and grow in amplitude and
						// eventually break. i model "Deep" water, but then simply ramp down waves in non-deep water with a linear multiplier.
						// http://hyperphysics.phy-astr.gsu.edu/hbase/Waves/watwav2.html
						// http://hyperphysics.phy-astr.gsu.edu/hbase/watwav.html#c1
						//half depth_wt = saturate(depth / (0.5 * minWavelength)); // slightly different result - do per wavelength for now
						half depth_wt = saturate(depth / (0.5 * _Wavelengths[j]));
						// leave a little bit - always keep 10% of amplitude
						wt *= .1 + .9 * depth_wt;

						// wave speed
						half C = ComputeWaveSpeed(_Wavelengths[j]);
						// direction
						half2 D = half2(cos(_Angles[j]), sin(_Angles[j]));
						// wave number
						half k = TWOPI / _Wavelengths[j];
						// spatial location
						half x = dot(D, i.worldPos_wt.xy);

						half3 result_i = wt * _Amplitudes[j];
						result_i.y *= cos(k*(x + C*_Time.y) + _Phases[j]);
						result_i.xz *= -_Chop * D * sin(k*(x + C * _Time.y) + _Phases[j]);
						result += result_i;
					}

					return half4(i.worldPos_wt.z * result, 0.);
				}

				ENDCG
			}
		}
	}
}
