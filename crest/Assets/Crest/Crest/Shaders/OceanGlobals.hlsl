// Crest Ocean System

// Copyright 2020 Wave Harmonic Ltd

// GLOBALs - we're allowed to use these anywhere. TODO should all be prefixed by "Crest"!

#ifndef CREST_OCEAN_GLOBALS_H
#define CREST_OCEAN_GLOBALS_H

SamplerState LODData_linear_clamp_sampler;
SamplerState LODData_point_clamp_sampler;
SamplerState sampler_Crest_linear_repeat;

//CBUFFER_START(CrestPerFrame)
float _CrestTime;
half _TexelsPerWave;
float3 _OceanCenterPosWorld;
float _SliceCount;
float _MeshScaleLerp;

float3 _PrimaryLightDirection;
float3 _PrimaryLightIntensity;
//CBUFFER_END

#endif
