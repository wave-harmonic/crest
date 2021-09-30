// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// This adds the height from the geometry. This allows setting the water height to some level for rivers etc, but still
// getting the waves added on top.

Shader "Crest/Inputs/Animated Waves/Set Base Water Height Using Geometry"
{
	Properties
	{
		[HideInInspector] _ObsoleteMessage( "Use <i>Crest/Inputs/Sea Floor Depth/Set Base Water Height Using Geometry</i> instead.", Float ) = 0
		[Enum(BlendOp)] _BlendOp("Blend Op", Int) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc("Src Blend Mode", Int) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeTgt("Tgt Blend Mode", Int) = 1
		[Enum(ColorWriteMask)] _ColorWriteMask("Color Write Mask", Int) = 15
	}

	SubShader
	{
		Pass
		{
			BlendOp [_BlendOp]
			Blend [_BlendModeSrc] [_BlendModeTgt]
			ColorMask [_ColorWriteMask]

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanHelpersNew.hlsl"

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

			half4 Frag(Varyings input) : SV_Target
			{
				float3 uv = WorldToUV(input.worldPos.xz, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);
				half seaLevelOffset = _LD_TexArray_SeaFloorDepth.SampleLevel(LODData_linear_clamp_sampler, uv, 0.0).y;

				// Write displacement to get from sea level of ocean to the y value of this geometry
				float addHeight = input.worldPos.y - _OceanCenterPosWorld.y - seaLevelOffset;
				return _Weight * half4(0.0, addHeight, 0.0, 0.0);
			}
			ENDCG
		}
	}

	CustomEditor "Crest.ObsoleteShaderGUI"
}
