// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders the geometry to the clip surface texture.

Shader "Crest/Inputs/Clip Surface/Add From Geometry"
{
	SubShader
	{
		Pass
		{
			Blend One One
			Cull Off
			ColorMask RG

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../../OceanHelpers.hlsl"

			struct Attributes
			{
				float3 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 surfacePos : TEXCOORD0;
				float heightWS : TEXCOORD1;
			};

			float3 _InstanceData;

			Varyings Vert(Attributes input)
			{
				Varyings o;
				o.positionCS = UnityObjectToClipPos(input.positionOS);
				o.heightWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).y;

				// move to world
				float3 surfacePos;
				surfacePos.xz = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xz;
				surfacePos.y = 0.0;

				// vertex snapping and lod transition
				float lodAlpha = ComputeLodAlpha(surfacePos, _InstanceData.x);

				// sample shape textures - always lerp between 2 scales, so sample two textures

				// sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
				float wt_smallerLod = (1.0 - lodAlpha) * _LD_Params[_LD_SliceIndex].z;
				float wt_biggerLod = (1.0 - wt_smallerLod) * _LD_Params[_LD_SliceIndex + 1].z;
				// sample displacement textures, add results to current world pos / normal / foam
				const float2 worldXZ = surfacePos.xz;
				half foam = 0.0;
				half sss = 0.;
				if (wt_smallerLod > 0.001)
				{
					SampleDisplacements(_LD_TexArray_AnimatedWaves, WorldToUV(worldXZ), wt_smallerLod, surfacePos, sss);
				}
				if (wt_biggerLod > 0.001)
				{
					SampleDisplacements(_LD_TexArray_AnimatedWaves, WorldToUV_BiggerLod(worldXZ), wt_biggerLod, surfacePos, sss);
				}

				// move to sea level
				surfacePos.y += _OceanCenterPosWorld.y;

				o.surfacePos = surfacePos;

				return o;
			}

			float4 Frag(Varyings input) : SV_Target
			{
				float2 clip = 0;

				if (input.heightWS >= input.surfacePos.y)
				{
					clip = float2(1, 0);
				}
				else if (input.heightWS < input.surfacePos.y)
				{
					clip = float2(0, 1);
				}
				return float4(clip.x, clip.y, 0, 1);
			}
			ENDCG
		}
	}
}
