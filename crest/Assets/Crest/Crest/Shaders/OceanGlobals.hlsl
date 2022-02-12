// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// GLOBALs - we're allowed to use these anywhere. TODO should all be prefixed by "Crest"!

#ifndef CREST_OCEAN_GLOBALS_H
#define CREST_OCEAN_GLOBALS_H

SamplerState LODData_linear_clamp_sampler;
SamplerState LODData_point_clamp_sampler;
SamplerState sampler_Crest_linear_repeat;

CBUFFER_START(CrestPerFrame)
float3 _OceanCenterPosWorld;
float _CrestTime;
float _SliceCount;
float _MeshScaleLerp;
float _CrestClipByDefault;
float _CrestLodAlphaBlackPointFade;
float _CrestLodAlphaBlackPointWhitePointFade;
int _CrestDepthTextureOffset;
int _CrestDataSliceOffset;
float3 _CrestFloatingOriginOffset;
// Hack - due to SV_IsFrontFace occasionally coming through as true for
// backfaces, add a param here that forces ocean to be in undrwater state. I
// think the root cause here might be imprecision or numerical issues at ocean
// tile boundaries, although I'm not sure why cracks are not visible in this case.
float _CrestForceUnderwater;

float3 _PrimaryLightDirection;
float3 _PrimaryLightIntensity;
CBUFFER_END

#endif
