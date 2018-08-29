// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Ocean"
{
	Properties
	{
		[Toggle] _ApplyNormalMapping("Apply Normal Mapping", Float) = 1
		[NoScaleOffset] _Normals ( "    Normals", 2D ) = "bump" {}
		_NormalsStrength("    Strength", Range(0.01, 2.0)) = 0.3
		_NormalsScale("    Scale", Range(0.01, 50.0)) = 1.0
		[NoScaleOffset] _Skybox ("Skybox", CUBE) = "" {}
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
		[Enum(CullMode)] _CullMode("Cull Mode", Int) = 2
		[Toggle] _DebugDisableShapeTextures("Debug Disable Shape Textures", Float) = 0
		[Toggle] _DebugVisualiseShapeSample("Debug Visualise Shape Sample", Float) = 0
		[Toggle] _DebugVisualiseFlow("Debug Visualise Flow", Float) = 0
		[Toggle] _DebugDisableSmoothLOD("Debug Disable Smooth LOD", Float) = 0
		[Toggle] _CompileShaderWithDebugInfo("Compile Shader With Debug Info (D3D11)", Float) = 0
		[Toggle] _ApplyFlowToNormals("Apply Flow To Normals (Experimental)", Float) = 0
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
				#pragma shader_feature _DEBUGDISABLESHAPETEXTURES_ON
				#pragma shader_feature _DEBUGVISUALISESHAPESAMPLE_ON
				#pragma shader_feature _DEBUGVISUALISEFLOW_ON
				#pragma shader_feature _DEBUGDISABLESMOOTHLOD_ON
				#pragma shader_feature _COMPILESHADERWITHDEBUGINFO_ON
				#pragma shader_feature _APPLYFLOWTONORMALS_ON

				#if _COMPILESHADERWITHDEBUGINFO_ON
				#pragma enable_d3d11_debug_symbols
				#endif

				#include "UnityCG.cginc"

				struct appdata_t
				{
					float4 vertex : POSITION;
					float2 texcoord: TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					half3 n : TEXCOORD1;
					#if _APPLYFLOWTONORMALS_ON
					half2 flow : TEXCOORD2;
					#endif
					half4 foam_screenPos : TEXCOORD4;
					half4 lodAlpha_worldXZUndisplaced_oceanDepth : TEXCOORD5;
					float3 worldPos : TEXCOORD7;
					#if _DEBUGVISUALISESHAPESAMPLE_ON
					half3 debugtint : TEXCOORD8;
					#endif
					half4 grabPos : TEXCOORD9;

					UNITY_FOG_COORDS( 3 )
				};

				// GLOBAL PARAMS

				#include "OceanLODData.cginc"

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
					o.n = half3(0., 1., 0.);
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
						SampleDisplacements(_LD_Sampler_AnimatedWaves_0, uv_0, wt_0, _LD_Params_0.w, _LD_Params_0.x, o.worldPos, o.n);
						#endif

						#if _FOAM_ON
						SampleFoam(_LD_Sampler_Foam_0, uv_0, wt_0, o.foam_screenPos.x);
						#endif

						#if _APPLYFLOWTONORMALS_ON
						SampleFlow(_LD_Sampler_Flow_0, uv_0, wt_0, o.flow);
						#endif

						#if _SUBSURFACESHALLOWCOLOUR_ON
						SampleOceanDepth(_LD_Sampler_SeaFloorDepth_0, uv_0, wt_0, o.lodAlpha_worldXZUndisplaced_oceanDepth.w);
						#endif
					}
					if (wt_1 > 0.001)
					{
						const float2 uv_1 = LD_1_WorldToUV(worldXZBefore);
						#if !_DEBUGDISABLESHAPETEXTURES_ON
						SampleDisplacements(_LD_Sampler_AnimatedWaves_1, uv_1, wt_1, _LD_Params_1.w, _LD_Params_1.x, o.worldPos, o.n);
						#endif

						#if _FOAM_ON
						SampleFoam(_LD_Sampler_Foam_1, uv_1, wt_1, o.foam_screenPos.x);
						#endif

						#if _APPLYFLOWTONORMALS_ON
						SampleFlow(_LD_Sampler_Flow_1, uv_1, wt_1, o.flow);
						#endif

						#if _SUBSURFACESHALLOWCOLOUR_ON
						SampleOceanDepth(_LD_Sampler_SeaFloorDepth_1, uv_1, wt_1, o.lodAlpha_worldXZUndisplaced_oceanDepth.w);
						#endif
					}

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
				uniform half4 _Diffuse;
				uniform half _DirectionalLightFallOff;
				uniform half _DirectionalLightBoost;

				uniform half3 _SubSurfaceColour;
				uniform half _SubSurfaceBase;
				uniform half _SubSurfaceSun;
				uniform half _SubSurfaceSunFallOff;
				uniform half _SubSurfaceHeightMax;
				uniform half _SubSurfaceHeightPower;
				uniform half3 _SubSurfaceCrestColour;

				uniform half _SubSurfaceDepthMax;
				uniform half _SubSurfaceDepthPower;
				uniform half3 _SubSurfaceShallowCol;

				uniform half4 _DepthFogDensity;
				uniform samplerCUBE _Skybox;

				uniform sampler2D _FoamTexture;
				uniform float4 _FoamTexture_TexelSize;
				uniform half4 _FoamWhiteColor;
				uniform half4 _FoamBubbleColor;
				uniform half _ShorelineFoamMinDepth;
				uniform half _WaveFoamFeather;
				uniform half _WaveFoamBubblesCoverage;
				uniform half _WaveFoamNormalStrength;
				uniform half _WaveFoamSpecularFallOff;
				uniform half _WaveFoamSpecularBoost;
				uniform half _WaveFoamLightScale;

				uniform sampler2D _Normals;
				uniform half _NormalsStrength;
				uniform half _NormalsScale;
				uniform half _FoamScale;
				uniform half _FresnelPower;
				uniform fixed4 _LightColor0;
				uniform half2 _WindDirXZ;

				uniform sampler2D _CausticsTexture;
				uniform half _CausticsTextureScale;
				uniform half _CausticsTextureAverage;
				uniform half _CausticsStrength;
				uniform half _CausticsFocalDepth;
				uniform half _CausticsDepthOfField;
				uniform half _CausticsDistortionScale;
				uniform half _CausticsDistortionStrength;

				// these are copied from the render target by unity
				sampler2D _BackgroundTexture;
				sampler2D _CameraDepthTexture;

				half2 SampleNormalMaps(float2 worldXZUndisplaced, float lodAlpha)
				{
					const float2 v0 = float2(0.94, 0.34), v1 = float2(-0.85, -0.53);
					const float geomSquareSize = _GeomData.x;
					float nstretch = _NormalsScale * geomSquareSize; // normals scaled with geometry
					const float spdmulL = _GeomData.y;
					half2 norm =
						UnpackNormal(tex2D( _Normals, (v0*_Time.y*spdmulL + worldXZUndisplaced) / nstretch )).xy +
						UnpackNormal(tex2D( _Normals, (v1*_Time.y*spdmulL + worldXZUndisplaced) / nstretch )).xy;

					// blend in next higher scale of normals to obtain continuity
					const float farNormalsWeight = _InstanceData.y;
					const half nblend = lodAlpha * farNormalsWeight;
					if( nblend > 0.001 )
					{
						// next lod level
						nstretch *= 2.;
						const float spdmulH = _GeomData.z;
						norm = lerp( norm,
							UnpackNormal(tex2D( _Normals, (v0*_Time.y*spdmulH + worldXZUndisplaced) / nstretch )).xy +
							UnpackNormal(tex2D( _Normals, (v1*_Time.y*spdmulH + worldXZUndisplaced) / nstretch )).xy,
							nblend );
					}

					// approximate combine of normals. would be better if normals applied in local frame.
					return _NormalsStrength * norm;
				}

				void ApplyNormalMapsWithFlow(float2 worldXZUndisplaced, float2 flow, float lodAlpha, inout half3 io_n )
				{
					const float half_period = .05;
					const float period = half_period * 2;
					float sample1_offset = fmod(_Time, period);
					float sample1_weight = sample1_offset / half_period;
					if(sample1_weight > 1.0) sample1_weight = 2.0 - sample1_weight;
					float sample2_offset = fmod(_Time + half_period, period);
					float sample2_weight = 1.0 - sample1_weight;

					// In order to prevent flow from distorting the UVs too much,
					// we fade between two samples of normal maps so that for each
					// sample the UVs can be reset
					half2 io_n_1 = SampleNormalMaps(worldXZUndisplaced - (flow * sample1_offset), lodAlpha);
					half2 io_n_2 = SampleNormalMaps(worldXZUndisplaced - (flow * sample2_offset), lodAlpha);
					io_n.xz += sample1_weight * io_n_1;
					io_n.xz += sample2_weight * io_n_2;
					io_n = normalize(io_n);
				}

				half3 AmbientLight()
				{
					return half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
				}

				half WhiteFoamTexture(half i_foam, float2 i_worldXZUndisplaced)
				{
					half ft = lerp(
						tex2D(_FoamTexture, (1.25*i_worldXZUndisplaced + _Time.y / 10.) / _FoamScale).r,
						tex2D(_FoamTexture, (3.00*i_worldXZUndisplaced - _Time.y / 10.) / _FoamScale).r,
						0.5);

					// black point fade
					i_foam = saturate(1. - i_foam);
					return smoothstep(i_foam, i_foam + _WaveFoamFeather, ft);
				}

				void ComputeFoam(half i_foam, float2 i_worldXZUndisplaced, float2 i_worldXZ, half3 i_n, float i_pixelZ, float i_sceneZ, half3 i_view, float3 i_lightDir, out half3 o_bubbleCol, out half4 o_whiteFoamCol)
				{
					half foamAmount = i_foam;

					// feather foam very close to shore
					foamAmount *= saturate((i_sceneZ - i_pixelZ) / _ShorelineFoamMinDepth);

					// Additive underwater foam - use same foam texture but add mip bias to blur for free
					float2 foamUVBubbles = (lerp(i_worldXZUndisplaced, i_worldXZ, 0.05) + 0.5 * _Time.y * _WindDirXZ) / _FoamScale + 0.125 * i_n.xz;
					half bubbleFoamTexValue = tex2Dlod(_FoamTexture, float4(.74 * foamUVBubbles - .05*i_view.xz / i_view.y, 0., 5.)).r;
					o_bubbleCol = (half3)bubbleFoamTexValue * _FoamBubbleColor.rgb * saturate(i_foam * _WaveFoamBubblesCoverage);

					// White foam on top, with black-point fading
					half whiteFoam = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced);

					#if _FOAM3DLIGHTING_ON
					// Scale up delta by Z - keeps 3d look better at distance. better way to do this?
					float2 dd = float2(0.25 * i_pixelZ * _FoamTexture_TexelSize.x, 0.);
					half whiteFoam_x = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced + dd.xy);
					half whiteFoam_z = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced + dd.yx);

					// compute a foam normal
					half dfdx = whiteFoam_x - whiteFoam, dfdz = whiteFoam_z - whiteFoam;
					half3 fN = normalize(i_n + _WaveFoamNormalStrength * half3(-dfdx, 0., -dfdz));
					// do simple NdL and phong lighting
					half foamNdL = max(0., dot(fN, i_lightDir));
					o_whiteFoamCol.rgb = _FoamWhiteColor.rgb * (AmbientLight() + _WaveFoamLightScale * _LightColor0 * foamNdL);
					half3 refl = reflect(-i_view, fN);
					o_whiteFoamCol.rgb += pow(max(0., dot(refl, i_lightDir)), _WaveFoamSpecularFallOff) * _WaveFoamSpecularBoost * _LightColor0;
					#else // _FOAM3DLIGHTING_ON
					o_whiteFoamCol.rgb = _FoamWhiteColor.rgb * (AmbientLight() + _WaveFoamLightScale * _LightColor0);
					#endif // _FOAM3DLIGHTING_ON

					o_whiteFoamCol.a = _FoamWhiteColor.a * whiteFoam;
				}

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

				half3 OceanEmission(float3 worldPos, half oceanDepth, half3 view, half3 n, half3 n_geom, float3 lightDir, half4 grabPos, half3 screenPos, float pixelZ, half2 uvDepth, float sceneZ, float sceneZ01, half3 bubbleCol)
				{
					// base colour
					half3 col = _Diffuse;

					#if _SUBSURFACESCATTERING_ON
					{
						#if _SUBSURFACESHALLOWCOLOUR_ON
						float deepness = pow(1. - saturate(oceanDepth / _SubSurfaceDepthMax), _SubSurfaceDepthPower);
						col = lerp(col, _SubSurfaceShallowCol, deepness);
						#endif

						#if _SUBSURFACEHEIGHTLERP_ON
						half h = worldPos.y - _OceanCenterPosWorld.y;
						col += pow(saturate(0.5 + 2.0 * h / _SubSurfaceHeightMax), _SubSurfaceHeightPower) * _SubSurfaceCrestColour.rgb;
						#endif

						// light
						// use the constant term (0th order) of SH stuff - this is the average. it seems to give the right kind of colour
						col *= half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

						// Approximate subsurface scattering - add light when surface faces viewer. Use geometry normal - don't need high freqs.
						half towardsSun = pow(max(0., dot(lightDir, -view)), _SubSurfaceSunFallOff);
						col += (_SubSurfaceBase + _SubSurfaceSun * towardsSun) * max(dot(n_geom, view), 0.) * _SubSurfaceColour.rgb * _LightColor0;
					}
					#endif // _SUBSURFACESCATTERING_ON

					// underwater bubbles reflect in light
					col += bubbleCol;

					#if _TRANSPARENCY_ON

					// zfar? then don't read from the backbuffer at all, as i get occasionally nans spread across the screen when reading
					// from uninit'd backbuffer
					if (sceneZ01 != 0.0)
					{
						half2 uvBackgroundRefract = grabPos.xy / grabPos.w + .02 * n.xz;
						half2 uvDepthRefract = uvDepth + .02 * n.xz;
						half3 alpha = (half3)1.;

						// if we haven't refracted onto a surface in front of the water surface, compute an alpha based on Z delta
						if (sceneZ > pixelZ)
						{
							float sceneZRefract = LinearEyeDepth(tex2D(_CameraDepthTexture, uvDepthRefract).x);
							float maxZ = max(sceneZ, sceneZRefract);
							float deltaZ = maxZ - pixelZ;
							alpha = 1. - exp(-_DepthFogDensity.xyz * deltaZ);
						}

						half3 sceneColour = tex2D(_BackgroundTexture, uvBackgroundRefract).rgb;

						#if _CAUSTICS_ON
						// underwater caustics - dedicated to P
						float3 camForward = mul((float3x3)unity_CameraToWorld, float3(0., 0., 1.));
						float3 scenePos = _WorldSpaceCameraPos - view * sceneZ / dot(camForward, -view);
						half sceneDepth = _OceanCenterPosWorld.y - scenePos.y;
						half bias = abs(sceneDepth - _CausticsFocalDepth) / _CausticsDepthOfField;
						half2 causticN = _CausticsDistortionStrength * UnpackNormal(tex2D(_Normals, scenePos.xz / _CausticsDistortionScale)).xy;
						half4 cuv1 = half4((scenePos.xz / _CausticsTextureScale + 1.3 *causticN + half2(0.88*_Time.x + 17.16, -3.38*_Time.x)), 0., bias);
						half4 cuv2 = half4((1.37*scenePos.xz / _CausticsTextureScale + 1.77*causticN + half2(4.96*_Time.x, 2.34*_Time.x)), 0., bias);
						sceneColour *= 1. + _CausticsStrength *
							(0.5*tex2Dbias(_CausticsTexture, cuv1).x + 0.5*tex2Dbias(_CausticsTexture, cuv2).x - _CausticsTextureAverage);
						#endif

						col = lerp(sceneColour, col, alpha);
					}
					#endif // _TRANSPARENCY_ON

					return col;
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

					// could be per-vertex i reckon
					float3 lightDir = WorldSpaceLightDir(i.worldPos);

					// Normal - geom + normal mapping
					half3 n_geom = normalize(i.n);
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
					ComputeFoam(i.foam_screenPos.x, i.lodAlpha_worldXZUndisplaced_oceanDepth.yz, i.worldPos.xz, n_pixel, pixelZ, sceneZ, view, lightDir, bubbleCol, whiteFoamCol);
					#endif

					// Compute color of ocean - in-scattered light + refracted scene
					half3 col = OceanEmission(i.worldPos, i.lodAlpha_worldXZUndisplaced_oceanDepth.w, view, n_pixel, n_geom, lightDir, i.grabPos, screenPos, pixelZ, uvDepth, sceneZ, sceneZ01, bubbleCol);

					// Reflection
					half3 refl = reflect(-view, n_pixel);
					half3 skyColor = texCUBE(_Skybox, refl);
					#if _COMPUTEDIRECTIONALLIGHT_ON
					skyColor += pow(max(0., dot(refl, lightDir)), _DirectionalLightFallOff) * _DirectionalLightBoost * _LightColor0;
					#endif

					// Fresnel
					const float IOR_AIR = 1.0;
					const float IOR_WATER = 1.33;
					// reflectance at facing angle
					float R_0 = (IOR_AIR - IOR_WATER) / (IOR_AIR + IOR_WATER); R_0 *= R_0;
					// schlick's approximation
					float R_theta = R_0 + (1.0 - R_0) * pow(1.0 - max(dot(n_pixel, view), 0.), _FresnelPower);
					col = lerp(col, skyColor, R_theta);

					// Override final result with white foam - bubbles on surface
					#if _FOAM_ON
					col = lerp(col, whiteFoamCol.rgb, whiteFoamCol.a);
					#endif

					// Fog
					UNITY_APPLY_FOG(i.fogCoord, col);

					#if _DEBUGVISUALISESHAPESAMPLE_ON
					col = mix(col.rgb, i.debugtint, 0.5);
					#endif
					#if _DEBUGVISUALISEFLOW_ON
					col.rg = mix(col.rg, i.flow.xy, 0.5);
					#endif

					return half4(col, 1.);
				}

				ENDCG
			}
		}
	}
}
