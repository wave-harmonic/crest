// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// A batch of Gerstner components
Shader "Crest/Inputs/Animated Waves/Gerstner Batch"
{
	Properties
	{
		// This is purely for convenience - it makes the value appear in material section of the inspector and is useful for debugging.
		_NumInBatch("_NumInBatch", float) = 0
	}

	SubShader
	{
		Pass
		{
			Blend One One
		
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"

			// IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatched.cs, both must be updated together!
			#define BATCH_SIZE 32

			#define PI 3.141593

			half _AttenuationInShallows;
			uint _NumWaveVecs;

			half4 _TwoPiOverWavelengths[BATCH_SIZE / 4];
			half4 _Amplitudes[BATCH_SIZE / 4];
			half4 _WaveDirX[BATCH_SIZE / 4];
			half4 _WaveDirZ[BATCH_SIZE / 4];
			half4 _Phases[BATCH_SIZE / 4];
			half4 _ChopAmps[BATCH_SIZE / 4];

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 worldPos_wt : TEXCOORD0;
				float2 uv : TEXCOORD1;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = float4(input.positionOS.x, -input.positionOS.y, 0.0, 0.5);

				float2 worldXZ = LD_0_UVToWorld(input.uv);

				o.worldPos_wt.xy = worldXZ;
				o.worldPos_wt.z = input.color.x;

				o.uv = input.uv;

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				const half4 oneMinusAttenuation = (half4)1.0 - (half4)_AttenuationInShallows;

				// sample ocean depth (this render target should 1:1 match depth texture, so UVs are trivial)
				const half depth = CREST_OCEAN_DEPTH_BASELINE - tex2D(_LD_Sampler_SeaFloorDepth_0, input.uv).x;
				half3 result = (half3)0.0;

				// gerstner computation is vectorized - processes 4 wave components at once
				for (uint vi = 0; vi < _NumWaveVecs; vi++)
				{
					// attenuate waves based on ocean depth. if depth is greater than 0.5*wavelength, water is considered Deep and wave is
					// unaffected. if depth is less than this, wave velocity decreases. waves will then bunch up and grow in amplitude and
					// eventually break. i model "Deep" water, but then simply ramp down waves in non-deep water with a linear multiplier.
					// http://hyperphysics.phy-astr.gsu.edu/hbase/Waves/watwav2.html
					// http://hyperphysics.phy-astr.gsu.edu/hbase/watwav.html#c1
					//half depth_wt = saturate(depth / (0.5 * _MinWavelength)); // slightly different result - do per wavelength for now
					// The below is a few things collapsed together.
					half4 depth_wt = saturate(depth * _TwoPiOverWavelengths[vi] / PI);
					// keep some proportion of amplitude so that there is some waves remaining
					half4 wt = _AttenuationInShallows * depth_wt + oneMinusAttenuation;

					// direction
					half4 Dx = _WaveDirX[vi];
					half4 Dz = _WaveDirZ[vi];
					// wave number
					half4 k = _TwoPiOverWavelengths[vi];
					// spatial location
					half4 x = Dx * input.worldPos_wt.x + Dz * input.worldPos_wt.y;
					half4 angle = k * x + _Phases[vi];

					// dx and dz could be baked into _ChopAmps
					half4 disp = _ChopAmps[vi] * sin(angle);
					half4 resultx = disp * Dx;
					half4 resultz = disp * Dz;

					half4 resulty = _Amplitudes[vi] * cos(angle);

					// sum the vector results
					result.x += dot(resultx, wt);
					result.y += dot(resulty, wt);
					result.z += dot(resultz, wt);
				}

				return half4(input.worldPos_wt.z * result, 0.0);
			}

			ENDCG
		}
	}
}
