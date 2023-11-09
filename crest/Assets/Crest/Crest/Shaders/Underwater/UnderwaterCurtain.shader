﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Underwater Curtain"
{
	Properties
	{
		// Most properties are copied over from the main ocean material on startup

		// Shader features need to be statically configured - it works to dynamically configure them in editor, but in standalone
		// builds they need to be preconfigured. This is a pitfall unfortunately - the settings need to be manually matched.
		[Toggle] _Shadows("Shadowing", Float) = 0
		[Toggle] _SubSurfaceScattering("Sub-Surface Scattering", Float) = 1
		[Toggle] _SubSurfaceShallowColour("Sub-Surface Shallow Colour", Float) = 1
		[Toggle] _Transparency("Transparency", Float) = 1
		[Toggle] _Caustics("Caustics", Float) = 1
	}

	SubShader
	{
		Tags{ "LightMode" = "ForwardBase" "Queue" = "Geometry+510" "IgnoreProjector" = "True" "RenderType" = "Opaque" }

		Pass
		{
			// The ocean surface will render after the skirt, and overwrite the pixels
			ZWrite Off
			ZTest Always
			Blend DstColor SrcColor

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			// #pragma enable_d3d11_debug_symbols

			// Use multi_compile because these keywords are copied over from the ocean material. With shader_feature,
			// the keywords would be stripped from builds. Unused shader variants are stripped using a build processor.
			#pragma multi_compile_local __ _SUBSURFACESCATTERING_ON
			#pragma multi_compile_local __ _SUBSURFACESHALLOWCOLOUR_ON
			#pragma multi_compile_local __ _TRANSPARENCY_ON
			#pragma multi_compile_local __ _CAUSTICS_ON
			#pragma multi_compile_local __ _SHADOWS_ON

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			#include "../Helpers/BIRP/Core.hlsl"

			#include "../ShaderLibrary/Common.hlsl"

			#include "../OceanGlobals.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanShaderData.hlsl"
			#include "../OceanHelpersNew.hlsl"
			#include "../OceanShaderHelpers.hlsl"
			#include "../OceanLightingHelpers.hlsl"
			#include "UnderwaterShared.hlsl"

			#include "../OceanEmission.hlsl"

			#define MAX_OFFSET 5.0

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
				half4 foam_screenPos : TEXCOORD1;
				half4 grabPos : TEXCOORD2;
				float3 positionWS : TEXCOORD3;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_OUTPUT(Varyings, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				// Goal of this vert shader is to place a sheet of triangles in front of the camera. The geometry has
				// two rows of verts, the top row and the bottom row (top and bottom are view relative). The bottom row
				// is pushed down below the bottom of the screen. Every vert in the top row can take any vertical position
				// on the near plane in order to find the meniscus of the water. Due to render states, the ocean surface
				// will stomp over the results of this shader. The ocean surface has necessary code to render from underneath
				// and correctly fog etc.

				// Potential optimisations (note that this shader runs over a few dozen vertices, not over screen pixels!):
				// - when looking down through the water surface, the code currently pushes the top verts of the skirt
				//   up to cover the whole screen, but it only needs to get pushed up to the horizon level to meet the water surface

				// view coordinate frame for camera
				const float3 right   = UNITY_MATRIX_I_V._11_21_31;
				const float3 up      = UNITY_MATRIX_I_V._12_22_32;
				const float3 forward = -UNITY_MATRIX_I_V._13_23_33;

				const float3 nearPlaneCenter = _WorldSpaceCameraPos + forward * _ProjectionParams.y * 1.001;
				// Spread verts across the near plane.
				const float aspect = _ScreenParams.x / _ScreenParams.y;
				o.positionWS = nearPlaneCenter
					+ 2.6 * unity_CameraInvProjection._m11 * aspect * right * input.positionOS.x * _ProjectionParams.y
					+ up * input.positionOS.z * _ProjectionParams.y;

				// Isolate topmost edge
				if (input.positionOS.z > 0.45)
				{
					const float3 posOnNearPlane = o.positionWS;

					// Only compute intersection of water if viewer is looking "horizontal-ish". When the viewer starts to look
					// too much up or down, the intersection between the near plane and the water surface can be complex.
					if (abs(forward.y) < CREST_MAX_UPDOWN_AMOUNT)
					{
						// move vert in the up direction, but only to an extent, otherwise numerical issues can cause weirdness
						o.positionWS += min(IntersectRayWithWaterSurface(o.positionWS, up, _CrestCascadeData[_LD_SliceIndex]), MAX_OFFSET) * up;

						// Move the geometry towards the horizon. As noted above, the skirt will be stomped by the ocean
						// surface render. If we project a bit towards the horizon to make a bit of overlap then we can reduce
						// the chance render issues from cracks/gaps with down angles, or of the skirt being too high for up angles.
						float3 horizonPoint = _WorldSpaceCameraPos + (posOnNearPlane - _WorldSpaceCameraPos) * 10000.0;
						horizonPoint.y = _OceanCenterPosWorld.y;
						const float3 horizonDir = normalize(horizonPoint - _WorldSpaceCameraPos);
						const float3 projectionOfHorizonOnNearPlane = _WorldSpaceCameraPos + horizonDir / dot(horizonDir, forward);
						o.positionWS = lerp(o.positionWS, projectionOfHorizonOnNearPlane, 0.1);
					}
					else if (_HeightOffset < -1.0)
					{
						// Deep under water - always push top edge up to cover screen
						o.positionWS += MAX_OFFSET * up;
					}
					else
					{
						// Near water surface - this is where the water can intersect the lens in nontrivial ways and causes problems
						// for finding the meniscus / water line.

						// Push top edge up if we are looking down so that the screen defaults to looking underwater.
						// Push top edge down if we are looking up so that the screen defaults to looking out of water.
						o.positionWS -= sign(forward.y) * MAX_OFFSET * up;
					}

					// Test - always put top row of verts at water horizon, because then it will always meet the water
					// surface. Good idea but didnt work because it then does underwater shading on opaque surfaces which
					// can be ABOVE the water surface. Not sure if theres any way around this.
					o.positionCS = mul(UNITY_MATRIX_VP, float4(o.positionWS, 1.0));
					o.positionCS.z = o.positionCS.w;
				}
				else
				{
					// Bottom row of verts - push them down below bottom of screen
					o.positionWS -= MAX_OFFSET * up;

					o.positionCS = mul(UNITY_MATRIX_VP, float4(o.positionWS, 1.0));
					o.positionCS.z = o.positionCS.w;
				}

				o.foam_screenPos.x = 0.0;
				o.foam_screenPos.yzw = ComputeScreenPos(o.positionCS).xyw;
				o.grabPos = ComputeGrabScreenPos(o.positionCS);

				o.uv = input.uv;

				return o;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				// We need this when sampling a screenspace texture.
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				const half3 view = normalize(_WorldSpaceCameraPos - input.positionWS);

				const float pixelZ = LinearEyeDepth(input.positionCS.z);
				const half3 screenPos = input.foam_screenPos.yzw;
				const half2 uvDepth = screenPos.xy / screenPos.z;
				const float sceneZ01 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uvDepth).x;
				const float sceneZ = LinearEyeDepth(sceneZ01);

				const CascadeParams cascadeData0 = _CrestCascadeData[_LD_SliceIndex];
				const CascadeParams cascadeData1 = _CrestCascadeData[_LD_SliceIndex + 1];

				const float3 lightDir = _WorldSpaceLightPos0.xyz;
				const half3 lightCol = _LightColor0;
				const half3 n_pixel = 0.0;
				const half3 bubbleCol = 0.0;

				const half shadow = 1.0;
				const half sss = 0.0;

				half seaFloorDepth = CREST_OCEAN_DEPTH_BASELINE;
#if _SUBSURFACESHALLOWCOLOUR_ON
				{
					// compute scatter colour from cam pos. two scenarios this can be called:
					// 1. rendering ocean surface from bottom, in which case the surface may be some distance away. use the scatter
					//    colour at the camera, not at the surface, to make sure its consistent.
					// 2. for the underwater skirt geometry, we don't have the lod data sampled from the verts with lod transitions etc,
					//    so just approximate by sampling at the camera position.
					// this used to sample LOD1 but that doesnt work in last LOD, the data will be missing.
					const float3 uv_slice = WorldToUV(_WorldSpaceCameraPos.xz, cascadeData0, _LD_SliceIndex);
					SampleSeaDepth(_LD_TexArray_SeaFloorDepth, uv_slice, 1.0, seaFloorDepth);
				}
#endif // _SUBSURFACESHALLOWCOLOUR_ON

				const half3 scatterCol = ScatterColour(seaFloorDepth, shadow, sss, view, _CrestAmbientLighting, lightDir, lightCol, true);

				half3 sceneColour = 1.0;

#if _CAUSTICS_ON
				if (sceneZ01 != 0.0)
				{
					float3 scenePos = _WorldSpaceCameraPos - view * sceneZ / dot(UNITY_MATRIX_I_V._13_23_33, view);
					ApplyCaustics(_CausticsTiledTexture, _CausticsDistortionTiledTexture, input.positionCS.xy, scenePos, lightDir, sceneZ, true, sceneColour, _LD_SliceIndex + 1, cascadeData1);
				}
#endif // _CAUSTICS_ON

				half3 col = lerp(sceneColour, scatterCol, 1.0 - exp(-_DepthFogDensity.xyz * sceneZ));

				return half4(col, 1.0);
			}
			ENDCG
		}
	}
}
