// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Ocean"
{
	Properties
	{
		[Header(Normal Mapping)]
		[Toggle] _ApplyNormalMapping("Enable", Float) = 1
		[NoScaleOffset] _Normals("Normal Map", 2D) = "bump" {}
		_NormalsStrength("Strength", Range(0.01, 2.0)) = 0.3
		_NormalsScale("Scale", Range(0.01, 50.0)) = 1.0

		// Base light scattering settings which give water colour
		[Header(Scattering)]
		// Base colour
		_Diffuse("Diffuse", Color) = (0.2, 0.05, 0.05, 1.0)
		// Changes colour in shadow. Requires 'Create Shadow Data' enabled on OceanRenderer script.
		[Toggle] _Shadows("Shadowing", Float) = 0
		// Base colour in shadow
		_DiffuseShadow("Diffuse (Shadow)", Color) = (0.2, 0.05, 0.05, 1.0)

		// Light scattering contribution from primary light
		[Header(Directional Scattering)]
		[Toggle] _SubSurfaceScattering("Enable", Float) = 1
		// Colour tint for primary light contribution
		_SubSurfaceColour("Colour", Color) = (0.0, 0.48, 0.36)
		// Amount of primary light contribution that always comes in
		_SubSurfaceBase("Base Mul", Range(0.0, 2.0)) = 0.6
		// Primary light contribution in direction of light to emulate light passing through waves
		_SubSurfaceSun("Sun Mul", Range(0.0, 10.0)) = 0.8
		// Fall-off for primary light scattering to affect directionality
		_SubSurfaceSunFallOff("Sun Fall-Off", Range(1.0, 16.0)) = 4.0

		// Light scattering at wave peaks
		[Header(Height Based Scattering)]
		[Toggle] _SubSurfaceHeightLerp("Enable", Float) = 1
		// Height from sea level where scattering is at maximum
		_SubSurfaceHeightMax("Height Max", Range(0.0, 100.0)) = 3.0
		// Fall off of height scattering
		_SubSurfaceHeightPower("Height Power", Range(0.01, 10.0)) = 1.0
		// Tint for height scattering
		_SubSurfaceCrestColour("Crest Colour", Color) = (0.42, 0.69, 0.52)

		// Light scattering in shallow water
		[Header(Shallow Scattering)]
		[Toggle] _SubSurfaceShallowColour("Enable", Float) = 1
		// Max depth that is considered 'shallow'
		_SubSurfaceDepthMax("Depth Max", Range(0.01, 50.0)) = 3.0
		// Fall off of shallow scattering
		_SubSurfaceDepthPower("Depth Power", Range(0.01, 10.0)) = 1.0
		// Colour in shallow water
		_SubSurfaceShallowCol("Shallow Colour", Color) = (0.42, 0.75, 0.69)
		// Shallow water colour in shadow (see comment on Shadowing param above)
		_SubSurfaceShallowColShadow("Shallow Colour (Shadow)", Color) = (0.42, 0.75, 0.69)

		// Reflection properites
		[Header(Reflection Environment)]
		// Controls specular response of water surface
		_Specular("Specular", Range(0.0, 1.0)) = 1.0
		// Controls harshness of Fresnel behaviour
		_FresnelPower("Fresnel Power", Range(1.0, 20.0)) = 5.0
		// Refractive indices
		_RefractiveIndexOfAir("Refractive Index of Air", Range(1.0, 2.0)) = 1.0
		_RefractiveIndexOfWater("Refractive Index of Water", Range(1.0, 2.0)) = 1.333
		// Dynamically rendered 'reflection plane' style reflections. Requires OceanPlanarReflection script added to main camera.
		[Toggle] _PlanarReflections("Planar Reflections", Float) = 0
		// How much the water normal affects the planar reflection
		_PlanarReflectionNormalsStrength("Planar Reflections Distortion", Float) = 1
		// Whether to use an overridden reflection cubemap (provided in the next property)
		[Toggle] _OverrideReflectionCubemap("Override Reflection Cubemap", Float) = 0
		// Custom environment map to reflect
		[NoScaleOffset] _ReflectionCubemapOverride("Override Reflection Cubemap", CUBE) = "" {}

		// A simple procedural skybox, not suitable for rendering on screen, but can be useful to give control over reflection colour
		// especially in stylized/non realistic applications
		[Header(Procedural Skybox)]
		[Toggle] _ProceduralSky("Enable", Float) = 0
		// Base sky colour
		[HDR] _SkyBase("Base", Color) = (1.0, 1.0, 1.0, 1.0)
		// Colour in sun direction
		[HDR] _SkyTowardsSun("Towards Sun", Color) = (1.0, 1.0, 1.0, 1.0)
		// Direction fall off
		_SkyDirectionality("Directionality", Range(0.0, 0.99)) = 1.0
		// Colour away from sun direction
		[HDR] _SkyAwayFromSun("Away From Sun", Color) = (1.0, 1.0, 1.0, 1.0)

		[Header(Add Directional Light)]
		[Toggle] _ComputeDirectionalLight("Enable", Float) = 1
		_DirectionalLightFallOff("Fall-Off", Range(1.0, 4096.0)) = 128.0
		_DirectionalLightBoost("Boost", Range(0.0, 512.0)) = 5.0

		[Header(Foam)]
		[Toggle] _Foam("Enable", Float) = 1
		[NoScaleOffset] _FoamTexture("Texture", 2D) = "white" {}
		_FoamScale("Scale", Range(0.01, 50.0)) = 10.0
		// Colour tint for whitecaps / foam on water surface
		_FoamWhiteColor("White Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
		// Colour tint bubble foam underneath water surface
		_FoamBubbleColor("Bubble Foam Color", Color) = (0.64, 0.83, 0.82, 1.0)
		// Parallax for underwater bubbles to give feeling of volume
		_FoamBubbleParallax("Bubble Foam Parallax", Range(0.0, 0.5)) = 0.05
		// Proximity to sea floor where foam starts to get generated
		_ShorelineFoamMinDepth("Shoreline Foam Min Depth", Range(0.01, 5.0)) = 0.27
		// Controls how gradual the transition is from full foam to no foam
		_WaveFoamFeather("Wave Foam Feather", Range(0.001, 1.0)) = 0.32
		// How much underwater bubble foam is generated
		_WaveFoamBubblesCoverage("Wave Foam Bubbles Coverage", Range(0.0, 5.0)) = 0.95

		// Generates normals for the foam based on foam values/texture and use it for foam lighting
		[Header(Foam 3D Lighting)]
		[Toggle] _Foam3DLighting("Enable", Float) = 1
		_WaveFoamLightScale("Light Scale", Range(0.0, 2.0)) = 0.7
		// Strength of the generated normals
		_WaveFoamNormalStrength("Normals Strength", Range(0.0, 30.0)) = 3.5
		// Acts like a gloss parameter for specular response
		_WaveFoamSpecularFallOff("Specular Fall-Off", Range(1.0, 512.0)) = 275.0
		// Strength of specular response
		_WaveFoamSpecularBoost("Specular Boost", Range(0.0, 16.0)) = 4.0

		[Header(Transparency)]
		// Whether light can pass through the water surface
		[Toggle] _Transparency("Enable", Float) = 1
		// Scattering coefficient within water volume, per channel
		_DepthFogDensity("Fog Density", Vector) = (0.28, 0.16, 0.24, 1.0)
		// How strongly light is refracted when passing through water surface
		_RefractionStrength("Refraction Strength", Range(0.0, 2.0)) = 0.1

		// Appoximate rays being focused/defocused on geometry under water
		[Header(Caustics)]
		[Toggle] _Caustics("Enable", Float) = 1
		[NoScaleOffset] _CausticsTexture("Caustics", 2D) = "black" {}
		_CausticsTextureScale("Scale", Range(0.0, 25.0)) = 5.0
		// The 'mid' value of the caustics texture, around which the caustic texture values are scaled
		_CausticsTextureAverage("Texture Average Value", Range(0.0, 1.0)) = 0.07
		// Scaling / intensity
		_CausticsStrength("Strength", Range(0.0, 10.0)) = 3.2
		// The depth at which the caustics are in focus
		_CausticsFocalDepth("Focal Depth", Range(0.0, 25.0)) = 2.0
		// The range of depths over which the caustics are in focus
		_CausticsDepthOfField("Depth Of Field", Range(0.01, 10.0)) = 0.33
		// How much the caustics texture is distorted
		_CausticsDistortionStrength("Distortion Strength", Range(0.0, 0.25)) = 0.075
		// The scale of the distortion pattern used to distort the caustics
		_CausticsDistortionScale("Distortion Scale", Range(0.01, 50.0)) = 10.0

		// To use the underwater effect the UnderWaterCurtainGeom and UnderWaterMeniscus prefabs must be parented to the camera.
		[Header(Underwater)]
		// Whether the underwater effect is being used. This enables code that shades the surface correctly from underneath.
		[Toggle] _Underwater("Enable", Float) = 0
		// Ordinarily set this to Back to cull back faces, but set to Off to make sure both sides of the surface draw if the
		// underwater effect is being used.
		[Enum(CullMode)] _CullMode("Cull Mode", Int) = 2

		// Flow is horizontal motion in water as demonstrated in the 'whirlpool' example scene. 'Create Flow Sim' must be
		// enabled on the OceanRenderer to generate flow data.
		[Header(Flow)]
		[Toggle] _Flow("Enable", Float) = 0

		[Header(Debug Options)]
		// Build shader with debug info which allows stepping through the code in a GPU debugger. I typically use RenderDoc or
		// PIX for Windows (requires DX12 API to be selected).
		[Toggle] _CompileShaderWithDebugInfo("Compile Shader With Debug Info (D3D11)", Float) = 0
		[Toggle] _DebugDisableShapeTextures("Debug Disable Shape Textures", Float) = 0
		[Toggle] _DebugVisualiseShapeSample("Debug Visualise Shape Sample", Float) = 0
		[Toggle] _DebugVisualiseFlow("Debug Visualise Flow", Float) = 0
		[Toggle] _DebugDisableSmoothLOD("Debug Disable Smooth LOD", Float) = 0
	}

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
			Cull [_CullMode]

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			// for VFACE
			#pragma target 3.0
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
			#pragma shader_feature _OVERRIDEREFLECTIONCUBEMAP_ON

			#pragma shader_feature _PROCEDURALSKY_ON
			#pragma shader_feature _UNDERWATER_ON
			#pragma shader_feature _FLOW_ON
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

			struct Attributes
			{
				// The old unity macros require this name and type.
				float4 vertex : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				half4 flow_shadow : TEXCOORD1;
				half4 foam_screenPos : TEXCOORD4;
				half4 lodAlpha_worldXZUndisplaced_oceanDepth : TEXCOORD5;
				float3 worldPos : TEXCOORD7;
				#if _DEBUGVISUALISESHAPESAMPLE_ON
				half3 debugtint : TEXCOORD8;
				#endif
				half4 grabPos : TEXCOORD9;

				UNITY_FOG_COORDS(3)
			};

			#include "OceanHelpers.hlsl"

			uniform float _CrestTime;

			// MeshScaleLerp, FarNormalsWeight, LODIndex (debug), unused
			uniform float4 _InstanceData;

			// Argument name is v because some macros like COMPUTE_EYEDEPTH require it.
			Varyings Vert(Attributes v)
			{
				Varyings o;

				// Move to world space
				o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));

				// Vertex snapping and lod transition
				float lodAlpha;
				SnapAndTransitionVertLayout(_InstanceData.x, o.worldPos, lodAlpha);
				o.lodAlpha_worldXZUndisplaced_oceanDepth.x = lodAlpha;
				o.lodAlpha_worldXZUndisplaced_oceanDepth.yz = o.worldPos.xz;

				// sample shape textures - always lerp between 2 LOD scales, so sample two textures
				o.flow_shadow = half4(0., 0., 0., 0.);
				o.foam_screenPos.x = 0.;

				o.lodAlpha_worldXZUndisplaced_oceanDepth.w = CREST_OCEAN_DEPTH_BASELINE;
				// Sample shape textures - always lerp between 2 LOD scales, so sample two textures

				// Calculate sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
				const float wt_smallerLod = (1. - lodAlpha) * _LD_Params[_LD_SliceIndex].z;
				const float wt_biggerLod = (1. - wt_smallerLod) * _LD_Params[_LD_SliceIndex + 1].z;
				// Sample displacement textures, add results to current world pos / normal / foam
				const float2 positionWS_XZ_before = o.worldPos.xz;

				// Data that needs to be sampled at the undisplaced position
				if (wt_smallerLod > 0.001)
				{
					const float3 uv_slice_smallerLod = WorldToUV(positionWS_XZ_before);

					#if !_DEBUGDISABLESHAPETEXTURES_ON
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice_smallerLod, wt_smallerLod, o.worldPos);
					#endif

					#if _FOAM_ON
					SampleFoam(_LD_TexArray_Foam, uv_slice_smallerLod, wt_smallerLod, o.foam_screenPos.x);
					#endif

					#if _FLOW_ON
					SampleFlow(_LD_TexArray_Flow, uv_slice_smallerLod, wt_smallerLod, o.flow_shadow.xy);
					#endif
				}
				if (wt_biggerLod > 0.001)
				{
					const float3 uv_slice_biggerLod = WorldToUV_BiggerLod(positionWS_XZ_before);

					#if !_DEBUGDISABLESHAPETEXTURES_ON
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice_biggerLod, wt_biggerLod, o.worldPos);
					#endif

					#if _FOAM_ON
					SampleFoam(_LD_TexArray_Foam, uv_slice_biggerLod, wt_biggerLod, o.foam_screenPos.x);
					#endif

					#if _FLOW_ON
					SampleFlow(_LD_TexArray_Flow, uv_slice_biggerLod, wt_biggerLod, o.flow_shadow.xy);
					#endif
				}

				// Data that needs to be sampled at the displaced position
				if (wt_smallerLod > 0.001)
				{
					const float3 uv_slice_smallerLodDisp = WorldToUV(o.worldPos.xz);

					#if _SUBSURFACESHALLOWCOLOUR_ON
					SampleSeaDepth(_LD_TexArray_SeaFloorDepth, uv_slice_smallerLodDisp, wt_smallerLod, o.lodAlpha_worldXZUndisplaced_oceanDepth.w);
					#endif

					#if _SHADOWS_ON
					SampleShadow(_LD_TexArray_Shadow, uv_slice_smallerLodDisp, wt_smallerLod, o.flow_shadow.zw);
					#endif
				}
				if (wt_biggerLod > 0.001)
				{
					const float3 uv_slice_biggerLodDisp = WorldToUV_BiggerLod(o.worldPos.xz);

					#if _SUBSURFACESHALLOWCOLOUR_ON
					SampleSeaDepth(_LD_TexArray_SeaFloorDepth, uv_slice_biggerLodDisp, wt_biggerLod, o.lodAlpha_worldXZUndisplaced_oceanDepth.w);
					#endif

					#if _SHADOWS_ON
					SampleShadow(_LD_TexArray_Shadow, uv_slice_biggerLodDisp, wt_biggerLod, o.flow_shadow.zw);
					#endif
				}

				// Foam can saturate
				o.foam_screenPos.x = saturate(o.foam_screenPos.x);

				// debug tinting to see which shape textures are used
				#if _DEBUGVISUALISESHAPESAMPLE_ON
				#define TINT_COUNT (uint)7
				half3 tintCols[TINT_COUNT]; tintCols[0] = half3(1., 0., 0.); tintCols[1] = half3(1., 1., 0.); tintCols[2] = half3(1., 0., 1.); tintCols[3] = half3(0., 1., 1.); tintCols[4] = half3(0., 0., 1.); tintCols[5] = half3(1., 0., 1.); tintCols[6] = half3(.5, .5, 1.);
				o.debugtint = wt_smallerLod * tintCols[_LD_LodIdx_0 % TINT_COUNT] + wt_biggerLod * tintCols[_LD_LodIdx_1 % TINT_COUNT];
				#endif

				// view-projection
				o.positionCS = mul(UNITY_MATRIX_VP, float4(o.worldPos, 1.));

				UNITY_TRANSFER_FOG(o, o.positionCS);

				// unfortunate hoop jumping - this is inputs for refraction. depending on whether HDR is on or off, the grabbed scene
				// colours may or may not come from the backbuffer, which means they may or may not be flipped in y. use these macros
				// to get the right results, every time.
				o.grabPos = ComputeGrabScreenPos(o.positionCS);
				o.foam_screenPos.yzw = ComputeScreenPos(o.positionCS).xyw;
				return o;
			}

			// frag shader uniforms

			#include "OceanFoam.hlsl"
			#include "OceanEmission.hlsl"
			#include "OceanReflection.hlsl"
			uniform sampler2D _Normals;
			#include "OceanNormalMapping.hlsl"

			uniform sampler2D _CameraDepthTexture;

			// Hack - due to SV_IsFrontFace occasionally coming through as true for backfaces,
			// add a param here that forces ocean to be in undrwater state. I think the root
			// cause here might be imprecision or numerical issues at ocean tile boundaries, although
			// i'm not sure why cracks are not visible in this case.
			uniform float _ForceUnderwater;

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

			bool IsUnderwater(const float facing)
			{
#if !_UNDERWATER_ON
				return false;
#endif
				const bool backface = facing < 0.0;
				return backface || _ForceUnderwater > 0.0;
			}

			half4 Frag(const Varyings input, const float facing : VFACE) : SV_Target
			{
				const bool underwater = IsUnderwater(facing);

				half3 view = normalize(_WorldSpaceCameraPos - input.worldPos);

				// water surface depth, and underlying scene opaque surface depth
				float pixelZ = LinearEyeDepth(input.positionCS.z);
				half3 screenPos = input.foam_screenPos.yzw;
				half2 uvDepth = screenPos.xy / screenPos.z;
				float sceneZ01 = tex2D(_CameraDepthTexture, uvDepth).x;
				float sceneZ = LinearEyeDepth(sceneZ01);

				float3 lightDir = WorldSpaceLightDir(input.worldPos);
				// Soft shadow, hard shadow
				fixed2 shadow = (fixed2)1.0
				#if _SHADOWS_ON
					- input.flow_shadow.zw
				#endif
					;

				// Normal - geom + normal mapping
				half3 n_geom = half3(0.0, 1.0, 0.0);
				const float lodAlpha = input.lodAlpha_worldXZUndisplaced_oceanDepth.x;

				//if(false)
				{
					const float3 uv_slice_smallerLod = WorldToUV(input.lodAlpha_worldXZUndisplaced_oceanDepth.yz);
					const float3 uv_slice_biggerLod = WorldToUV_BiggerLod(input.lodAlpha_worldXZUndisplaced_oceanDepth.yz);
					const float wt_smallerLod = (1. - lodAlpha) * _LD_Params[_LD_SliceIndex].z;
					const float wt_biggerLod = (1. - wt_smallerLod) * _LD_Params[_LD_SliceIndex + 1].z;
					float3 dummy = 0.;
					if (wt_smallerLod > 0.001) SampleDisplacementsNormals(_LD_TexArray_AnimatedWaves, uv_slice_smallerLod, wt_smallerLod, _LD_Params[_LD_SliceIndex].w, _LD_Params[_LD_SliceIndex].x, dummy, n_geom.xz);
					if (wt_biggerLod > 0.001) SampleDisplacementsNormals(_LD_TexArray_AnimatedWaves, uv_slice_biggerLod, wt_biggerLod, _LD_Params[_LD_SliceIndex + 1].w, _LD_Params[_LD_SliceIndex + 1].x, dummy, n_geom.xz);
					n_geom = normalize(n_geom);
				}

				if (underwater) n_geom = -n_geom;
				half3 n_pixel = n_geom;
				#if _APPLYNORMALMAPPING_ON
				#if _FLOW_ON
				ApplyNormalMapsWithFlow(input.lodAlpha_worldXZUndisplaced_oceanDepth.yz, input.flow_shadow.xy, input.lodAlpha_worldXZUndisplaced_oceanDepth.x, n_pixel);
				#else
				n_pixel.xz += (underwater ? -1. : 1.) * SampleNormalMaps(input.lodAlpha_worldXZUndisplaced_oceanDepth.yz, input.lodAlpha_worldXZUndisplaced_oceanDepth.x);
				n_pixel = normalize(n_pixel);
				#endif
				#endif

				// Foam - underwater bubbles and whitefoam
				half3 bubbleCol = (half3)0.;
				#if _FOAM_ON
				half4 whiteFoamCol;
				#if !_FLOW_ON
				ComputeFoam(input.foam_screenPos.x, input.lodAlpha_worldXZUndisplaced_oceanDepth.yz, input.worldPos.xz, n_pixel, pixelZ, sceneZ, view, lightDir, shadow.y, lodAlpha, bubbleCol, whiteFoamCol);
				#else
				ComputeFoamWithFlow(input.flow_shadow.xy, input.foam_screenPos.x, input.lodAlpha_worldXZUndisplaced_oceanDepth.yz, input.worldPos.xz, n_pixel, pixelZ, sceneZ, view, lightDir, shadow.y, lodAlpha, bubbleCol, whiteFoamCol);
				#endif // _FLOW_ON
				#endif // _FOAM_ON

				// Compute color of ocean - in-scattered light + refracted scene
				half3 scatterCol = ScatterColour(input.worldPos, input.lodAlpha_worldXZUndisplaced_oceanDepth.w, _WorldSpaceCameraPos, lightDir, view, shadow.x, underwater, true);

				half3 col = OceanEmission(view, n_pixel, lightDir, input.grabPos, pixelZ, uvDepth, sceneZ, sceneZ01, bubbleCol, _Normals, _CameraDepthTexture, underwater, scatterCol);

				// Light that reflects off water surface
				#if _UNDERWATER_ON
				if (underwater)
				{
					ApplyReflectionUnderwater(view, n_pixel, lightDir, shadow.y, input.foam_screenPos.yzzw, scatterCol, col);
				}
				else
				#endif
				{
					ApplyReflectionSky(view, n_pixel, lightDir, shadow.y, input.foam_screenPos.yzzw, col);
				}

				// Override final result with white foam - bubbles on surface
				#if _FOAM_ON
				col = lerp(col, whiteFoamCol.rgb, whiteFoamCol.a);
				#endif

				// Fog
				if (!underwater)
				{
					// Above water - do atmospheric fog. If you are using a third party sky package such as Azure, replace this with their stuff!
					UNITY_APPLY_FOG(input.fogCoord, col);
				}
				else
				{
					// underwater - do depth fog
					col = lerp(col, scatterCol, 1. - exp(-_DepthFogDensity.xyz * pixelZ));
				}

				#if _DEBUGVISUALISESHAPESAMPLE_ON
				col = lerp(col.rgb, input.debugtint, 0.5);
				#endif
				#if _DEBUGVISUALISEFLOW_ON
				#if _FLOW_ON
				col.rg = lerp(col.rg, input.flow_shadow.xy, 0.5);
				#endif
				#endif

				return half4(col, 1.);
			}

			ENDCG
		}
	}
}
