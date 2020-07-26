// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Adds Gerstner waves everywhere. Must be given batch prepared by ShapeGerstnerBatched.cs.
Shader "Hidden/Crest/Inputs/Animated Waves/Gerstner Batch Global"
{
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
			#pragma multi_compile_local __ CREST_DIRECT_TOWARDS_POINT_INTERNAL
			#pragma multi_compile_local __ CREST_SDF_SHORELINES

			#pragma enable_d3d11_debug_symbols

			#include "UnityCG.cginc"

			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanLODData.hlsl"

			#include "../GerstnerShared.hlsl"

			struct Attributes
			{
				float3 positionOS : POSITION;
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
				// sample ocean depth (this render target should 1:1 match depth texture, so UVs are trivial)
				half4 depth_distance_dirXZ = _LD_TexArray_SeaFloorDepth.Sample(LODData_linear_clamp_sampler, input.uv_slice);


				float4 result = ComputeGerstner(input.worldPosXZ, input.uv_slice, depth_distance_dirXZ.x);

#if CREST_SDF_SHORELINES
				const float2 headingvec = normalize(depth_distance_dirXZ.zw);
				const float lerpDistance = 100.0;
				const float directionalStrengh = 1.0 - clamp(depth_distance_dirXZ.y / lerpDistance, 0.0, 1.0);
				result += ComputeShorelineGerstner(input.worldPosXZ, input.uv_slice, depth_distance_dirXZ.x, depth_distance_dirXZ.y, headingvec, directionalStrengh) * directionalStrengh;
#endif

				return result;
			}
			ENDCG
		}
	}
}
