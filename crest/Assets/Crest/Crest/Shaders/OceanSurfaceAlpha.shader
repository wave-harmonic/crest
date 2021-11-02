// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders alpha geometry overlaid on ocean surface. Samples the ocean shape texture in the vertex shader to track
// the surface. Requires the right texture to be assigned (see RenderAlphaOnSurface script).
Shader "Crest/Ocean Surface Alpha"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Alpha("Alpha Multiplier", Range(0.0, 1.0)) = 1.0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc("Src Blend Mode", Int) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeTgt("Tgt Blend Mode", Int) = 10
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }

		Pass
		{
			Blend [_BlendModeSrc] [_BlendModeTgt]

			ZWrite Off
			// Depth offset to stop intersection with water. "Factor" and "Units". typical seems to be (-1,-1). (-0.5,0) gives
			// pretty good results for me when alpha geometry is fairly well matched but fails when alpha geo is too low res.
			// the ludicrously large value below seems to work in most of my tests.
			Offset 0, -1000000

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			#include "OceanConstants.hlsl"
			#include "OceanGlobals.hlsl"
			#include "OceanInputsDriven.hlsl"
			#include "OceanHelpersNew.hlsl"
			#include "OceanVertHelpers.hlsl"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			half _Alpha;

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float lodAlpha : TEXCOORD2;
				UNITY_FOG_COORDS(3)

				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_OUTPUT(Varyings, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				const CascadeParams cascadeData0 = _CrestCascadeData[_LD_SliceIndex];
				const CascadeParams cascadeData1 = _CrestCascadeData[_LD_SliceIndex + 1];

				// move to world
				float3 worldPos;
				worldPos.xz = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xz;
				worldPos.y = 0.0;

				// vertex snapping and lod transition
				float meshScaleLerp = _CrestPerCascadeInstanceData[_LD_SliceIndex]._meshScaleLerp;
				float lodAlpha = ComputeLodAlpha(worldPos, meshScaleLerp, cascadeData0);

				// sample shape textures - always lerp between 2 scales, so sample two textures

				// sample displacement textures, add results to current world pos / normal / foam
				half foam = 0.0;
				// sample weight. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
				float wt_smallerLod = (1.0 - lodAlpha) * cascadeData0._weight;
				{
					const float3 uv_slice = WorldToUV(worldPos.xz, cascadeData0, _LD_SliceIndex);
					half variance = 0.0;
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice, wt_smallerLod, worldPos, variance);
				}
				const float wt_biggerLod = (1.0 - wt_smallerLod) * cascadeData1._weight;
				{
					// sample weight. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
					const float3 uv_slice = WorldToUV(worldPos.xz, cascadeData1, _LD_SliceIndex + 1);
					half variance = 0.0;
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice, wt_biggerLod, worldPos, variance);
				}

				// Data that needs to be sampled at the displaced position.
				float seaLevelOffset = 0.0;
				{
					const float3 uv_slice_smallerLodDisp = WorldToUV(worldPos.xz, cascadeData0, _LD_SliceIndex);
					const float3 uv_slice_biggerLodDisp = WorldToUV( worldPos.xz, cascadeData1, _LD_SliceIndex + 1 );
					SampleSeaLevelOffset(_LD_TexArray_SeaFloorDepth, uv_slice_smallerLodDisp, wt_smallerLod, seaLevelOffset);
					SampleSeaLevelOffset(_LD_TexArray_SeaFloorDepth, uv_slice_biggerLodDisp, wt_biggerLod, seaLevelOffset);
				}

				// move to sea level
				worldPos.y += _OceanCenterPosWorld.y + seaLevelOffset;

				// view-projection
				o.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

				// For clip surface sampling
				o.worldPos = worldPos;
				o.lodAlpha = lodAlpha;

				o.uv = TRANSFORM_TEX(input.uv, _MainTex);
				UNITY_TRANSFER_FOG(o, o.positionCS);
				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				// We don't want decals etc floating on nothing
				ApplyOceanClipSurface(input.worldPos, input.lodAlpha);

				half4 col = tex2D(_MainTex, input.uv);

				UNITY_APPLY_FOG(input.fogCoord, col);

				col.a *= _Alpha;

				return col;
			}
			ENDCG
		}
	}
}
