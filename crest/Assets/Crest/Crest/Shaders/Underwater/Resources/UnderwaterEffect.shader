// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Underwater/Underwater Effect"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma multi_compile_instancing

			// Use multi_compile because these keywords are copied over from the ocean material. With shader_feature,
			// the keywords would be stripped from builds. Unused shader variants are stripped using a build processor.
			#pragma multi_compile_local __ _SUBSURFACESCATTERING_ON
			#pragma multi_compile_local __ _SUBSURFACESHALLOWCOLOUR_ON
			#pragma multi_compile_local __ _TRANSPARENCY_ON
			#pragma multi_compile_local __ _CAUSTICS_ON
			#pragma multi_compile_local __ _SHADOWS_ON
			#pragma multi_compile_local __ _COMPILESHADERWITHDEBUGINFO_ON
			#pragma multi_compile_local __ _PROJECTION_PERSPECTIVE _PROJECTION_ORTHOGRAPHIC

			#pragma multi_compile_local __ CREST_MENISCUS
			#pragma multi_compile_local __ _FULL_SCREEN_EFFECT
			#pragma multi_compile_local __ _DEBUG_VIEW_OCEAN_MASK

			#if _COMPILESHADERWITHDEBUGINFO_ON
			#pragma enable_d3d11_debug_symbols
			#endif

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			#include "../../OceanGlobals.hlsl"
			#include "../../OceanInputsDriven.hlsl"
			#include "../../OceanShaderData.hlsl"
			#include "../../OceanHelpersNew.hlsl"
			#include "../../OceanShaderHelpers.hlsl"
			#include "../../FullScreenTriangle.hlsl"
			#include "../../OceanEmission.hlsl"

			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestCameraColorTexture);
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture);
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestOceanMaskDepthTexture);

			#include "../UnderwaterEffectShared.hlsl"

			struct Attributes
			{
				uint id : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 viewWS : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings Vert (Attributes input)
			{
				Varyings output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_OUTPUT(Varyings, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.positionCS = GetFullScreenTriangleVertexPosition(input.id);
				output.uv = GetFullScreenTriangleTexCoord(input.id);

				// Compute world space view vector
				output.viewWS = ComputeWorldSpaceView(output.uv);

				return output;
			}

			fixed4 Frag (Varyings input) : SV_Target
			{
				// We need this when sampling a screenspace texture.
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float4 horizonPositionNormal; bool isBelowHorizon;
				GetHorizonData(input.uv, horizonPositionNormal, isBelowHorizon);

				const float2 uvScreenSpace = UnityStereoTransformScreenSpaceTex(input.uv);
				half3 sceneColour = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestCameraColorTexture, uvScreenSpace).rgb;
				float rawDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, uvScreenSpace).x;
				const float mask = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, uvScreenSpace).x;
				const float rawOceanDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskDepthTexture, uvScreenSpace).x;

				bool isOceanSurface; bool isUnderwater; float sceneZ;
				GetOceanSurfaceAndUnderwaterData(uvScreenSpace, rawOceanDepth, mask, isBelowHorizon, rawDepth, isOceanSurface, isUnderwater, sceneZ, 0.0);

				float wt = ComputeMeniscusWeight(uvScreenSpace, mask, horizonPositionNormal, sceneZ);

#if _DEBUG_VIEW_OCEAN_MASK
				return DebugRenderOceanMask(isOceanSurface, isUnderwater, mask, sceneColour);
#endif // _DEBUG_VIEW_OCEAN_MASK

				if (isUnderwater)
				{
					const half3 view = normalize(input.viewWS);
					float3 scenePos = _WorldSpaceCameraPos - view * sceneZ / dot(unity_CameraToWorld._m02_m12_m22, -view);
					const float3 lightDir = _WorldSpaceLightPos0.xyz;
					const half3 lightCol = _LightColor0;
					sceneColour = ApplyUnderwaterEffect(scenePos, sceneColour, lightCol, lightDir, rawDepth, sceneZ, view, isOceanSurface);
				}

				return half4(wt * sceneColour, 1.0);
			}
			ENDHLSL
		}
	}
}
