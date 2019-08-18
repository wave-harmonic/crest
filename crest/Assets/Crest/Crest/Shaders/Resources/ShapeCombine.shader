// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// #pragma kernel ShapeCombine
// #pragma kernel ShapeCombine_DISABLE_COMBINE _DISABLE_COMBINE
// #pragma kernel ShapeCombine_FLOW_ON _FLOW_ON
// #pragma kernel ShapeCombine_FLOW_ON_DISABLE_COMBINE _FLOW_ON _DISABLE_COMBINE
// #pragma kernel ShapeCombine_DYNAMIC_WAVE_SIM_ON _DYNAMIC_WAVE_SIM_ON
// #pragma kernel ShapeCombine_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE _DYNAMIC_WAVE_SIM_ON _DISABLE_COMBINE
// #pragma kernel ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON _FLOW_ON _DYNAMIC_WAVE_SIM_ON
// #pragma kernel ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE _FLOW_ON _DYNAMIC_WAVE_SIM_ON _DISABLE_COMBINE


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
			// For SV_VertexID
			#pragma target 3.5
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma multi_compile __ _DYNAMIC_WAVE_SIM_ON
			#pragma multi_compile __ _FLOW_ON
			#pragma multi_compile __ _DISABLE_COMBINE

			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"
			#include "../FullScreenTriangle.hlsl"

			float _HorizDisplace;
			float _DisplaceClamp;
			float _CrestTime;

			struct Attributes
			{
				uint vertexID : SV_VertexID;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
				output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
				return output;
			}

			void Flow(out float2 offsets, out float2 weights)
			{
				const float period = 3.0 * _LD_Params[_LD_SliceIndex].x;
				const float half_period = period / 2.0;
				offsets = fmod(float2(_CrestTime, _CrestTime + half_period), period);
				weights.x = offsets.x / half_period;
				if (weights.x > 1.0) weights.x = 2.0 - weights.x;
				weights.y = 1.0 - weights.x;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				const float2 uv_thisLod = input.uv ;// IDtoUV(id.xy, width, height);

				const float2 worldPosXZ = UVToWorld(uv_thisLod);

				// sample the shape 1 texture at this world pos
				const float2 uv_biggerLod = WorldToUV_BiggerLod(worldPosXZ);

				float3 result = 0.0;
				half sss = 0.;
				// this lods waves
#if _FLOW_ON
				float2 flow = 0.0;
				SampleFlow(_LD_Texture_Flow, uv_thisLod, 1.0, flow);

				float2 offsets, weights;
				Flow(offsets, weights);

				float2 uv_thisLod_flow_0 = WorldToUV(worldPosXZ - offsets[0] * flow);
				float2 uv_thisLod_flow_1 = WorldToUV(worldPosXZ - offsets[1] * flow);
				SampleDisplacements(_LD_Texture_WaveBuffer, uv_thisLod_flow_0, weights[0], result, sss);
				SampleDisplacements(_LD_Texture_WaveBuffer, uv_thisLod_flow_1, weights[1], result, sss);
#else
				float4 data =_LD_Texture_WaveBuffer.SampleLevel(LODData_linear_clamp_sampler, uv_thisLod, 0.0);
				result += data.xyz;
				sss = data.a;
#endif

			// C# Script determines whether this enabled or not by selecting appropriate
			// kernel for each LOD.
#if !_DISABLE_COMBINE
				const float4 lod = SampleLod(_LD_Texture_AnimatedWaves_BiggerLod, uv_biggerLod);
				result += lod.xyz;
				sss += lod.a;
#endif

#if _DYNAMIC_WAVE_SIM_ON
				{
					// convert dynamic wave sim to displacements

					half waveSimY = SampleLod(_LD_Texture_DynamicWaves, uv_thisLod).x;
					result.y += waveSimY;

					const float2 invRes = float2(_LD_Params[_LD_SliceIndex].w, 0.0);
					const half waveSimY_px = SampleLod(_LD_Texture_DynamicWaves, uv_thisLod + invRes.xy).x;
					const half waveSimY_nx = SampleLod(_LD_Texture_DynamicWaves, uv_thisLod - invRes.xy).x;
					const half waveSimY_pz = SampleLod(_LD_Texture_DynamicWaves, uv_thisLod + invRes.yx).x;
					const half waveSimY_nz = SampleLod(_LD_Texture_DynamicWaves, uv_thisLod - invRes.yx).x;
					// compute displacement from gradient of water surface - discussed in issue #18 and then in issue #47

					// For gerstner waves, horiz displacement is proportional to derivative of vertical displacement multiplied by the wavelength
					const float wavelength_mid = 2.0 * _LD_Params[_LD_SliceIndex].x * 1.5;
					const float wavevector = 2.0 * 3.14159 / wavelength_mid;
					const float2 dydx = (float2(waveSimY_px, waveSimY_pz) - float2(waveSimY_nx, waveSimY_nz)) / (2.0 * _LD_Params[_LD_SliceIndex].x);
					float2 dispXZ = _HorizDisplace * dydx / wavevector;

					const float maxDisp = _LD_Params[_LD_SliceIndex].x * _DisplaceClamp;
					dispXZ = clamp(dispXZ, -maxDisp, maxDisp);

					result.xz += dispXZ;
				}
#endif // _DYNAMIC_WAVE_SIM_

				return half4(result, sss);
			}
			ENDCG
		}
	}
}


// [numthreads(THREAD_GROUP_SIZE_X,THREAD_GROUP_SIZE_Y,1)] void ShapeCombine(uint3 id : SV_DispatchThreadID) { ShapeCombineBase(id); }
// [numthreads(THREAD_GROUP_SIZE_X,THREAD_GROUP_SIZE_Y,1)] void ShapeCombine_DISABLE_COMBINE(uint3 id : SV_DispatchThreadID) { ShapeCombineBase(id); }
// [numthreads(THREAD_GROUP_SIZE_X,THREAD_GROUP_SIZE_Y,1)] void ShapeCombine_FLOW_ON(uint3 id : SV_DispatchThreadID) { ShapeCombineBase(id); }
// [numthreads(THREAD_GROUP_SIZE_X,THREAD_GROUP_SIZE_Y,1)] void ShapeCombine_FLOW_ON_DISABLE_COMBINE(uint3 id : SV_DispatchThreadID) { ShapeCombineBase(id); }
// [numthreads(THREAD_GROUP_SIZE_X,THREAD_GROUP_SIZE_Y,1)] void ShapeCombine_DYNAMIC_WAVE_SIM_ON(uint3 id : SV_DispatchThreadID) { ShapeCombineBase(id); }
// [numthreads(THREAD_GROUP_SIZE_X,THREAD_GROUP_SIZE_Y,1)] void ShapeCombine_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE(uint3 id : SV_DispatchThreadID) { ShapeCombineBase(id); }
// [numthreads(THREAD_GROUP_SIZE_X,THREAD_GROUP_SIZE_Y,1)] void ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON(uint3 id : SV_DispatchThreadID) { ShapeCombineBase(id); }
// [numthreads(THREAD_GROUP_SIZE_X,THREAD_GROUP_SIZE_Y,1)] void ShapeCombine_FLOW_ON_DYNAMIC_WAVE_SIM_ON_DISABLE_COMBINE(uint3 id : SV_DispatchThreadID) { ShapeCombineBase(id); }
