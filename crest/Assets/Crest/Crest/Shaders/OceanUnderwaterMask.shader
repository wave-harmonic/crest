// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Ocean Underwater Mask"
{
	SubShader
	{
		// Always - Tell Unity not to apply any lighting
		Tags { "LightMode"="Always" "IgnoreProjector"="True" "RenderType"="Opaque" }

		Pass
		{
			// We always disable culling when rendering ocean mask, as we only
			// use it for underwater rendering features.
			Cull Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			// for VFACE
			#pragma target 3.0
			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			struct Attributes
			{
				// The old unity macros require this name and type.
				float4 vertex : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
			};

			#include "OceanHelpers.hlsl"

			uniform float _CrestTime;

			// MeshScaleLerp, FarNormalsWeight, LODIndex (debug), unused
			uniform float4 _InstanceData;

			// Hack - due to SV_IsFrontFace occasionally coming through as true for backfaces,
			// add a param here that forces ocean to be in undrwater state. I think the root
			// cause here might be imprecision or numerical issues at ocean tile boundaries, although
			// i'm not sure why cracks are not visible in this case.
			uniform float _ForceUnderwater;

			Varyings Vert(Attributes v)
			{
				Varyings output;

				float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));

				// Vertex snapping and lod transition
				float lodAlpha;
				SnapAndTransitionVertLayout(_InstanceData.x, worldPos, lodAlpha);

				// Calculate sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
				const float wt_smallerLod = (1. - lodAlpha) * _LD_Params[_LD_SliceIndex].z;
				const float wt_biggerLod = (1. - wt_smallerLod) * _LD_Params[_LD_SliceIndex + 1].z;
				// Sample displacement textures, add results to current world pos / normal / foam
				const float2 positionWS_XZ_before = worldPos.xz;

				// Data that needs to be sampled at the undisplaced position
				if (wt_smallerLod > 0.001)
				{
					half sss = 0.;
					const float3 uv_slice_smallerLod = WorldToUV(positionWS_XZ_before);
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice_smallerLod, wt_smallerLod, worldPos, sss);
				}
				if (wt_biggerLod > 0.001)
				{
					half sss = 0.;
					const float3 uv_slice_biggerLod = WorldToUV_BiggerLod(positionWS_XZ_before);
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice_biggerLod, wt_biggerLod, worldPos, sss);
				}

				output.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.));
				return output;
			}

			half4 Frag(const Varyings input, const float facing : VFACE) : SV_Target
			{
				if(IsUnderwater(facing, _ForceUnderwater))
				{
					return half4(2.0, 2.0, 2.0, 1.0);
				}
				else
				{
					return half4(1.0, 1.0, 1.0, 1.0);
				}
			}
			ENDCG
		}
	}
}
