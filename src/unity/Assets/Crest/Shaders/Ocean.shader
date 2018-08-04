// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Ocean"
{
	Properties
	{
		[Toggle] _ApplyNormalMapping("Apply Normal Mapping", Float) = 1
		[NoScaleOffset] _Normals ( "Normals", 2D ) = "bump" {}
		_NormalsStrength("Normals Strength", Range(0.0, 2.0)) = 0.3
		_NormalsScale("Normals Scale", Range(0.01, 50.0)) = 1.0
		[NoScaleOffset] _Skybox ("Skybox", CUBE) = "" {}
		_Diffuse("Diffuse", Color) = (0.2, 0.05, 0.05, 1.0)
		[Toggle] _ComputeDirectionalLight("Add Directional Light", Float) = 1
		_DirectionalLightFallOff("Directional Light Fall-Off", Range(1.0, 512.0)) = 128.0
		_DirectionalLightBoost("Directional Light Boost", Range(0.0, 16.0)) = 5.0
		[Toggle] _SubSurfaceScattering("Sub-Surface Scattering", Float) = 1
		_SubSurfaceColour("Sub-Surface Scattering Colour", Color) = (0.0, 0.48, 0.36, 1.)
		_SubSurfaceBase("Sub-Surface Scattering Base Mul", Range(0.0, 2.0)) = 0.6
		_SubSurfaceSun("Sub-Surface Scattering Sun Mul", Range(0.0, 10.0)) = 0.8
		_SubSurfaceSunFallOff("Sub-Surface Scattering Sun Fall-Off", Range(1.0, 16.0)) = 4.0
		[Toggle] _Foam("Foam", Float) = 1
		[NoScaleOffset] _FoamTexture ( "Foam Texture", 2D ) = "white" {}
		_FoamScale("Foam Scale", Range(0.01, 50.0)) = 10.0
		_FoamWhiteColor("White Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_FoamBubbleColor("Bubble Foam Color", Color) = (0.0, 0.0904, 0.105, 1.0)
		_ShorelineFoamMinDepth("Shoreline Foam Min Depth", Range(0.01, 5.0)) = 0.27
		_WaveFoamCoverage("Wave Foam Coverage", Range(0.0,5.0)) = 0.95
		_WaveFoamFeather("Wave Foam Feather", Range(0.001,1.0)) = 0.32
		_WaveFoamBubblesCoverage("Wave Foam Bubbles Coverage", Range(0.0,5.0)) = 0.95
		[Toggle] _Foam3DLighting("Foam 3D Lighting", Float) = 1
		_WaveFoamLightScale("Wave Foam Light Scale", Range(0.0, 2.0)) = 0.7
		_WaveFoamNormalStrength("Wave Foam Normals Strength", Range(0.0, 10.0)) = 3.5
		_WaveFoamSpecularFallOff("Wave Foam Specular Fall-Off", Range(1.0, 512.0)) = 275.0
		_WaveFoamSpecularBoost("Wave Foam Specular Boost", Range(0.0, 16.0)) = 4.0
		[Toggle] _Transparency("Transparency", Float) = 1
		_DepthFogDensity("Depth Fog Density", Vector) = (0.28, 0.16, 0.24, 1.0)
		_FresnelPower("Fresnel Power", Range(0.0,20.0)) = 3.0
		[Toggle] _DebugDisableShapeTextures("Debug Disable Shape Textures", Float) = 0
		[Toggle] _DebugVisualiseShapeSample("Debug Visualise Shape Sample", Float) = 0
		[Toggle] _DebugDisableSmoothLOD("Debug Disable Smooth LOD", Float) = 0
		[Toggle] _CompileShaderWithDebugInfo("Compile Shader With Debug Info (D3D11)", Float) = 0
	}

	Category
	{
		Tags {}

		SubShader
		{
			Tags { "LightMode"="ForwardBase" "Queue"="Geometry+100" "IgnoreProjector"="True" "RenderType"="Opaque" }

			GrabPass
			{
				"_BackgroundTexture"
			}

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#pragma shader_feature _APPLYNORMALMAPPING_ON
				#pragma shader_feature _COMPUTEDIRECTIONALLIGHT_ON
				#pragma shader_feature _SUBSURFACESCATTERING_ON
				#pragma shader_feature _TRANSPARENCY_ON
				#pragma shader_feature _FOAM_ON
				#pragma shader_feature _FOAM3DLIGHTING_ON
				#pragma shader_feature _DEBUGDISABLESHAPETEXTURES_ON
				#pragma shader_feature _DEBUGVISUALISESHAPESAMPLE_ON
				#pragma shader_feature _DEBUGDISABLESMOOTHLOD_ON
				#pragma shader_feature _COMPILESHADERWITHDEBUGINFO_ON

				#if _COMPILESHADERWITHDEBUGINFO_ON
				#pragma enable_d3d11_debug_symbols
				#endif

				#include "UnityCG.cginc"
				#include "TextureBombing.cginc"

				struct appdata_t
				{
					float4 vertex : POSITION;
					float2 texcoord: TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					half3 n : TEXCOORD1;
					half4 foam_screenPos : TEXCOORD4;
					half3 lodAlpha_worldXZUndisplaced : TEXCOORD5;
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
					o.lodAlpha_worldXZUndisplaced.x = lodAlpha;
					o.lodAlpha_worldXZUndisplaced.yz = o.worldPos.xz;

					// sample shape textures - always lerp between 2 scales, so sample two textures
					o.n = half3(0., 1., 0.);
					o.foam_screenPos.x = 0.;
					// sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
					float wt_0 = (1. - lodAlpha) * _WD_Params_0.z;
					float wt_1 = (1. - wt_0) * _WD_Params_1.z;
					// sample displacement textures, add results to current world pos / normal / foam
					#if !_DEBUGDISABLESHAPETEXTURES_ON
					const float2 worldXZBefore = o.worldPos.xz;
					SampleDisplacements( _WD_Sampler_0, _WD_OceanDepth_Sampler_0, _WD_Pos_Scale_0.xy, _WD_Params_0.y, _WD_Params_0.w, _WD_Params_0.x, worldXZBefore, wt_0, o.worldPos, o.n, o.foam_screenPos.x);
					SampleDisplacements( _WD_Sampler_1, _WD_OceanDepth_Sampler_1, _WD_Pos_Scale_1.xy, _WD_Params_1.y, _WD_Params_1.w, _WD_Params_1.x, worldXZBefore, wt_1, o.worldPos, o.n, o.foam_screenPos.x);
					#endif

					// debug tinting to see which shape textures are used
					#if _DEBUGVISUALISESHAPESAMPLE_ON
					#define TINT_COUNT (uint)7
					half3 tintCols[TINT_COUNT]; tintCols[0] = half3(1., 0., 0.); tintCols[1] = half3(1., 1., 0.); tintCols[2] = half3(1., 0., 1.); tintCols[3] = half3(0., 1., 1.); tintCols[4] = half3(0., 0., 1.); tintCols[5] = half3(1., 0., 1.); tintCols[6] = half3(.5, .5, 1.);
					o.debugtint = wt_0 * tintCols[_WD_LodIdx_0 % TINT_COUNT] + wt_1 * tintCols[_WD_LodIdx_1 % TINT_COUNT];
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

				uniform half4 _SubSurfaceColour;
				uniform half _SubSurfaceBase;
				uniform half _SubSurfaceSun;
				uniform half _SubSurfaceSunFallOff;

				uniform half4 _DepthFogDensity;
				uniform samplerCUBE _Skybox;

				uniform sampler2D _FoamTexture;
				uniform float4 _FoamTexture_TexelSize;
				uniform half4 _FoamWhiteColor;
				uniform half4 _FoamBubbleColor;
				uniform half _ShorelineFoamMinDepth;
				uniform half _WaveFoamCoverage, _WaveFoamFeather;
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
				uniform float _MyTime;
				uniform fixed4 _LightColor0;
				uniform half2 _WindDirXZ;

				// these are copied from the render target by unity
				sampler2D _BackgroundTexture;
				sampler2D _CameraDepthTexture;

				void ApplyNormalMaps(float2 worldXZUndisplaced, float lodAlpha, inout half3 io_n )
				{
					const float2 v0 = float2(0.94, 0.34), v1 = float2(-0.85, -0.53);
					const float geomSquareSize = _GeomData.x;
					float nstretch = _NormalsScale * geomSquareSize; // normals scaled with geometry
					const float spdmulL = _GeomData.y;
					half2 norm =
						UnpackNormal(tex2D( _Normals, (v0*_MyTime*spdmulL + worldXZUndisplaced) / nstretch )).xy +
						UnpackNormal(tex2D( _Normals, (v1*_MyTime*spdmulL + worldXZUndisplaced) / nstretch )).xy;

					// blend in next higher scale of normals to obtain continuity
					const float farNormalsWeight = _InstanceData.y;
					const half nblend = lodAlpha * farNormalsWeight;
					if( nblend > 0.001 )
					{
						// next lod level
						nstretch *= 2.;
						const float spdmulH = _GeomData.z;
						norm = lerp( norm,
							UnpackNormal(tex2D( _Normals, (v0*_MyTime*spdmulH + worldXZUndisplaced) / nstretch )).xy +
							UnpackNormal(tex2D( _Normals, (v1*_MyTime*spdmulH + worldXZUndisplaced) / nstretch )).xy,
							nblend );
					}

					// approximate combine of normals. would be better if normals applied in local frame.
					io_n.xz += _NormalsStrength * norm;
					io_n = normalize(io_n);
				}

				half3 AmbientLight()
				{
					return half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
				}

				half WhiteFoamTexture(half i_foam, float2 i_worldXZUndisplaced)
				{
					half ft = lerp(
						texture(_FoamTexture, (1.25*i_worldXZUndisplaced + _MyTime / 10.) / _FoamScale).r,
						texture(_FoamTexture, (3.00*i_worldXZUndisplaced - _MyTime / 10.) / _FoamScale).r,
						0.5);

					// black point fade
					i_foam = saturate(1. - i_foam + _WaveFoamCoverage);
					return smoothstep(i_foam, i_foam + _WaveFoamFeather, ft);
				}

				void ComputeFoam(half i_foam, float2 i_worldXZUndisplaced, float2 i_worldXZ, half3 i_n, float i_pixelZ, float i_sceneZ, half3 i_view, float3 i_lightDir, out half3 o_bubbleCol, out half4 o_whiteFoamCol)
				{
					half foamAmount = i_foam;
					
					// feather foam very close to shore
					foamAmount *= saturate((i_sceneZ - i_pixelZ) / _ShorelineFoamMinDepth);

					// Additive underwater foam - use same foam texture but add mip bias to blur for free
					float2 foamUVBubbles = (lerp(i_worldXZUndisplaced, i_worldXZ, 0.05) + 0.5 * _MyTime * _WindDirXZ) / _FoamScale + 0.25 * i_n.xz;
					half bubbleFoamTexValue = tex2Dlod(_FoamTexture, float4(.74 * foamUVBubbles - .05*i_view.xz / i_view.y, 0., 5.)).r;
					o_bubbleCol = (half3)bubbleFoamTexValue * _FoamBubbleColor.rgb * saturate(i_foam - _WaveFoamBubblesCoverage);

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

					o_whiteFoamCol.a = min(2. * whiteFoam, _FoamWhiteColor.a);
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

				half3 OceanEmission(half3 view, half3 n, half3 n_geom, float3 lightDir, half4 grabPos, half3 screenPos, float pixelZ, half2 uvDepth, float sceneZ, float sceneZ01, half3 bubbleCol)
				{
					// use the constant layer of SH stuff - this is the average. it seems to give the right kind of colour
					half3 col = _Diffuse * half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

					#if _SUBSURFACESCATTERING_ON
					// Approximate subsurface scattering - add light when surface faces viewer. Use geometry normal - don't need high freqs.
					half towardsSun = pow(max(0., dot(lightDir, -view)), _SubSurfaceSunFallOff);
					col += (_SubSurfaceBase + _SubSurfaceSun * towardsSun) * max(dot(n_geom, view), 0.) * _SubSurfaceColour * _LightColor0;
					#endif

					col += bubbleCol;

					#if _TRANSPARENCY_ON
					// zfar? then don't read from the backbuffer at all, as i get occasionally nans spread across the screen when reading
					// from uninit'd backbuffer
					if (sceneZ01 == 0.0)
						return col;

					half2 uvBackgroundRefract = grabPos.xy / grabPos.w + .02 * n.xz;
					half2 uvDepthRefract = uvDepth +.02 * n.xz;
					half3 alpha = (half3)1.;

					// if we haven't refracted onto a surface in front of the water surface, compute an alpha based on Z delta
					if (sceneZ > pixelZ)
					{
						float sceneZRefract = LinearEyeDepth(texture(_CameraDepthTexture, uvDepthRefract).x);
						float maxZ = max(sceneZ, sceneZRefract);
						float deltaZ = maxZ - pixelZ;
						alpha = 1. - exp(-_DepthFogDensity.xyz * deltaZ);
					}

					half3 sceneColour = texture(_BackgroundTexture, uvBackgroundRefract).rgb;
					col = lerp(sceneColour, col, alpha);
					#endif

					return col;
				}

				half3 frag(v2f i) : SV_Target
				{
					half3 view = normalize(_WorldSpaceCameraPos - i.worldPos);

					// water surface depth, and underlying scene opaque surface depth
					float pixelZ = LinearEyeDepth(i.vertex.z);
					half3 screenPos = i.foam_screenPos.yzw;
					half2 uvDepth = screenPos.xy / screenPos.z;
					float sceneZ01 = texture(_CameraDepthTexture, uvDepth).x;
					float sceneZ = LinearEyeDepth(sceneZ01);

					// could be per-vertex i reckon
					float3 lightDir = WorldSpaceLightDir(i.worldPos);

					// Normal - geom + normal mapping
					half3 n_geom = normalize(i.n);
					half3 n_pixel = n_geom;
					#if _APPLYNORMALMAPPING_ON
					ApplyNormalMaps(i.lodAlpha_worldXZUndisplaced.yz, i.lodAlpha_worldXZUndisplaced.x, n_pixel);
					#endif

					// Foam - underwater bubbles and whitefoam
					half3 bubbleCol = (half3)0.;
					#if _FOAM_ON
					half4 whiteFoamCol;
					ComputeFoam(i.foam_screenPos.x, i.lodAlpha_worldXZUndisplaced.yz, i.worldPos.xz, n_pixel, pixelZ, sceneZ, view, lightDir, bubbleCol, whiteFoamCol);
					#endif

					// Compute color of ocean - in-scattered light + refracted scene
					half3 col = OceanEmission(view, n_pixel, n_geom, lightDir, i.grabPos, screenPos, pixelZ, uvDepth, sceneZ, sceneZ01, bubbleCol);

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

					return col;
				}

				ENDCG
			}
		}
	}
}
