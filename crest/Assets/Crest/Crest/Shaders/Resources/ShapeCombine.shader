// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Simulation/Combine Animated Wave LODs"
{
	SubShader
	{
		// No culling or depth
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma multi_compile __ _DYNAMIC_WAVE_SIM_ON
			#pragma multi_compile __ _FLOW_ON

			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"

			float _HorizDisplace;
			float _DisplaceClamp;
			float _CrestTime;

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.uv = input.uv;
				return o;
			}

			void Flow(out float2 offsets, out float2 weights)
			{
				const float period = 3.0 * _LD_Params_0.x;
				const float half_period = period / 2.0;
				offsets = fmod(float2(_CrestTime, _CrestTime + half_period), period);
				weights.x = offsets.x / half_period;
				if (weights.x > 1.0) weights.x = 2.0 - weights.x;
				weights.y = 1.0 - weights.x;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				// go from uv out to world for the current shape texture
				const float2 worldPosXZ = LD_0_UVToWorld(input.uv);

				// sample the shape 1 texture at this world pos
				const float2 uv_1 = LD_1_WorldToUV(worldPosXZ);

				float2 flow = 0.0;
				SampleFlow(_LD_TexArray_Flow_0, ADD_SLICE_0_TO_UV(input.uv), 1.0, flow);

				float3 result = 0.0;

				// this lods waves
#if _FLOW_ON
				float2 offsets, weights;
				Flow(offsets, weights);

				float2 uv_0_flow_0 = LD_0_WorldToUV(worldPosXZ - offsets[0] * flow);
				float2 uv_0_flow_1 = LD_0_WorldToUV(worldPosXZ - offsets[1] * flow);
				SampleDisplacements(_LD_TexArray_AnimatedWaves_0, ADD_SLICE_0_TO_UV(uv_0_flow_0), weights[0], result);
				SampleDisplacements(_LD_TexArray_AnimatedWaves_0, ADD_SLICE_0_TO_UV(uv_0_flow_1), weights[1], result);
#else
				SampleDisplacements(_LD_TexArray_AnimatedWaves_0, ADD_SLICE_0_TO_UV(input.uv), 1.0, result);
#endif

				// waves to combine down from the next lod up the chain
				SampleDisplacements(_LD_TexArray_AnimatedWaves_1, ADD_SLICE_1_TO_UV(uv_1), 1.0, result);

				// TODO - uncomment this define once it works in standalone builds
#if _DYNAMIC_WAVE_SIM_ON
				{
					// convert dynamic wave sim to displacements

					half waveSimY = tex2Dlod(_LD_TexArray_DynamicWaves_0, float4(input.uv, 0.0, 0.0)).x;
					result.y += waveSimY;

					const float2 invRes = float2(_LD_Params_0.w, 0.0);
					const half waveSimY_px = tex2Dlod(_LD_TexArray_DynamicWaves_0, float4(input.uv + invRes.xy, 0.0, 0.0)).x;
					const half waveSimY_nx = tex2Dlod(_LD_TexArray_DynamicWaves_0, float4(input.uv - invRes.xy, 0.0, 0.0)).x;
					const half waveSimY_pz = tex2Dlod(_LD_TexArray_DynamicWaves_0, float4(input.uv + invRes.yx, 0.0, 0.0)).x;
					const half waveSimY_nz = tex2Dlod(_LD_TexArray_DynamicWaves_0, float4(input.uv - invRes.yx, 0.0, 0.0)).x;
					// compute displacement from gradient of water surface - discussed in issue #18 and then in issue #47

					// For gerstner waves, horiz displacement is proportional to derivative of vertical displacement multiplied by the wavelength
					const float wavelength_mid = 2.0 * _LD_Params_0.x * 1.5;
					const float wavevector = 2.0 * 3.14159 / wavelength_mid;
					const float2 dydx = (float2(waveSimY_px, waveSimY_pz) - float2(waveSimY_nx, waveSimY_nz)) / (2.0 * _LD_Params_0.x);
					float2 dispXZ = _HorizDisplace * dydx / wavevector;

					const float maxDisp = _LD_Params_0.x * _DisplaceClamp;
					dispXZ = clamp(dispXZ, -maxDisp, maxDisp);

					result.xz += dispXZ;
				}
#endif // _DYNAMIC_WAVE_SIM_

				return half4(result, 1.0);
			}
			ENDCG
		}
	}
}
