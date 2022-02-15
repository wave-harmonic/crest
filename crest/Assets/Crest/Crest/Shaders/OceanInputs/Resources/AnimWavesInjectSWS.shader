// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)


Shader "Hidden/Crest/Inputs/Animated Waves/Inject SWS"
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
			//#pragma enable_d3d11_debug_symbols

			#include "UnityCG.cginc"

			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanHelpersNew.hlsl"
			#include "../../FullScreenTriangle.hlsl"

			Texture2D<float> _swsH;

			CBUFFER_START(CrestPerOceanInput)
			int _WaveBufferSliceIndex;
			float _Weight;
			float _AverageWavelength;
			float _AttenuationInShallows;
			float2 _AxisX;
			float _RespectShallowWaterAttenuation;
			half _MaximumAttenuationDepth;
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

				const float2 quadUV = GetFullScreenTriangleTexCoord(input.VertexID);

				o.uv = UVToWorld(quadUV, _LD_SliceIndex, _CrestCascadeData[_LD_SliceIndex]);
				o.uv /= 64.0;
				o.uv += 0.5;

				return o;
			}

			half4 Frag( Varyings input ) : SV_Target
			{
				float wt = _Weight;

				float h = _swsH.SampleLevel(LODData_linear_clamp_sampler, input.uv, 0.0).x;
			
				return half4(0.0, wt * h, 0.0, 0.0);
			}
			ENDCG
		}
	}
}
