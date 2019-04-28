// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// solve 2D wave equation
Shader "Hidden/Crest/Simulation/Update Dynamic Waves"
{
	SubShader
	{
		Pass
		{
			Name "UpdateDynWaves"
			Blend Off
			ZWrite Off
			ZTest Always

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#define MIN_DT 0.00001

			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"

			half _Damping;
			float2 _LaplacianAxisX;
			half _Gravity;
			float _SimDeltaTime;
			float _SimDeltaTimePrev;

			struct Attributes
			{
				float4 positionCS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 positionWS_XZ : TEXCOORD0;
				float2 uv : TEXCOORD1;
			};

			// How many samples we want in one wave. trade quality for perf.
			float _TexelsPerWave;
			// Current resolution
			float _GridSize;

			Varyings Vert(Attributes input)
			{
				Varyings o = (Varyings)0;

				o.positionCS = input.positionCS;

#if !UNITY_UV_STARTS_AT_TOP // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
				o.positionCS.y = -o.positionCS.y;
#endif

				o.uv = input.uv;

				// lod data 1 is current frame, compute world pos from quad uv
				o.positionWS_XZ = LD_1_UVToWorld(input.uv);

				return o;
			}

#include "UpdateDynWavesFunction.hlsl"

			half2 Frag(Varyings input) : SV_Target
			{
				return UpdateDynWavesFunction(input.uv, input.positionWS_XZ);
			}
			ENDCG
		}
	}
}
