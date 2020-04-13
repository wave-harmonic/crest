// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders gerstner waves from geometry. Allows localised wave areas. Can fade waves based on UVs - fades to 0
// as U or V approach 0 or 1, with configurable feather width.
Shader "Crest/Inputs/Animated Waves/Gerstner Batch Geometry"
{
	Properties
	{
		[Toggle] _FeatherAtUVExtents("Feather at UV extents", Float) = 0
		_FeatherWidth("Feather width", Range(0.001, 0.5)) = 0.1
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
			#pragma shader_feature _FEATHERATUVEXTENTS_ON

			#include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanLODData.hlsl"

			#include "GerstnerShared.hlsl"

			CBUFFER_START(GerstnerPerMaterial)
			half _FeatherWidth;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 worldPosXZ_uv : TEXCOORD0;
				float4 uv_slice_wt : TEXCOORD1;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);

				o.worldPosXZ_uv.xy = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xz;
				o.worldPosXZ_uv.zw = input.uv;

				o.uv_slice_wt.xyz = WorldToUV(o.worldPosXZ_uv.xy, _LD_SliceIndex);
				o.uv_slice_wt.w = 1.0;
				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float wt = 1.0;

#if _FEATHERATUVEXTENTS_ON
				float2 offset = abs(input.worldPosXZ_uv.zw - 0.5);
				float r_l1 = max(offset.x, offset.y);
				wt = saturate(1.0 - (r_l1 - (0.5 - _FeatherWidth)) / _FeatherWidth);
#endif

				return wt * ComputeGerstner(input.worldPosXZ_uv.xy, input.uv_slice_wt.xyz);
			}
			ENDCG
		}
	}
}
