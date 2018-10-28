// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Underwater Skirt"
{
	Properties
	{
		[NoScaleOffset] _Normals ( "    Normals", 2D ) = "bump" {}
		_Diffuse("Diffuse", Color) = (0.2, 0.05, 0.05, 1.0)
		[Toggle] _SubSurfaceScattering("Sub-Surface Scattering", Float) = 1
		_SubSurfaceColour("    Colour", Color) = (0.0, 0.48, 0.36)
		_SubSurfaceBase("    Base Mul", Range(0.0, 2.0)) = 0.6
		_SubSurfaceSun("    Sun Mul", Range(0.0, 10.0)) = 0.8
		_SubSurfaceSunFallOff("    Sun Fall-Off", Range(1.0, 16.0)) = 4.0
		[Toggle] _SubSurfaceHeightLerp("Sub-Surface Scattering Height Lerp", Float) = 1
		_SubSurfaceHeightMax("    Height Max", Range(0.0, 50.0)) = 3.0
		_SubSurfaceHeightPower("    Height Power", Range(0.01, 10.0)) = 1.0
		_SubSurfaceCrestColour("    Crest Colour", Color) = (0.42, 0.69, 0.52)
		[Toggle] _SubSurfaceShallowColour("Sub-Surface Shallow Colour", Float) = 1
		_SubSurfaceDepthMax("    Depth Max", Range(0.01, 50.0)) = 3.0
		_SubSurfaceDepthPower("    Depth Power", Range(0.01, 10.0)) = 1.0
		_SubSurfaceShallowCol("    Shallow Colour", Color) = (0.42, 0.75, 0.69)
		[Toggle] _Transparency("Transparency", Float) = 1
		_DepthFogDensity("    Density", Vector) = (0.28, 0.16, 0.24, 1.0)
		[Toggle] _Caustics("Caustics", Float) = 1
		[NoScaleOffset] _CausticsTexture("    Caustics", 2D) = "black" {}
		_CausticsTextureScale("    Scale", Range(0.0, 25.0)) = 5.0
		_CausticsTextureAverage("    Texture Average Value", Range(0.0, 1.0)) = 0.07
		_CausticsStrength("    Strength", Range(0.0, 10.0)) = 3.2
		_CausticsFocalDepth("    Focal Depth", Range(0.0, 25.0)) = 2.0
		_CausticsDepthOfField("    Depth Of Field", Range(0.01, 10.0)) = 0.33
		_CausticsDistortionScale("    Distortion Scale", Range(0.01, 50.0)) = 10.0
		_CausticsDistortionStrength("    Distortion Strength", Range(0.0, 0.25)) = 0.075
		[Toggle] _Shadows("Shadows", Float) = 1
		[Toggle] _CompileShaderWithDebugInfo("Compile Shader With Debug Info (D3D11)", Float) = 0
	}

	SubShader
	{
		Tags{ "LightMode" = "ForwardBase" "Queue" = "Geometry+510" "IgnoreProjector" = "True" "RenderType" = "Opaque" }
		LOD 100

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
			#pragma vertex vert
			#pragma fragment frag
			
			#pragma shader_feature _SUBSURFACESCATTERING_ON
			#pragma shader_feature _SUBSURFACEHEIGHTLERP_ON
			#pragma shader_feature _SUBSURFACESHALLOWCOLOUR_ON
			#pragma shader_feature _TRANSPARENCY_ON
			#pragma shader_feature _CAUSTICS_ON
			#pragma shader_feature _SHADOWS_ON

			#pragma shader_feature _COMPILESHADERWITHDEBUGINFO_ON

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "../../Crest/Shaders/OceanLODData.hlsl"

			uniform float _CrestTime;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				half4 foam_screenPos : TEXCOORD1;
				half4 grabPos : TEXCOORD2;
				float3 worldPos : TEXCOORD3;
			};

			v2f vert (appdata v)
			{
				v2f o;

				// Goal of this vert shader is to place a sheet of triangles in front of the camera. The geometry has
				// two rows of verts, the top row and the bottom row (top and bottom are view relative). The bottom row
				// is pushed down below the bottom of the screen. Every vert in the top row can take any vertical position
				// on the near plane in order to find the hibiscus of the water. Due to render states, the ocean surface
				// will stomp over the results of this shader. The ocean surface has necessary code to render from underneath
				// and correctly fog etc.

				// Potential optimisations (note that this shader runs over a few dozen vertices, not over screen pixels!):
				// - pull the view vectors out of the cam matrix directly
				// - test lower FPI iteration count
				// - sample displacements without normal..
				// - when looking down through the water surface, the code currently pushes the top verts of the skirt
				//   up to cover the whole screen, but it only needs to get pushed up to the horizon level to meet the water surface
				// - the projection to the horizon could probably collapse down to a few LOC to compute the NDC y without a full projection

				// view coordinate frame for camera
				float3 right   = mul((float3x3)unity_CameraToWorld, float3(1., 0., 0.));
				float3 up      = mul((float3x3)unity_CameraToWorld, float3(0., 1., 0.));
				float3 forward = mul((float3x3)unity_CameraToWorld, float3(0., 0., 1.));

				float3 nearPlaneCenter = _WorldSpaceCameraPos + forward * _ProjectionParams.y * 1.001;
				// Spread verts across the near plane.
				// TODO replace 3. with a projection specific value
				o.worldPos = nearPlaneCenter
					+ 3. * right * v.vertex.x * _ProjectionParams.y
					+ up * v.vertex.z * _ProjectionParams.y;

				// Isolate topmost edge
				if (v.vertex.z > 0.45)
				{
					// Only compute intersection of water if viewer is looking "horizontal-ish". When the viewer starts to look
					// too much up or down, the intersection between the near plane and the water surface can be complex.
					if (abs(forward.y) < .8)
					{
						half2 nxz_dummy = (half2)0.;

						// Find intersection of the near plane and the water surface at this vert using FPI. See here for info about
						// FPI http://www.huwbowles.com/fpi-gdc-2016/
						float2 sampleXZ = o.worldPos.xz;
						float3 disp;
						for (int i = 0; i < 6; i++)
						{
							// Sample displacement textures, add results to current world pos / normal / foam
							disp = float3(sampleXZ.x, _OceanCenterPosWorld.y, sampleXZ.y);
							SampleDisplacements(_LD_Sampler_AnimatedWaves_0, LD_0_WorldToUV(sampleXZ), 1.0, _LD_Params_0.w, _LD_Params_0.x, disp, nxz_dummy);
							float3 nearestPointOnUp = o.worldPos + up * dot(disp - o.worldPos, up);
							float2 error = disp.xz - nearestPointOnUp.xz;
							sampleXZ -= error;
						}

						o.worldPos = disp;
					}
					else
					{
						// Push top edge up if we are looking down so that the screen defaults to looking underwater.
						// Push top edge down if we are looking up so that the screen defaults to looking out of water.
						o.worldPos -= sign(forward.y) * 2. * up;
					}

					// Move the geometry towards the horizon. As noted above, the skirt will be stomped by the ocean
					// surface render. If we project a bit towards the horizon to make a bit of overlap then we can reduce
					// the chance render issues from cracks/gaps with down angles, or of the skirt being too high for up angles.
					float3 horizonPoint = _WorldSpaceCameraPos + (o.worldPos - _WorldSpaceCameraPos) * 10000.;
					horizonPoint.y = _OceanCenterPosWorld.y;
					float3 horizonDir = normalize(horizonPoint - _WorldSpaceCameraPos);
					float3 projectionOfHorizonOnNearPlane = _WorldSpaceCameraPos + horizonDir / dot(horizonDir, forward);
					o.worldPos = lerp(o.worldPos, projectionOfHorizonOnNearPlane, 0.1);
					
					// Test - always put top row of verts at water horizon, because then it will always meet the water
					// surface. Good idea but didnt work because it then does underwater shading on opaque surfaces which
					// can be ABOVE the water surface. Not sure if theres any way around this.
					o.vertex = mul(UNITY_MATRIX_VP, float4(o.worldPos, 1.));
					o.vertex.z = o.vertex.w;
				}
				else
				{
					// Bottom row of verts - push them down below bottom of screen
					o.worldPos -= 8. * up;

					o.vertex = mul(UNITY_MATRIX_VP, float4(o.worldPos, 1.));
					o.vertex.z = o.vertex.w;
				}

				o.foam_screenPos.yzw = ComputeScreenPos(o.vertex).xyw;
				o.foam_screenPos.x = 0.;
				o.grabPos = ComputeGrabScreenPos(o.vertex);

				o.uv = v.uv;

				return o;
			}
			
			#include "OceanEmission.hlsl"
			uniform sampler2D _CameraDepthTexture;
			uniform sampler2D _Normals;

			half4 frag(v2f i) : SV_Target
			{
				const half3 view = normalize(_WorldSpaceCameraPos - i.worldPos);

				float pixelZ = LinearEyeDepth(i.vertex.z);
				half3 screenPos = i.foam_screenPos.yzw;
				half2 uvDepth = screenPos.xy / screenPos.z;
				const float sceneZ01 = tex2D(_CameraDepthTexture, uvDepth).x;
				const float sceneZ = LinearEyeDepth(sceneZ01);
				
				const float3 lightDir = _WorldSpaceLightPos0.xyz;
				const half shadow = 1.; // TODO ?
				const half3 n_pixel = 0.;
				const half3 bubbleCol = 0.;

				float3 surfaceAboveCamPosWorld = 0.; half2 nxz_dummy;
				SampleDisplacements(_LD_Sampler_AnimatedWaves_0, LD_0_WorldToUV(_WorldSpaceCameraPos.xz), 1.0, _LD_Params_0.w, _LD_Params_0.x, surfaceAboveCamPosWorld, nxz_dummy);
				surfaceAboveCamPosWorld.y += _OceanCenterPosWorld.y;

				half3 scatterCol = ScatterColour(surfaceAboveCamPosWorld, 0., _WorldSpaceCameraPos, lightDir, view, shadow, true, true);

				half3 sceneColour = tex2D(_BackgroundTexture, i.grabPos.xy / i.grabPos.w).rgb;

#if _CAUSTICS_ON
				if (sceneZ01 != 0.0)
				{
					ApplyCaustics(view, lightDir, sceneZ, _Normals, sceneColour);
				}
#endif // _CAUSTICS_ON

				half3 col = lerp(sceneColour, scatterCol, 1. - exp(-_DepthFogDensity.xyz * sceneZ));

				return half4(col, 1.);
			}
			ENDCG
		}
	}
}
