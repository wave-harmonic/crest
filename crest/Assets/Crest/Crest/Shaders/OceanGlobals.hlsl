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

float3 _PrimaryLightDirection;
float3 _PrimaryLightIntensity;
CBUFFER_END

#endif
