// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Shaders to perform combine as a ping pong process - combine happens into an auxiliary buffer, which is then copied
// back into the texture array.

Shader "Hidden/Crest/Simulation/Combine Animated Wave LODs"
{
	SubShader
	{
		// No culling or depth
		Cull Off
		ZWrite Off
		ZTest Always
		Blend Off

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma target 3.5

			#pragma multi_compile __ CREST_DYNAMIC_WAVE_SIM_ON_INTERNAL
			#pragma multi_compile __ CREST_FLOW_ON_INTERNAL

			#include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanHelpersNew.hlsl"
			#include "../FullScreenTriangle.hlsl"

			float _HorizDisplace;
			float _DisplaceClamp;

			struct Attributes
			{
				uint VertexID : SV_VertexID;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = GetFullScreenTriangleVertexPosition(input.VertexID);
				o.uv = GetFullScreenTriangleTexCoord(input.VertexID);
				return o;
			}

			void Flow(out float2 offsets, out float2 weights)
			{
				const float period = 3.0 * _CrestCascadeData[_LD_SliceIndex]._texelWidth;
				const float half_period = period / 2.0;
				offsets = fmod(float2(_CrestTime, _CrestTime + half_period), period);
				weights.x = offsets.x / half_period;
				if (weights.x > 1.0) weights.x = 2.0 - weights.x;
				weights.y = 1.0 - weights.x;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float3 uv_thisLod = float3(input.uv, _LD_SliceIndex);
				const CascadeParams cascadeData0 = _CrestCascadeData[_LD_SliceIndex];
				const CascadeParams cascadeData1 = _CrestCascadeData[_LD_SliceIndex + 1];

				// go from uv out to world for the current shape texture
				const float2 worldPosXZ = UVToWorld(input.uv, _LD_SliceIndex, cascadeData0);

				// sample the shape 1 texture at this world pos
				const float3 uv_nextLod = WorldToUV(worldPosXZ, cascadeData1, _LD_SliceIndex + 1);

				float3 result = 0.0;
				float variance = 0.0;

				// Sample in waves for this cascade.
#if CREST_FLOW_ON_INTERNAL
				half2 flow = 0.0;
				SampleFlow(_LD_TexArray_Flow, uv_thisLod, 1.0, flow);

				float2 offsets, weights;
				Flow(offsets, weights);

				const float3 uv_thisLod_flow_0 = WorldToUV(worldPosXZ - offsets[0] * flow, cascadeData0, _LD_SliceIndex);
				const float3 uv_thisLod_flow_1 = WorldToUV(worldPosXZ - offsets[1] * flow, cascadeData0, _LD_SliceIndex);
				SampleDisplacements(_LD_TexArray_WaveBuffer, uv_thisLod_flow_0, weights[0], result, variance);
				SampleDisplacements(_LD_TexArray_WaveBuffer, uv_thisLod_flow_1, weights[1], result, variance);
#else
				float4 data = _LD_TexArray_WaveBuffer.SampleLevel(LODData_linear_clamp_sampler, uv_thisLod, 0.0);
				result = data.xyz;
				variance = data.w;
#endif // CREST_FLOW_ON_INTERNAL

				float arrayDepth;
				{
					float w, h;
					_LD_TexArray_AnimatedWaves.GetDimensions(w, h, arrayDepth);
				}

				// Waves to combine down from the next lod up the chain.
				if ((float)_LD_SliceIndex < arrayDepth - 1.0)
				{
					float4 dataNextLod = _LD_TexArray_AnimatedWaves.SampleLevel(LODData_linear_clamp_sampler, uv_nextLod, 0.0);
					result += dataNextLod.xyz;
					// Do not combine variance. Variance is already cumulative - from low cascades up
				}

#if CREST_DYNAMIC_WAVE_SIM_ON_INTERNAL
				{
					// convert dynamic wave sim to displacements

					half waveSimY = SampleLod(_LD_TexArray_DynamicWaves, uv_thisLod).x;
					result.y += waveSimY;

					const float2 invRes = float2(cascadeData0._oneOverTextureRes, 0.0);
					const half waveSimY_px = SampleLod(_LD_TexArray_DynamicWaves, uv_thisLod + float3(invRes.xy, 0)).x;
					const half waveSimY_nx = SampleLod(_LD_TexArray_DynamicWaves, uv_thisLod - float3(invRes.xy, 0)).x;
					const half waveSimY_pz = SampleLod(_LD_TexArray_DynamicWaves, uv_thisLod + float3(invRes.yx, 0)).x;
					const half waveSimY_nz = SampleLod(_LD_TexArray_DynamicWaves, uv_thisLod - float3(invRes.yx, 0)).x;
					// compute displacement from gradient of water surface - discussed in issue #18 and then in issue #47

					// For gerstner waves, horiz displacement is proportional to derivative of vertical displacement multiplied by the wavelength
					const float wavelength_mid = 2.0 * cascadeData0._texelWidth * 1.5;
					const float wavevector = 2.0 * 3.14159 / wavelength_mid;
					const float2 dydx = (float2(waveSimY_px, waveSimY_pz) - float2(waveSimY_nx, waveSimY_nz)) / (2.0 * cascadeData0._texelWidth);
					float2 dispXZ = _HorizDisplace * dydx / wavevector;

					const float maxDisp = cascadeData0._texelWidth * _DisplaceClamp;
					dispXZ = clamp(dispXZ, -maxDisp, maxDisp);

					result.xz += dispXZ;
				}
#endif // CREST_DYNAMIC_WAVE_SIM_ON_INTERNAL

				return half4(result, variance);
			}
			ENDCG
		}



		// Copy back to lod texture array
		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma target 3.5

			#include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../FullScreenTriangle.hlsl"

			Texture2D _CombineBuffer;

			struct Attributes
			{
				uint VertexID : SV_VertexID;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = GetFullScreenTriangleVertexPosition(input.VertexID);
				o.uv = GetFullScreenTriangleTexCoord(input.VertexID);
				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return _CombineBuffer.SampleLevel(LODData_point_clamp_sampler, input.uv, 0.0);
			}
			ENDCG
		}
	}
}
