// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_INPUTS_DRIVEN_H
#define CREST_INPUTS_DRIVEN_H

#include "OceanConstants.hlsl"

CBUFFER_START(CrestOceanSurfaceDrivenValues)
uint _LD_SliceIndex;
int _CrestLodChange;
CBUFFER_END

// This must exactly match struct with same name in C#
// :CascadeParams
struct CascadeParams
{
	float2 _posSnapped;
	float _scale;
	float _textureRes;
	float _oneOverTextureRes;
	float _texelWidth;
	float _weight;
	float _maxWavelength;
};

StructuredBuffer<CascadeParams> _CrestCascadeData;
StructuredBuffer<CascadeParams> _CrestCascadeDataSource;

// This must exactly match struct with same name in C#
// :PerCascadeInstanceData
struct PerCascadeInstanceData
{
	float _meshScaleLerp;
	float _farNormalsWeight;
	float _geoGridWidth;
	float2 _normalScrollSpeeds;
	// Align to 32 bytes
	float3 __padding;
};

StructuredBuffer<PerCascadeInstanceData> _CrestPerCascadeInstanceData;
StructuredBuffer<PerCascadeInstanceData> _CrestPerCascadeInstanceDataSource;

Texture2DArray _LD_TexArray_AnimatedWaves;
Texture2DArray _LD_TexArray_WaveBuffer;
Texture2DArray _LD_TexArray_SeaFloorDepth;
Texture2DArray _LD_TexArray_ClipSurface;
Texture2DArray _LD_TexArray_Foam;
Texture2DArray _LD_TexArray_Flow;
Texture2DArray _LD_TexArray_DynamicWaves;
Texture2DArray _LD_TexArray_Shadow;

// These are used in lods where we operate on data from
// previously calculated lods. Used in simulations and
// shadowing for example.
Texture2DArray _LD_TexArray_AnimatedWaves_Source;
Texture2DArray _LD_TexArray_WaveBuffer_Source;
Texture2DArray _LD_TexArray_ClipSurface_Source;
Texture2DArray _LD_TexArray_Foam_Source;
Texture2DArray _LD_TexArray_Flow_Source;
Texture2DArray _LD_TexArray_DynamicWaves_Source;
Texture2DArray _LD_TexArray_Shadow_Source;

#endif // CREST_INPUTS_DRIVEN_H
