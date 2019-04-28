// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// A batch of Gerstner components
Shader "Crest/Inputs/Animated Waves/Gerstner Batch"
{
	Properties
	{
		// This is purely for convenience - it makes the value appear in material section of the inspector and is useful for debugging.
		_NumInBatch("_NumInBatch", float) = 0
	}

	SubShader
	{
		Pass
		{
			Blend SrcAlpha One
			ZWrite Off
			ZTest Always
			Cull Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#include "UnityCG.cginc"
			#include "../../OceanLODData.hlsl"

			// IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatched.cs, both must be updated together!
			#define BATCH_SIZE 32

			#define PI 3.141593

			half _AttenuationInShallows;
			uint _NumWaveVecs;

			half4 _TwoPiOverWavelengths[BATCH_SIZE / 4];
			half4 _Amplitudes[BATCH_SIZE / 4];
			half4 _WaveDirX[BATCH_SIZE / 4];
			half4 _WaveDirZ[BATCH_SIZE / 4];
			half4 _Phases[BATCH_SIZE / 4];
			half4 _ChopAmps[BATCH_SIZE / 4];

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 worldPos_wt : TEXCOORD0;
				float2 uv : TEXCOORD1;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = float4(input.positionOS.xy, 0.0, 0.5);

#if UNITY_UV_STARTS_AT_TOP // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
				o.positionCS.y = -o.positionCS.y;
#endif

				float2 worldXZ = LD_0_UVToWorld(input.uv);

				o.worldPos_wt.xy = worldXZ;
				o.worldPos_wt.z = input.color.x;

				o.uv = input.uv;

				return o;
			}

#include "AnimWavesGerstnerBatchFunction.hlsl"

			half4 Frag(Varyings input) : SV_Target
			{
				return AnimWavesGerstnerBatchFunction(
					float4(input.uv, 0, 0),
					input.worldPos_wt.xy,
					input.worldPos_wt.z
				);
			}

			ENDCG
		}
	}
}
