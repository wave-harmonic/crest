// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Adds Gerstner waves everywhere. Must be given batch prepared by ShapeGerstnerBatched.cs.
Shader "Crest/Inputs/Animated Waves/Gerstner Batch Global"
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
			#pragma multi_compile __ _DIRECT_TOWARDS_POINT

			// TODO - remove this later
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

				// TODO - the below is hardcoded to do a fullscreen quad. i think something like this
				// would draw it properly.
				//o.positionCS = UnityObjectToClipPos(input.positionOS);
				//o.worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xz;
				//o.uv_slice = WorldToUV(o.worldPos);

				o.positionCS = float4(input.positionOS.xy, 0.0, 0.5);

#if UNITY_UV_STARTS_AT_TOP // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
				o.positionCS.y = -o.positionCS.y;
#endif
				float2 worldXZ = UVToWorld(input.uv);
				o.worldPosXZ = worldXZ;
				o.uv_slice = float3(input.uv, _LD_SliceIndex);
				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return ComputeGerstner(input.worldPosXZ, input.uv_slice);
			}
			ENDCG
		}
	}
}
