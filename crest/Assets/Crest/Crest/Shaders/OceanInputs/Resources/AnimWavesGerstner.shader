// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Adds Gestner waves to world

Shader "Hidden/Crest/Inputs/Animated Waves/Gerstner Global"
{
	SubShader
	{
		// Additive blend everywhere
		Blend One One
		ZWrite Off
		ZTest Always
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma enable_d3d11_debug_symbols

			#include "UnityCG.cginc"

			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanHelpersNew.hlsl"
			#include "../../FullScreenTriangle.hlsl"

			Texture2DArray _WaveBuffer;

			CBUFFER_START(CrestPerOceanInput)
			int _WaveBufferSliceIndex;
			float _Weight;
			CBUFFER_END

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

				float2 worldPosXZ = UVToWorld( GetFullScreenTriangleTexCoord( input.VertexID ), _LD_SliceIndex, _CrestCascadeData[_LD_SliceIndex] );

				float scale = (float)(1 << _WaveBufferSliceIndex);
				o.uv = worldPosXZ / scale;

				return o;
			}
			
			half4 Frag( Varyings input ) : SV_Target
			{
				return _Weight * _WaveBuffer.SampleLevel( sampler_Crest_linear_repeat, float3(input.uv, _WaveBufferSliceIndex), 0 );
			}
			ENDCG
		}
	}
}
