﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Inputs/Animated Waves/Inject SWS"
{
	SubShader
	{
		// Additive blend everywhere
		Blend SrcAlpha OneMinusSrcAlpha
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
			Texture2D<float> _swsGroundHeight;
			Texture2D<float> _swsSimulationMask;

			CBUFFER_START(CrestPerOceanInput)
			int _WaveBufferSliceIndex;
			float _Weight;
			float _AverageWavelength;
			float _AttenuationInShallows;
			float2 _AxisX;
			float _RespectShallowWaterAttenuation;
			half _DomainWidth;
			float3 _SimOrigin;
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
				o.uv = (o.worldXZ - _SimOrigin.xz) / _DomainWidth + 0.5;

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				// Over scan to ensure signal continued off the edges which helps at low LODs
				const float overscan = 0.0; // 0.2
				if (!all(input.uv == clamp(input.uv, -overscan, 1.0 + overscan))) discard;

				float wt = _Weight;

				float h = _swsH.SampleLevel(LODData_linear_clamp_sampler, input.uv, 0.0).x;
				float heightRaw = h;

				if (h < 0.001) h = 0.0;// -= 0.1;

				// Add ground height to water height to get world height of surface
				h += _swsGroundHeight.SampleLevel(LODData_linear_clamp_sampler, input.uv, 0.0).x;

				// Move to world space
				h += _SimOrigin.y;

				// Make relative to sea level
				h -= _OceanCenterPosWorld.y;

				float alpha = _swsSimulationMask.SampleLevel(LODData_linear_clamp_sampler, input.uv, 0.0).x;

				// Fade out when approaching dry. Does .. something.
				alpha *= saturate(heightRaw / 0.02);

				// Fade out at edge of domain
				float2 offset = abs(input.uv - 0.5);
				float maxOff = max(offset.x, offset.y);
				alpha *= smoothstep(0.5, 0.45, maxOff);

				// Power up alpha to bring anim waves further in towards shore
				return half4(0.0, wt * h, 0.0, pow(alpha, 2.0));
			}
			ENDCG
		}
	}
}