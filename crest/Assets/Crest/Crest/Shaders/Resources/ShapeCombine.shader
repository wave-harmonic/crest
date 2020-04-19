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

			#pragma multi_compile __ CREST_DYNAMIC_WAVE_SIM_ON_INTERNAL
			#pragma multi_compile __ CREST_FLOW_ON_INTERNAL

			#include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanLODData.hlsl"
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
				const float period = 3.0 * _LD_Params[_LD_SliceIndex].x;
				const float half_period = period / 2.0;
				offsets = fmod(float2(_CrestTime, _CrestTime + half_period), period);
				weights.x = offsets.x / half_period;
				if (weights.x > 1.0) weights.x = 2.0 - weights.x;
				weights.y = 1.0 - weights.x;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float3 uv_thisLod = float3(input.uv, _LD_SliceIndex);

				// go from uv out to world for the current shape texture
				const float2 worldPosXZ = UVToWorld(input.uv);

				// sample the shape 1 texture at this world pos
				const float3 uv_nextLod = WorldToUV_BiggerLod(worldPosXZ);

				float3 result = 0.0;
				half sss = 0.0;

#if CREST_FLOW_ON_INTERNAL
				half2 flow = 0.0;
				SampleFlow(_LD_TexArray_Flow, uv_thisLod, 1.0, flow);

				float2 offsets, weights;
				Flow(offsets, weights);

				float3 uv_thisLod_flow_0 = WorldToUV(worldPosXZ - offsets[0] * flow);
				float3 uv_thisLod_flow_1 = WorldToUV(worldPosXZ - offsets[1] * flow);
				SampleDisplacements(_LD_TexArray_WaveBuffer, uv_thisLod_flow_0, weights[0], result, sss);
				SampleDisplacements(_LD_TexArray_WaveBuffer, uv_thisLod_flow_1, weights[1], result, sss);
#else
				float4 data = _LD_TexArray_WaveBuffer.SampleLevel(LODData_linear_clamp_sampler, uv_thisLod, 0.0);
				result += data.xyz;
				sss = data.w;
#endif // CREST_FLOW_ON_INTERNAL

				float arrayDepth;
				{
					float w, h;
					_LD_TexArray_AnimatedWaves.GetDimensions(w, h, arrayDepth);
				}

				// waves to combine down from the next lod up the chain
				if ((float)_LD_SliceIndex < arrayDepth - 1.0)
				{
					float4 dataNextLod = _LD_TexArray_AnimatedWaves.SampleLevel(LODData_linear_clamp_sampler, uv_nextLod, 0.0);
					result += dataNextLod.xyz;
					sss += dataNextLod.w;
				}

#if CREST_DYNAMIC_WAVE_SIM_ON_INTERNAL
				{
					// convert dynamic wave sim to displacements

					half waveSimY = SampleLod(_LD_TexArray_DynamicWaves, uv_thisLod).x;
					result.y += waveSimY;

					const float2 invRes = float2(_LD_Params[_LD_SliceIndex].w, 0.0);
					const half waveSimY_px = SampleLod(_LD_TexArray_DynamicWaves, uv_thisLod + float3(invRes.xy, 0)).x;
					const half waveSimY_nx = SampleLod(_LD_TexArray_DynamicWaves, uv_thisLod - float3(invRes.xy, 0)).x;
					const half waveSimY_pz = SampleLod(_LD_TexArray_DynamicWaves, uv_thisLod + float3(invRes.yx, 0)).x;
					const half waveSimY_nz = SampleLod(_LD_TexArray_DynamicWaves, uv_thisLod - float3(invRes.yx, 0)).x;
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
#endif // CREST_DYNAMIC_WAVE_SIM_ON_INTERNAL

				return half4(result, sss);
			}
			ENDCG
		}



		// Copy back to lod texture array
		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

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
