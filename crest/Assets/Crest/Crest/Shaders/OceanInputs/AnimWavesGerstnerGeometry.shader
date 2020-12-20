// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Adds Gestner waves to world

Shader "Crest/Inputs/Animated Waves/Gerstner Geometry"
{
	Properties
	{
		[Toggle] _WeightFromVertexColourRed("Weight from vertex colour (red channel)", Float) = 0
		[Toggle] _FeatherAtUVExtents("Feather at UV extents", Float) = 0
		_FeatherWidth("Feather width", Range(0.001, 0.5)) = 0.1
	}

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
			#pragma shader_feature_local _WEIGHTFROMVERTEXCOLOURRED_ON
			#pragma shader_feature_local _FEATHERATUVEXTENTS_ON

			#include "UnityCG.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanHelpersNew.hlsl"

			Texture2DArray _WaveBuffer;

			CBUFFER_START(CrestPerOceanInput)
			int _WaveBufferSliceIndex;
			float _Weight;
			float _AverageWavelength;
			float _AttenuationInShallows;
			CBUFFER_END

			CBUFFER_START(GerstnerPerMaterial)
			half _FeatherWidth;
			float3 _DisplacementAtInputPosition;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
#if _WEIGHTFROMVERTEXCOLOURRED_ON
				float3 colour : COLOR0;
#endif
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 uvGeo_uvWaves : TEXCOORD0;
				float2 worldPosXZ : TEXCOORD3;
				float4 uv_slice_wt : TEXCOORD1;
#if _WEIGHTFROMVERTEXCOLOURRED_ON
				float weight : TEXCOORD2;
#endif
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				o.uvGeo_uvWaves.xy = input.uv;

				float3 worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0)).xyz;
				// Correct for displacement
				worldPos.xz -= _DisplacementAtInputPosition.xz;
				o.worldPosXZ = worldPos.xz;
				
				o.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

				o.uv_slice_wt.xyz = WorldToUV(o.worldPosXZ, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);

				o.uv_slice_wt.w = 1.0;
#if _WEIGHTFROMVERTEXCOLOURRED_ON
				o.uv_slice_wt.w = input.colour.x;
#endif

				float scale = 0.5f * (1 << _WaveBufferSliceIndex);
				o.uvGeo_uvWaves.zw = o.worldPosXZ / scale;

				return o;
			}
			
			half4 Frag( Varyings input ) : SV_Target
			{
				float wt = _Weight;

#if _FEATHERATUVEXTENTS_ON
				float2 offset = abs(input.uvGeo_uvWaves.xy - 0.5);
				float r_l1 = max(offset.x, offset.y);
				wt *= saturate(1.0 - (r_l1 - (0.5 - _FeatherWidth)) / _FeatherWidth);
#endif

				const half depth = _LD_TexArray_SeaFloorDepth.SampleLevel(LODData_linear_clamp_sampler, input.uv_slice_wt.xyz, 0.0).x;
				half depth_wt = saturate(2.0 * depth / _AverageWavelength);
				wt *= _AttenuationInShallows * depth_wt + (1.0 - _AttenuationInShallows);

				return wt * _WaveBuffer.SampleLevel( sampler_Crest_linear_repeat, float3(input.uvGeo_uvWaves.zw, _WaveBufferSliceIndex), 0 );
			}
			ENDCG
		}
	}
}
