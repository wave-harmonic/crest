// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_OCEAN_SHADER_DATA_INCLUDED
#define CREST_OCEAN_SHADER_DATA_INCLUDED

#include "OceanConstants.hlsl"

/////////////////////////////
// Samplers

TEXTURE2D_X(_CameraDepthTexture2); SAMPLER(sampler_CameraDepthTexture2);
UNITY_DECLARE_SCREENSPACE_TEXTURE(_BackgroundTexture);

half3 _CrestAmbientLighting;

float4 _CameraDepthTexture2_TexelSize;

#if defined(_APPLYNORMALMAPPING_ON) || defined(_CAUSTICS_ON)
Texture2D _Normals;
SamplerState sampler_Normals;
#endif

#if _FOAM_ON
Texture2D _FoamTexture;
SamplerState sampler_FoamTexture;
#endif

#if _CAUSTICS_ON
Texture2D _CausticsTexture;
SamplerState sampler_CausticsTexture;
#endif

TEXTURE2D_X(_CrestScreenSpaceShadowTexture);
float4 _CrestScreenSpaceShadowTexture_TexelSize;

TEXTURE2D(_ReflectionTex); SAMPLER(sampler_ReflectionTex);
#if _OVERRIDEREFLECTIONCUBEMAP_ON
TEXTURECUBE(_ReflectionCubemapOverride); SAMPLER(sampler_ReflectionCubemapOverride);
#endif

/////////////////////////////
// Constant buffer: CrestPerMaterial

CBUFFER_START(CrestInputsPerMaterial)

// ----------------------------------------------------------------------------
// Diffuse
// ----------------------------------------------------------------------------

half3 _Diffuse;
half3 _DiffuseGrazing;
#if _SHADOWS_ON
half3 _DiffuseShadow;
#endif

// ----------------------------------------------------------------------------
// Transparency
// ----------------------------------------------------------------------------

#if _TRANSPARENCY_ON
half _RefractionStrength;
#endif
half4 _DepthFogDensity;
half4 _CrestDepthFogDensity;

// ----------------------------------------------------------------------------
// Normals
// ----------------------------------------------------------------------------

#if defined(_APPLYNORMALMAPPING_ON) || defined(_CAUSTICS_ON)
float4 _Normals_TexelSize;
#endif

half _NormalsStrengthOverall;
#if _APPLYNORMALMAPPING_ON
half _NormalsStrength;
half _NormalsScale;
static const WaveHarmonic::Crest::TiledTexture _NormalsTiledTexture =
    WaveHarmonic::Crest::TiledTexture::Make(_Normals, sampler_Normals, _Normals_TexelSize, _NormalsScale);
#endif

// ----------------------------------------------------------------------------
// Sub-Surface Scattering
// ----------------------------------------------------------------------------

#if _SUBSURFACESCATTERING_ON
half3 _SubSurfaceColour;
half _SubSurfaceBase;
half _SubSurfaceSun;
half _SubSurfaceSunFallOff;
#endif

#if _SUBSURFACESHALLOWCOLOUR_ON
half _SubSurfaceDepthMax;
half _SubSurfaceDepthPower;
half3 _SubSurfaceShallowCol;
#if _SHADOWS_ON
half3 _SubSurfaceShallowColShadow;
#endif
#endif

// ----------------------------------------------------------------------------
// Reflections
// ----------------------------------------------------------------------------

half _Specular;
half _Roughness;
half _FresnelPower;
float _RefractiveIndexOfAir;
float _RefractiveIndexOfWater;

#if _COMPUTEDIRECTIONALLIGHT_ON
half _DirectionalLightFallOff;
half _DirectionalLightBoost;
#if _DIRECTIONALLIGHTVARYROUGHNESS_ON
half _DirectionalLightFarDistance;
half _DirectionalLightFallOffFar;
#endif
#endif

#if _PLANARREFLECTIONS_ON
half _PlanarReflectionNormalsStrength;
half _PlanarReflectionDistanceFactor;
half _PlanarReflectionIntensity;
#endif

#if _PROCEDURALSKY_ON
half3 _SkyBase;
half3 _SkyAwayFromSun;
half3 _SkyTowardsSun;
half _SkyDirectionality;
#endif

// ----------------------------------------------------------------------------
// Foam
// ----------------------------------------------------------------------------

#if _FOAM_ON
half _FoamScale;
half4 _FoamWhiteColor;
half _WaveFoamFeather;
half _WaveFoamLightScale;
half _ShorelineFoamMinDepth;
#if _FOAM3DLIGHTING_ON
half _WaveFoamNormalStrength;
half _WaveFoamSpecularFallOff;
half _WaveFoamSpecularBoost;
#endif
// Foam Bubbles
half4 _FoamBubbleColor;
half _FoamBubbleParallax;
half _WaveFoamBubblesCoverage;

float4 _FoamTexture_TexelSize;

static const WaveHarmonic::Crest::TiledTexture _FoamTiledTexture =
    WaveHarmonic::Crest::TiledTexture::Make(_FoamTexture, sampler_FoamTexture, _FoamTexture_TexelSize, _FoamScale);
#endif

// ----------------------------------------------------------------------------
// Caustics
// ----------------------------------------------------------------------------

#if _CAUSTICS_ON
half _CausticsTextureScale;
half _CausticsTextureAverage;
half _CausticsStrength;
half _CausticsFocalDepth;
half _CausticsDepthOfField;
half _CausticsDistortionScale;
half _CausticsDistortionStrength;

float4 _CausticsTexture_TexelSize;

static const WaveHarmonic::Crest::TiledTexture _CausticsTiledTexture =
    WaveHarmonic::Crest::TiledTexture::Make(_CausticsTexture, sampler_CausticsTexture, _CausticsTexture_TexelSize, _CausticsTextureScale);
static const WaveHarmonic::Crest::TiledTexture _CausticsDistortionTiledTexture =
    WaveHarmonic::Crest::TiledTexture::Make(_Normals, sampler_Normals, _Normals_TexelSize, _CausticsDistortionScale);
#endif

// TODO: This can be removed once we use the underwater post-process effect.
float _HeightOffset;

CBUFFER_END

#endif // CREST_OCEAN_SHADER_DATA_INCLUDED
