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
			half _DomainWidth;
			CBUFFER_END

			struct Attributes
			{
				uint VertexID : SV_VertexID;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 worldXZ : TEXCOORD1;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = GetFullScreenTriangleVertexPosition(input.VertexID);

				const float2 quadUV = GetFullScreenTriangleTexCoord(input.VertexID);

				o.worldXZ = UVToWorld(quadUV, _LD_SliceIndex, _CrestCascadeData[_LD_SliceIndex]);
				o.uv = o.worldXZ / _DomainWidth + 0.5;

				return o;
			}

			float g(float2 worldXZ)
			{
				float g = 0.0;
				g += 1.5 * smoothstep(2.0, 0.0, length(worldXZ - 4.0));
				return g;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				if (!all(input.uv == saturate(input.uv))) discard;

				float wt = _Weight;

				float h = _swsH.SampleLevel(LODData_linear_clamp_sampler, input.uv, 0.0).x;

				if (h < 0.001) discard;

				h += g(input.worldXZ);
			
				return half4(0.0, wt * h, 0.0, 0.0);
			}
			ENDCG
		}
	}
}
