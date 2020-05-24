// Crest Ocean System

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
		[Toggle] _CompileShaderWithDebugInfo("Compile Shader With Debug Info (D3D11)", Float) = 0
	}

	SubShader
	{
		Tags{ "LightMode" = "ForwardBase" "Queue" = "Geometry+510" "IgnoreProjector" = "True" "RenderType" = "Opaque" }

		GrabPass
		{
			"_BackgroundTexture"
		}

		Pass
		{
			// The ocean surface will render after the skirt, and overwrite the pixels
			ZWrite Off
			ZTest Always

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma shader_feature _SUBSURFACESCATTERING_ON
			#pragma shader_feature _SUBSURFACESHALLOWCOLOUR_ON
			#pragma shader_feature _TRANSPARENCY_ON
			#pragma shader_feature _CAUSTICS_ON
			#pragma shader_feature _SHADOWS_ON
			#pragma shader_feature _COMPILESHADERWITHDEBUGINFO_ON

			#if _COMPILESHADERWITHDEBUGINFO_ON
			#pragma enable_d3d11_debug_symbols
			#endif

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			#include "../OceanGlobals.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanLODData.hlsl"
			#include "../OceanHelpersNew.hlsl"
			#include "UnderwaterShared.hlsl"

			float _HeightOffset;

			#include "../OceanEmission.hlsl"

			#define MAX_OFFSET 5.0

			sampler2D _CameraDepthTexture;
			sampler2D _Normals;

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				half4 foam_screenPos : TEXCOORD1;
				half4 grabPos : TEXCOORD2;
				float3 positionWS : TEXCOORD3;
			};

			Varyings Vert(Attributes input)
			{
				Varyings o;

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
				const float3 right   = unity_CameraToWorld._11_21_31;
				const float3 up      = unity_CameraToWorld._12_22_32;
				const float3 forward = unity_CameraToWorld._13_23_33;

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
						o.positionWS += min(IntersectRayWithWaterSurface(o.positionWS, up), MAX_OFFSET) * up;

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
				const half3 view = normalize(_WorldSpaceCameraPos - input.positionWS);

				const float pixelZ = LinearEyeDepth(input.positionCS.z);
				const half3 screenPos = input.foam_screenPos.yzw;
				const half2 uvDepth = screenPos.xy / screenPos.z;
				const float sceneZ01 = tex2D(_CameraDepthTexture, uvDepth).x;
				const float sceneZ = LinearEyeDepth(sceneZ01);

				const float3 lightDir = _WorldSpaceLightPos0.xyz;
				const half3 n_pixel = 0.0;
				const half3 bubbleCol = 0.0;

				float3 dummy = 0.0;
				half sss = 0.;
				const float3 uv_slice = WorldToUV(_WorldSpaceCameraPos.xz);
				SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice, 1.0, dummy, sss);

				// depth and shadow are computed in ScatterColour when underwater==true, using the LOD1 texture.
				const float depth = 0.0;
				const half shadow = 1.0;

				const half3 scatterCol = ScatterColour(depth, _WorldSpaceCameraPos, lightDir, view, shadow, true, true, sss);

				half3 sceneColour = tex2D(_BackgroundTexture, input.grabPos.xy / input.grabPos.w).rgb;

#if _CAUSTICS_ON
				if (sceneZ01 != 0.0)
				{
					ApplyCaustics(view, lightDir, sceneZ, _Normals, true, sceneColour);
				}
#endif // _CAUSTICS_ON

				half3 col = lerp(sceneColour, scatterCol, 1.0 - exp(-_DepthFogDensity.xyz * sceneZ));

				return half4(col, 1.0);
			}
			ENDCG
		}
	}
}
