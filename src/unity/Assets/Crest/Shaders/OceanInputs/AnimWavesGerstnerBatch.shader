// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// A batch of Gerstner components
Shader "Ocean/Inputs/Animated Waves/Gerstner Batch"
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
				#include "../MultiscaleShape.hlsl"
				#include "../OceanLODData.hlsl"

				#define TWOPI 6.283185

				struct appdata_t {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					half color : COLOR0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float3 worldPos_wt : TEXCOORD0;
					float2 uv : TEXCOORD1;
				};

				// IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatched.cs, both must be updated together!
				#define BATCH_SIZE 32

				v2f vert( appdata_t v )
				{
					v2f o;
					o.vertex = float4(v.vertex.x, -v.vertex.y, 0., .5);

					float2 worldXZ = LD_0_UVToWorld(v.uv);

					o.worldPos_wt.xy = worldXZ;
					o.worldPos_wt.z = v.color.x;

					o.uv = v.uv;

					return o;
				}

				uniform float _CrestTime;
				uniform half _Chop;
				uniform half _Gravity;
				uniform half4 _Wavelengths[BATCH_SIZE / 4];
				uniform half4 _Amplitudes[BATCH_SIZE / 4];
				uniform half4 _Angles[BATCH_SIZE / 4];
				uniform half4 _Phases[BATCH_SIZE / 4];
				uniform half4 _ChopScales[BATCH_SIZE / 4];
				uniform half4 _GravityScales[BATCH_SIZE / 4];

				half4 frag (v2f i) : SV_Target
				{
					const half minWavelength = MinWavelengthForCurrentOrthoCamera();
			
					// sample ocean depth (this render target should 1:1 match depth texture, so UVs are trivial)
					const half depth = DEPTH_BASELINE - tex2D(_LD_Sampler_SeaFloorDepth_0, i.uv).x;
					half3 result = (half3)0.;

					// unrolling this loop once helped SM Issue Utilization and some other stats, but the GPU time is already very low so leaving this for now
					for (uint vi = 0; vi < BATCH_SIZE / 4; vi++)
					{
						[unroll]
						for (uint ei = 0; ei < 4; ei++)
						{
							if (_Wavelengths[vi][ei] == 0.)
							{
								return half4(i.worldPos_wt.z * result, 0.);
							}

							// weight
							half wt = ComputeSortedShapeWeight(_Wavelengths[vi][ei], minWavelength);

							// attenuate waves based on ocean depth. if depth is greater than 0.5*wavelength, water is considered Deep and wave is
							// unaffected. if depth is less than this, wave velocity decreases. waves will then bunch up and grow in amplitude and
							// eventually break. i model "Deep" water, but then simply ramp down waves in non-deep water with a linear multiplier.
							// http://hyperphysics.phy-astr.gsu.edu/hbase/Waves/watwav2.html
							// http://hyperphysics.phy-astr.gsu.edu/hbase/watwav.html#c1
							//half depth_wt = saturate(depth / (0.5 * minWavelength)); // slightly different result - do per wavelength for now
							half depth_wt = saturate(depth / (0.5 * _Wavelengths[vi][ei]));
							// leave a little bit - always keep 10% of amplitude
							wt *= .1 + .9 * depth_wt;

							// wave speed
							half C = ComputeWaveSpeed(_Wavelengths[vi][ei], _Gravity * _GravityScales[vi][ei]);
							// direction
							half2 D = half2(cos(_Angles[vi][ei]), sin(_Angles[vi][ei]));
							// wave number
							half k = TWOPI / _Wavelengths[vi][ei];
							// spatial location
							half x = dot(D, i.worldPos_wt.xy);

							half3 result_i = wt * _Amplitudes[vi][ei];
							result_i.y *= cos(k*(x + C * _CrestTime) + _Phases[vi][ei]);
							result_i.xz *= -_Chop * _ChopScales[vi][ei] * D * sin(k*(x + C * _CrestTime) + _Phases[vi][ei]);
							result += result_i;
						}
					}

					return half4(i.worldPos_wt.z * result, 0.);
				}

				ENDCG
			}
		}
	}
}
