// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Persistent foam sim
Shader "Hidden/Crest/Simulation/Update Foam"
{
	SubShader
	{
		Pass
		{
			Name "UpdateFoam"
			Blend Off
			ZWrite Off
			ZTest Always

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"

			float _FoamFadeRate;
			float _WaveFoamStrength;
			float _WaveFoamCoverage;
			float _ShorelineFoamMaxDepth;
			float _ShorelineFoamStrength;
			float _SimDeltaTime;
			float _SimDeltaTimePrev;

			struct Attributes
			{
				// the input geom has clip space positions
				float4 positionCS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 uv_uv_lastframe : TEXCOORD0;
				float2 positionWS_XZ : TEXCOORD1;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				o.positionCS = input.positionCS;

#if !UNITY_UV_STARTS_AT_TOP // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
				o.positionCS.y = -o.positionCS.y;
#endif

				o.uv_uv_lastframe.xy = input.uv;

				// lod data 1 is current frame, compute world pos from quad uv
				o.positionWS_XZ = LD_1_UVToWorld(input.uv);
				o.uv_uv_lastframe.zw = LD_0_WorldToUV(o.positionWS_XZ);

				return o;
			}

#include "UpdateFoamFunction.hlsl"

			half Frag(Varyings input) : SV_Target
			{
				float4 uv = float4(input.uv_uv_lastframe.xy, 0.0, 0.0);
				float4 uv_lastframe = float4(input.uv_uv_lastframe.zw, 0.0, 0.0);

				return UpdateFoamFunction(uv, uv_lastframe, input.positionWS_XZ);
			}
			ENDCG
		}
	}
}
