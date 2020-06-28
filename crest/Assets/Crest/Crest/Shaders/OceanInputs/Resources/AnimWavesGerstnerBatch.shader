// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Adds Gerstner waves everywhere. Must be given batch prepared by ShapeGerstnerBatched.cs.
Shader "Hidden/Crest/Inputs/Animated Waves/Gerstner Batch Global"
{
	Properties
	{
	}

	SubShader
	{
		Pass
		{
			Blend One One
			ZWrite Off
			ZTest Always
			Cull Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile __ CREST_DIRECT_TOWARDS_POINT_INTERNAL

			#pragma enable_d3d11_debug_symbols

			#include "UnityCG.cginc"

			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanLODData.hlsl"

			#include "../GerstnerShared.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 worldPosXZ : TEXCOORD0;
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
				o.worldPosXZ = worldXZ;
				o.uv_slice = float3(input.uv, _LD_SliceIndex);
				return o;
			}


			void Quantize(float x, float dx, out float x0, out float alpha)
			{
				alpha = frac(x / dx);
				x0 = x - alpha * dx;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				half3 distance_dirXZ = _LD_TexArray_SeaFloorDepth.Sample(LODData_linear_clamp_sampler, input.uv_slice).yzw;
				float2 headingvec = distance_dirXZ.yz;
				float inangle = atan2(headingvec.y, headingvec.x);


				const float lerpDistance = 250.0;
				float directionalStrengh = 1.0 - clamp(distance_dirXZ.x / lerpDistance, 0.0, 1.0);
				half4 directionalWaves;
				half4 uniformWaves;
				{
					// Quantize the direction
					float angle = inangle;
					float dangle = PI / 32.0;
					float angleAlpha = 1.0;
					Quantize(inangle, dangle, angle, angleAlpha);

					// Compute the waves for nearest quantized directions, blend
					float4 waves0 = ComputeGerstner(input.worldPosXZ, input.uv_slice, -float2(cos(angle), sin(angle)));
					float4 waves1 = ComputeGerstner(input.worldPosXZ, input.uv_slice, -float2(cos(angle + dangle), sin(angle + dangle)));
					directionalWaves = lerp(waves0, waves1, angleAlpha);
				}

				float4 result = ComputeGerstner(input.worldPosXZ, input.uv_slice, float2(1, 0));

				result = lerp(result, directionalWaves, directionalStrengh);

				return result;
			}
			ENDCG
		}
	}
}
