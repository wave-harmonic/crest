// Crest Ocean System

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
			Blend SrcAlpha One
			ZWrite Off
			ZTest Always
			Cull Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile __ _DIRECT_TOWARDS_POINT

			#include "UnityCG.cginc"
			#include "../../OceanLODData.hlsl"

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

			float4 _TargetPointData;

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
				float3 uv_slice : TEXCOORD1;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = float4(input.positionOS.xy, 0.0, 0.5);

#if UNITY_UV_STARTS_AT_TOP // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
				o.positionCS.y = -o.positionCS.y;
#endif

				float2 worldXZ = UVToWorld(input.uv);

				o.worldPos_wt.xy = worldXZ;
				o.worldPos_wt.z = input.color.x;

				o.uv_slice = float3(input.uv, _LD_SliceIndex);

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				const half4 oneMinusAttenuation = (half4)1.0 - (half4)_AttenuationInShallows;

				// sample ocean depth (this render target should 1:1 match depth texture, so UVs are trivial)
				const half depth = _LD_TexArray_SeaFloorDepth.Sample(LODData_linear_clamp_sampler, input.uv_slice).x;

				// Preferred wave directions
#if _DIRECT_TOWARDS_POINT
				float2 offset = input.worldPos_wt.xy - _TargetPointData.xy;
				float preferDist = length(offset);
				float preferWt = smoothstep(_TargetPointData.w, _TargetPointData.z, preferDist);
				half2 preferredDir = preferWt * offset / preferDist;
				half4 preferredDirX = preferredDir.x;
				half4 preferredDirZ = preferredDir.y;
#endif

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

					// Peferred wave direction
#if _DIRECT_TOWARDS_POINT
					wt *= max((1.0 + Dx * preferredDirX + Dz * preferredDirZ) / 2.0, 0.1);
#endif

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

				return half4(result, input.worldPos_wt.z);
			}

			ENDCG
		}
	}
}
