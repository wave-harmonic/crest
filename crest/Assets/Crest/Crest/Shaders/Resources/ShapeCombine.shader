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
			#pragma enable_d3d11_debug_symbols

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
				const float period = 3.0 * _LD_Params_PrevFrame[_LD_SLICE_Index_ThisLod].x;
				const float half_period = period / 2.0;
				offsets = fmod(float2(_CrestTime, _CrestTime + half_period), period);
				weights.x = offsets.x / half_period;
				if (weights.x > 1.0) weights.x = 2.0 - weights.x;
				weights.y = 1.0 - weights.x;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				// go from uv out to world for the current shape texture
				const float2 worldPosXZ = UVToWorld_ThisFrame(input.uv);

				// sample the shape 1 texture at this world pos
				const float3 uv_nextLod = WorldToUV_NextLod(worldPosXZ);

				float3 uv_thisLod = ADD_SLICE_THIS_LOD_TO_UV(input.uv);

				float2 flow = 0.0;
				SampleFlow(_LD_TexArray_Flow_PrevFrame, uv_thisLod, 1.0, flow);

				float3 result = 0.0;

				// this lods waves
#if _FLOW_ON
				float2 offsets, weights;
				Flow(offsets, weights);

				float3 uv_thisLod_flow_0 = WorldToUV_ThisLod(worldPosXZ - offsets[0] * flow);
				float3 uv_thisLod_flow_1 = WorldToUV_ThisLod(worldPosXZ - offsets[1] * flow);
				SampleDisplacements(_LD_TexArray_AnimatedWaves_ThisFrame, uv_thisLod_flow_0, weights[0], result);
				SampleDisplacements(_LD_TexArray_AnimatedWaves_ThisFrame, uv_thisLod_flow_1, weights[1], result);
#else
				SampleDisplacements(_LD_TexArray_AnimatedWaves_ThisFrame, ADD_SLICE_THIS_LOD_TO_UV(uv_thisLod), 1.0, result);
#endif

				// waves to combine down from the next lod up the chain
				SampleDisplacements(_LD_TexArray_AnimatedWaves_ThisFrame, ADD_SLICE_NEXT_LOD_TO_UV(uv_nextLod), 1.0, result);

				// TODO - uncomment this define once it works in standalone builds
#if _DYNAMIC_WAVE_SIM_ON
				{
					// convert dynamic wave sim to displacements

					half waveSimY = SampleLod(_LD_TexArray_DynamicWaves_ThisFrame, uv_thisLod).x;
					result.y += waveSimY;

					const float2 invRes = float2(_LD_Params_PrevFrame[_LD_SLICE_Index_ThisLod].w, 0.0);
					const half waveSimY_px = SampleLod(_LD_TexArray_DynamicWaves_ThisFrame, uv_thisLod + float3(invRes.xy, 0)).x;
					const half waveSimY_nx = SampleLod(_LD_TexArray_DynamicWaves_ThisFrame, uv_thisLod - float3(invRes.xy, 0)).x;
					const half waveSimY_pz = SampleLod(_LD_TexArray_DynamicWaves_ThisFrame, uv_thisLod + float3(invRes.yx, 0)).x;
					const half waveSimY_nz = SampleLod(_LD_TexArray_DynamicWaves_ThisFrame, uv_thisLod - float3(invRes.yx, 0)).x;
					// compute displacement from gradient of water surface - discussed in issue #18 and then in issue #47

					// For gerstner waves, horiz displacement is proportional to derivative of vertical displacement multiplied by the wavelength
					const float wavelength_mid = 2.0 * _LD_Params_PrevFrame[_LD_SLICE_Index_ThisLod].x * 1.5;
					const float wavevector = 2.0 * 3.14159 / wavelength_mid;
					const float2 dydx = (float2(waveSimY_px, waveSimY_pz) - float2(waveSimY_nx, waveSimY_nz)) / (2.0 * _LD_Params_PrevFrame[_LD_SLICE_Index_ThisLod].x);
					float2 dispXZ = _HorizDisplace * dydx / wavevector;

					const float maxDisp = _LD_Params_PrevFrame[_LD_SLICE_Index_ThisLod].x * _DisplaceClamp;
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
