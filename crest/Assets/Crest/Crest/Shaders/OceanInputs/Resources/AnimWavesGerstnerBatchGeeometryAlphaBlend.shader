// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Animated Waves/Gerstner Batch Geometry Alpha Blend"
{
	Properties
	{
		_FeatherWidth("Feather width", Range(0.001, 0.5)) = 0.1
	}

	SubShader
	{
		Pass
		{
			Blend DstColor Zero
			ZWrite Off
			ZTest Always
			Cull Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			CBUFFER_START(GerstnerPerMaterial)
			half _FeatherWidth;
			half _Weight;
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
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);

				o.worldPosXZ_uv.xy = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xz;
				o.worldPosXZ_uv.zw = input.uv;

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				float wt = 1.0;

//#if _FEATHERATUVEXTENTS_ON
				float2 offset = abs(input.worldPosXZ_uv.zw - 0.5);
				float r_l1 = max(offset.x, offset.y);
				wt = saturate(1.0 - (r_l1 - (0.5 - _FeatherWidth)) / _FeatherWidth);
//#endif

				return saturate(1.0 - wt * _Weight);
			}
			ENDCG
		}
	}
}
