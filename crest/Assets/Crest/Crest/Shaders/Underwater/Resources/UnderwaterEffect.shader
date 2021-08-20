// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Underwater/Underwater Effect"
{
	SubShader
	{
		// These will be "Off" for fullscreen.
		Cull [_CullMode]
		ZTest [_ZTest]
		ZWrite Off

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
			// Both "__" and "_FULL_SCREEN_EFFECT" are fullscreen triangles. The latter only denotes an optimisation of
			// whether to skip the horizon calculation.
			#pragma multi_compile_local __ _FULL_SCREEN_EFFECT _GEOMETRY_EFFECT_PLANE _GEOMETRY_EFFECT_CONVEX_HULL
			#pragma multi_compile_local __ _DEBUG_VIEW_OCEAN_MASK

#if defined(_GEOMETRY_EFFECT_PLANE) || defined(_GEOMETRY_EFFECT_CONVEX_HULL)
			#define _GEOMETRY_EFFECT 1
#endif

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

#if _GEOMETRY_EFFECT_CONVEX_HULL

#endif

			#include "../UnderwaterEffectShared.hlsl"

			struct Attributes
			{
#if _GEOMETRY_EFFECT
				float3 positionOS : POSITION;
#else
				uint id : SV_VertexID;
#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
#if _GEOMETRY_EFFECT
				float4 screenPosition : TEXCOORD0;
#else
				float2 uv : TEXCOORD0;
#endif
				float3 viewWS : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings Vert (Attributes input)
			{
				Varyings output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_OUTPUT(Varyings, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if _GEOMETRY_EFFECT
				// Use actual geometry instead of full screen triangle.
				output.positionCS = UnityObjectToClipPos(float4(input.positionOS, 1.0));
				output.screenPosition = ComputeScreenPos(output.positionCS);

				// Compute world space view vector - TODO - the below code has XR considerations, and this code does not
				// work. Usually i'd expect a view vector to be (worldPos-_WorldSpaceCameraPos). And viewVS below appears to
				// take a view vector from the camera to the far plane, rather than to the geo, which likely is breaking the
				// rest of the shader...
				float3 worldPos = mul(UNITY_MATRIX_M, float4(input.positionOS, 1.0));
				output.viewWS = _WorldSpaceCameraPos - worldPos;
#else
				output.positionCS = GetFullScreenTriangleVertexPosition(input.id);
				output.uv = GetFullScreenTriangleTexCoord(input.id);

				// Compute world space view vector
				output.viewWS = ComputeWorldSpaceView(output.uv);
#endif

				return output;
			}

			fixed4 Frag (Varyings input) : SV_Target
			{
				// We need this when sampling a screenspace texture.
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if _GEOMETRY_EFFECT
				float2 uv = input.screenPosition.xy / input.screenPosition.w;
#else
				float2 uv = input.uv;
#endif

				float4 horizonPositionNormal; bool isBelowHorizon;
				GetHorizonData(uv, horizonPositionNormal, isBelowHorizon);

				const float2 uvScreenSpace = UnityStereoTransformScreenSpaceTex(uv);
				half3 sceneColour = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestCameraColorTexture, uvScreenSpace).rgb;
				float rawDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, uvScreenSpace).x;
				const float mask = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, uvScreenSpace).x;
				const float rawOceanDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskDepthTexture, uvScreenSpace).x;

				bool isOceanSurface; bool isUnderwater; float sceneZ;
				GetOceanSurfaceAndUnderwaterData(rawOceanDepth, mask, isBelowHorizon, rawDepth, isOceanSurface, isUnderwater, sceneZ, 0.0);

#if _GEOMETRY_EFFECT_CONVEX_HULL
				const float frontFaceBoundaryDepth01 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestWaterBoundaryGeometryTexture, uvScreenSpace).x;
				bool isBeforeFrontFaceBoundary = false;
				bool isAfterBackFaceBoundary = false;

				// return float4(sceneZ01, oceanDepth01, frontFaceBoundaryDepth01, 1);

				if (isUnderwater)
				{
					// scene is after back face boundary
					if (rawDepth < input.positionCS.z)
					{
						isAfterBackFaceBoundary = true;
					}

					if (frontFaceBoundaryDepth01 != 0)
					{
						// scene is before front face boundary
						if (rawDepth > frontFaceBoundaryDepth01)
						{
							return float4(sceneColour, 1.0);
						}
						else
						{
							isBeforeFrontFaceBoundary = true;
						}
					}
				}
#endif

				float wt = ComputeMeniscusWeight(uvScreenSpace, mask, horizonPositionNormal, sceneZ);

#if _DEBUG_VIEW_OCEAN_MASK
				return DebugRenderOceanMask(isOceanSurface, isUnderwater, mask, sceneColour);
#endif // _DEBUG_VIEW_OCEAN_MASK

				if (isUnderwater)
				{
					const half3 view = normalize(input.viewWS);
					float3 scenePos = _WorldSpaceCameraPos - view * sceneZ / dot(unity_CameraToWorld._m02_m12_m22, -view);
#if _GEOMETRY_EFFECT_CONVEX_HULL
					if (isAfterBackFaceBoundary)
					{
						// Cancels out caustics. We will want caustics outside of volume at some point though.
						isOceanSurface = true;
						sceneZ = input.screenPosition.w;
					}

					if (isBeforeFrontFaceBoundary)
					{
						sceneZ -= CrestLinearEyeDepth(frontFaceBoundaryDepth01);
					}
#elif _GEOMETRY_EFFECT_PLANE
					sceneZ -= CrestLinearEyeDepth(input.positionCS.z);
#endif
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
