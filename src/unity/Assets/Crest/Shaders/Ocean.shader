// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Ocean"
{
	Properties
	{
		[Toggle] _ApplyNormalMapping("Apply Normal Mapping", Float) = 1
		[NoScaleOffset] _Normals ( "    Normals", 2D ) = "bump" {}
		_NormalsStrength("    Strength", Range(0.01, 2.0)) = 0.3
		_NormalsScale("    Scale", Range(0.01, 50.0)) = 1.0
		_Diffuse("Diffuse", Color) = (0.2, 0.05, 0.05, 1.0)
		[Toggle] _ComputeDirectionalLight("Add Directional Light", Float) = 1
		_DirectionalLightFallOff("    Fall-Off", Range(1.0, 4096.0)) = 128.0
		_DirectionalLightBoost("    Boost", Range(0.0, 512.0)) = 5.0
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
		[Toggle] _Foam("Foam", Float) = 1
		[NoScaleOffset] _FoamTexture ( "    Texture", 2D ) = "white" {}
		_FoamScale("    Scale", Range(0.01, 50.0)) = 10.0
		_FoamWhiteColor("    White Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_FoamBubbleColor("    Bubble Foam Color", Color) = (0.0, 0.0904, 0.105, 1.0)
		_ShorelineFoamMinDepth("    Shoreline Foam Min Depth", Range(0.01, 5.0)) = 0.27
		_WaveFoamFeather("    Wave Foam Feather", Range(0.001,1.0)) = 0.32
		_WaveFoamBubblesCoverage("    Wave Foam Bubbles Coverage", Range(0.0,5.0)) = 0.95
		[Toggle] _Foam3DLighting("Foam 3D Lighting", Float) = 1
		_WaveFoamLightScale("    Light Scale", Range(0.0, 2.0)) = 0.7
		_WaveFoamNormalStrength("    Normals Strength", Range(0.0, 30.0)) = 3.5
		_WaveFoamSpecularFallOff("    Specular Fall-Off", Range(1.0, 512.0)) = 275.0
		_WaveFoamSpecularBoost("    Specular Boost", Range(0.0, 16.0)) = 4.0
		[Toggle] _Transparency("Transparency", Float) = 1
		_DepthFogDensity("    Density", Vector) = (0.28, 0.16, 0.24, 1.0)
		[Toggle] _Caustics("Caustics", Float) = 1
		[NoScaleOffset] _CausticsTexture ( "    Caustics", 2D ) = "black" {}
		_CausticsTextureScale("    Scale", Range(0.0, 25.0)) = 5.0
		_CausticsTextureAverage("    Texture Average Value", Range(0.0, 1.0)) = 0.07
		_CausticsStrength("    Strength", Range(0.0, 10.0)) = 3.2
		_CausticsFocalDepth("    Focal Depth", Range(0.0, 25.0)) = 2.0
		_CausticsDepthOfField("    Depth Of Field", Range(0.01, 10.0)) = 0.33
		_CausticsDistortionScale("    Distortion Scale", Range(0.01, 50.0)) = 10.0
		_CausticsDistortionStrength("    Distortion Strength", Range(0.0, 0.25)) = 0.075
		_FresnelPower("Fresnel Power", Range(0.0, 20.0)) = 3.0
		[NoScaleOffset] _Skybox ("Skybox", CUBE) = "" {}
		[Toggle] _PlanarReflections("Planar Reflections", Float) = 1
		[Toggle] _ProceduralSky("Procedural Sky", Float) = 0
		[HDR] _SkyBase("    Base", Color) = (1.0, 1.0, 1.0, 1.0)
		[HDR] _SkyTowardsSun("    Towards Sun", Color) = (1.0, 1.0, 1.0, 1.0)
		_SkyDirectionality("    Directionality", Range(0.0, 0.99)) = 1.0
		[HDR] _SkyAwayFromSun("    Away From Sun", Color) = (1.0, 1.0, 1.0, 1.0)
		[Toggle] _Shadows("Shadows", Float) = 1
		_DiffuseShadow("    Diffuse Colour", Color) = (0.2, 0.05, 0.05, 1.0)
		_SubSurfaceShallowColShadow("    Shallow Colour", Color) = (0.42, 0.75, 0.69)
		[Toggle] _ApplyFlowToNormals("Apply Flow To Normals (Experimental)", Float) = 0
		[Enum(CullMode)] _CullMode("Cull Mode", Int) = 2
		[Toggle] _DebugDisableShapeTextures("Debug Disable Shape Textures", Float) = 0
		[Toggle] _DebugVisualiseShapeSample("Debug Visualise Shape Sample", Float) = 0
		[Toggle] _DebugVisualiseFlow("Debug Visualise Flow", Float) = 0
		[Toggle] _DebugDisableSmoothLOD("Debug Disable Smooth LOD", Float) = 0
		[Toggle] _CompileShaderWithDebugInfo("Compile Shader With Debug Info (D3D11)", Float) = 0
	}

	Category
	{
		Tags {}

		SubShader
		{
			// ForwardBase - tell unity we're going to render water in forward manner and we're going to do lighting and it will set the appropriate uniforms
			// Geometry+510 - unity treats anything after Geometry+500 as transparent, and will render it in a forward manner and copy out the gbuffer data
			//     and do post processing before running it. Discussion of this in issue #53.
			Tags { "LightMode"="ForwardBase" "Queue"="Geometry+510" "IgnoreProjector"="True" "RenderType"="Opaque" }

			GrabPass
			{
				"_BackgroundTexture"
			}

			Pass
			{
				// Culling user defined - can be inverted for under water
				Cull[_CullMode]

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile_fog

				#pragma shader_feature _APPLYNORMALMAPPING_ON
				#pragma shader_feature _COMPUTEDIRECTIONALLIGHT_ON
				#pragma shader_feature _SUBSURFACESCATTERING_ON
				#pragma shader_feature _SUBSURFACEHEIGHTLERP_ON
				#pragma shader_feature _SUBSURFACESHALLOWCOLOUR_ON
				#pragma shader_feature _TRANSPARENCY_ON
				#pragma shader_feature _CAUSTICS_ON
				#pragma shader_feature _FOAM_ON
				#pragma shader_feature _FOAM3DLIGHTING_ON
				#pragma shader_feature _PLANARREFLECTIONS_ON
				#pragma shader_feature _PROCEDURALSKY_ON
				#pragma shader_feature _APPLYFLOWTONORMALS_ON
				#pragma shader_feature _SHADOWS_ON

				#pragma shader_feature _DEBUGDISABLESHAPETEXTURES_ON
				#pragma shader_feature _DEBUGVISUALISESHAPESAMPLE_ON
				#pragma shader_feature _DEBUGVISUALISEFLOW_ON
				#pragma shader_feature _DEBUGDISABLESMOOTHLOD_ON
				#pragma shader_feature _COMPILESHADERWITHDEBUGINFO_ON

				#if _COMPILESHADERWITHDEBUGINFO_ON
				#pragma enable_d3d11_debug_symbols
				#endif

				#include "UnityCG.cginc"
				#include "Lighting.cginc"

				struct appdata_t
				{
					float4 vertex : POSITION;
					float2 texcoord: TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					#if _APPLYFLOWTONORMALS_ON
					half2 flow : TEXCOORD2;
					#endif
					half4 n_shadow : TEXCOORD1;
					half4 foam_screenPos : TEXCOORD4;
					half4 lodAlpha_worldXZUndisplaced_oceanDepth : TEXCOORD5;
					float3 worldPos : TEXCOORD7;
					#if _DEBUGVISUALISESHAPESAMPLE_ON
					half3 debugtint : TEXCOORD8;
					#endif
					half4 grabPos : TEXCOORD9;

					UNITY_FOG_COORDS( 3 )
				};

				#include "OceanLODData.hlsl"

				// INSTANCE PARAMS

				// MeshScaleLerp, FarNormalsWeight, LODIndex (debug), unused
				uniform float4 _InstanceData;

				v2f vert( appdata_t v )
				{
					v2f o;

					// move to world
					o.worldPos = mul(unity_ObjectToWorld, v.vertex);

					// vertex snapping and lod transition
					float lodAlpha;
					SnapAndTransitionVertLayout(_InstanceData.x, o.worldPos, lodAlpha);
					o.lodAlpha_worldXZUndisplaced_oceanDepth.x = lodAlpha;
					o.lodAlpha_worldXZUndisplaced_oceanDepth.yz = o.worldPos.xz;

					// sample shape textures - always lerp between 2 LOD scales, so sample two textures
					o.n_shadow = half4(0., 0., 0., 0.);
					o.foam_screenPos.x = 0.;

					#if _APPLYFLOWTONORMALS_ON
					o.flow = half2(0., 0.);
					#endif

					o.lodAlpha_worldXZUndisplaced_oceanDepth.w = 0.;

					// sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
					float wt_0 = (1. - lodAlpha) * _LD_Params_0.z;
					float wt_1 = (1. - wt_0) * _LD_Params_1.z;
					// sample displacement textures, add results to current world pos / normal / foam
					const float2 worldXZBefore = o.worldPos.xz;
					if (wt_0 > 0.001)
					{
						const float2 uv_0 = LD_0_WorldToUV(worldXZBefore);

						#if !_DEBUGDISABLESHAPETEXTURES_ON
						SampleDisplacements(_LD_Sampler_AnimatedWaves_0, uv_0, wt_0, _LD_Params_0.w, _LD_Params_0.x, o.worldPos, o.n_shadow.xy);
						#endif

						#if _FOAM_ON
						SampleFoam(_LD_Sampler_Foam_0, uv_0, wt_0, o.foam_screenPos.x);
						#endif

						#if _APPLYFLOWTONORMALS_ON
						SampleFlow(_LD_Sampler_Flow_0, uv_0, wt_0, o.flow);
						#endif

						#if _SUBSURFACESHALLOWCOLOUR_ON
						SampleSeaFloorHeightAboveBaseline(_LD_Sampler_SeaFloorDepth_0, uv_0, wt_0, o.lodAlpha_worldXZUndisplaced_oceanDepth.w);
						#endif

						#if _SHADOWS_ON
						SampleShadow(_LD_Sampler_Shadow_0, uv_0, wt_0, o.n_shadow.zw);
						#endif
					}
					if (wt_1 > 0.001)
					{
						const float2 uv_1 = LD_1_WorldToUV(worldXZBefore);

						#if !_DEBUGDISABLESHAPETEXTURES_ON
						SampleDisplacements(_LD_Sampler_AnimatedWaves_1, uv_1, wt_1, _LD_Params_1.w, _LD_Params_1.x, o.worldPos, o.n_shadow.xy);
						#endif

						#if _FOAM_ON
						SampleFoam(_LD_Sampler_Foam_1, uv_1, wt_1, o.foam_screenPos.x);
						#endif

						#if _APPLYFLOWTONORMALS_ON
						SampleFlow(_LD_Sampler_Flow_1, uv_1, wt_1, o.flow);
						#endif

						#if _SUBSURFACESHALLOWCOLOUR_ON
						SampleSeaFloorHeightAboveBaseline(_LD_Sampler_SeaFloorDepth_1, uv_1, wt_1, o.lodAlpha_worldXZUndisplaced_oceanDepth.w);
						#endif

						#if _SHADOWS_ON
						SampleShadow(_LD_Sampler_Shadow_1, uv_1, wt_1, o.n_shadow.zw);
						#endif
					}

					// convert height above -1000m to depth below surface
					o.lodAlpha_worldXZUndisplaced_oceanDepth.w = DEPTH_BASELINE - o.lodAlpha_worldXZUndisplaced_oceanDepth.w;

					// foam can saturate
					o.foam_screenPos.x = saturate(o.foam_screenPos.x);

					// debug tinting to see which shape textures are used
					#if _DEBUGVISUALISESHAPESAMPLE_ON
					#define TINT_COUNT (uint)7
					half3 tintCols[TINT_COUNT]; tintCols[0] = half3(1., 0., 0.); tintCols[1] = half3(1., 1., 0.); tintCols[2] = half3(1., 0., 1.); tintCols[3] = half3(0., 1., 1.); tintCols[4] = half3(0., 0., 1.); tintCols[5] = half3(1., 0., 1.); tintCols[6] = half3(.5, .5, 1.);
					o.debugtint = wt_0 * tintCols[_LD_LodIdx_0 % TINT_COUNT] + wt_1 * tintCols[_LD_LodIdx_1 % TINT_COUNT];
					#endif

					// view-projection
					o.vertex = mul(UNITY_MATRIX_VP, float4(o.worldPos, 1.));

					UNITY_TRANSFER_FOG(o, o.vertex);

					// unfortunate hoop jumping - this is inputs for refraction. depending on whether HDR is on or off, the grabbed scene
					// colours may or may not come from the backbuffer, which means they may or may not be flipped in y. use these macros
					// to get the right results, every time.
					o.grabPos = ComputeGrabScreenPos(o.vertex);
					o.foam_screenPos.yzw = ComputeScreenPos(o.vertex).xyw;
					return o;
				}

				// frag shader uniforms

				#include "OceanFoam.hlsl"
				#include "OceanEmission.hlsl"
				#include "OceanReflection.hlsl"
				uniform sampler2D _Normals;
				#include "OceanNormalMapping.hlsl"

				uniform sampler2D _CameraDepthTexture;

				float3 WorldSpaceLightDir(float3 worldPos)
				{
					float3 lightDir = _WorldSpaceLightPos0.xyz;
					if (_WorldSpaceLightPos0.w > 0.)
					{
						// non-directional light - this is a position, not a direction
						lightDir = normalize(lightDir - worldPos.xyz);
					}
					return lightDir;
				}

				half4 frag(v2f i) : SV_Target
				{
					half3 view = normalize(_WorldSpaceCameraPos - i.worldPos);

					// water surface depth, and underlying scene opaque surface depth
					float pixelZ = LinearEyeDepth(i.vertex.z);
					half3 screenPos = i.foam_screenPos.yzw;
					half2 uvDepth = screenPos.xy / screenPos.z;
					float sceneZ01 = tex2D(_CameraDepthTexture, uvDepth).x;
					float sceneZ = LinearEyeDepth(sceneZ01);

					float3 lightDir = WorldSpaceLightDir(i.worldPos);
					// Soft shadow, hard shadow
					fixed2 shadow = (fixed2)1.0
					#if _SHADOWS_ON
						- i.n_shadow.zw
					#endif
						;

					// Normal - geom + normal mapping
					half3 n_geom = normalize(half3(i.n_shadow.x, 1., i.n_shadow.y));
					half3 n_pixel = n_geom;
					#if _APPLYNORMALMAPPING_ON
					#if _APPLYFLOWTONORMALS_ON
					ApplyNormalMapsWithFlow(i.lodAlpha_worldXZUndisplaced_oceanDepth.yz, i.flow, i.lodAlpha_worldXZUndisplaced_oceanDepth.x, n_pixel);
					#else
					n_pixel.xz += SampleNormalMaps(i.lodAlpha_worldXZUndisplaced_oceanDepth.yz, i.lodAlpha_worldXZUndisplaced_oceanDepth.x);
					n_pixel = normalize(n_pixel);
					#endif
					#endif

					// Foam - underwater bubbles and whitefoam
					half3 bubbleCol = (half3)0.;
					#if _FOAM_ON
					half4 whiteFoamCol;
					#if !_APPLYFLOWTONORMALS_ON
					ComputeFoam(i.foam_screenPos.x, i.lodAlpha_worldXZUndisplaced_oceanDepth.yz, i.worldPos.xz, n_pixel, pixelZ, sceneZ, view, lightDir, shadow.y, bubbleCol, whiteFoamCol);
					#else
					ComputeFoamWithFlow(i.flow, i.foam_screenPos.x, i.lodAlpha_worldXZUndisplaced_oceanDepth.yz, i.worldPos.xz, n_pixel, pixelZ, sceneZ, view, lightDir, shadow.y, bubbleCol, whiteFoamCol);
					#endif // _APPLYFLOWTONORMALS_ON
					#endif // _FOAM_ON

					// Compute color of ocean - in-scattered light + refracted scene
					half3 col = OceanEmission(i.worldPos, i.lodAlpha_worldXZUndisplaced_oceanDepth.w, view, n_pixel, n_geom, lightDir, shadow.x, i.grabPos, screenPos, pixelZ, uvDepth, sceneZ, sceneZ01, bubbleCol, _Normals, _CameraDepthTexture);

					// Light that reflects off water surface
					ApplyReflection(view, n_pixel, lightDir, shadow.y, i.foam_screenPos.yzzw, col);

					// Override final result with white foam - bubbles on surface
					#if _FOAM_ON
					col = lerp(col, whiteFoamCol.rgb, whiteFoamCol.a);
					#endif

					// Fog
					UNITY_APPLY_FOG(i.fogCoord, col);

					#if _DEBUGVISUALISESHAPESAMPLE_ON
					col = lerp(col.rgb, i.debugtint, 0.5);
					#endif
					#if _DEBUGVISUALISEFLOW_ON
					#if _APPLYFLOWTONORMALS_ON
					col.rg = lerp(col.rg, i.flow.xy, 0.5);
					#endif
					#endif

					return half4(col, 1.);
				}

				ENDCG
			}
		}
	}
}
