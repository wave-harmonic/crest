// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Inputs/Foam/Inject SWS"
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

			Texture2D<float> _swsSimulationMask;
			Texture2D<float> _swsH;
			Texture2D<float> _swsGroundHeight;

			CBUFFER_START(CrestPerOceanInput)
			int _WaveBufferSliceIndex;
			float _Weight;
			float _AverageWavelength;
			float _AttenuationInShallows;
			float2 _AxisX;
			float _RespectShallowWaterAttenuation;
			half _DomainWidth;
			float3 _SimOrigin;
			float _Resolution;
			float _DeltaTime;
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

				const float2 worldXZ = UVToWorld(quadUV, _LD_SliceIndex, _CrestCascadeData[_LD_SliceIndex]);
				o.uv = (worldXZ - _SimOrigin.xz) / _DomainWidth + 0.5;

				return o;
			}

			float WaterHeight(float2 uv)
			{
				float h = _swsH.SampleLevel(LODData_linear_clamp_sampler, uv, 0.0).x;

				if (h < 0.001) return 0.0;

				// Add ground height to water height to get height of surface
				h += _swsGroundHeight.SampleLevel(LODData_linear_clamp_sampler, uv, 0.0).x;

				return h;
			}

			half Frag(Varyings input) : SV_Target
			{
				if (any(input.uv != saturate(input.uv))) discard;

				// Use approximation of max curvature as foam term. Seems to grab leading wave edge alright
				float2 dx = float2(1.0 / _Resolution, 0.0);
				float h = WaterHeight(input.uv);
				float h_xm = WaterHeight(input.uv - dx.xy);
				float h_xp = WaterHeight(input.uv + dx.xy);
				float h_zm = WaterHeight(input.uv - dx.yx);
				float h_zp = WaterHeight(input.uv + dx.yx);
				float curvature = max(abs(h_xp + h_xm - 2.0 * h), abs(h_zp + h_zm - 2.0 * h)) / (2.0 * dx.x * _DomainWidth);

				float foam = 10.0 * curvature;

				// Weigh input
				foam *= _Weight;

				// Simulation mask
				foam *= _swsSimulationMask.SampleLevel(LODData_linear_clamp_sampler, input.uv, 0.0).x;
				
				// Domain mask
				float2 offset = abs(input.uv - 0.5);
				float maxOff = max(offset.x, offset.y);
				foam *= smoothstep(0.5, 0.45, maxOff);

				// Integrate
				foam *= unity_DeltaTime.x;

				return foam;
			}
			ENDCG
		}
	}
}
