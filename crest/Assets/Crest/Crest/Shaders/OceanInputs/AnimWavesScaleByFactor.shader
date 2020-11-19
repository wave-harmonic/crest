﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// This writes straight into the displacement texture and sets the water height to the y value of the geometry.

Shader "Crest/Inputs/Animated Waves/Scale By Factor"
{
	Properties
	{
		[Enum(ColorWriteMask)] _ColorWriteMask("Color Write Mask", Int) = 15
	}

	SubShader
	{
		Pass
		{
			Blend Zero SrcColor
			ColorMask [_ColorWriteMask]

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"

			CBUFFER_START(CrestPerOceanInput)
			float _Weight;
			float3 _DisplacementAtInputPosition;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				
				o.worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement
				o.worldPos.xz -= _DisplacementAtInputPosition.xz;
				
				o.positionCS = mul(UNITY_MATRIX_VP, float4(o.worldPos, 1.0));

				return o;
			}

			half4 Frag( Varyings input ) : SV_Target
			{
				return 0.35;
			}
			ENDCG
		}
	}
}