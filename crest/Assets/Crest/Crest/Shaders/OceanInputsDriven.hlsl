// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_INPUTS_DRIVEN_H
#define CREST_INPUTS_DRIVEN_H

#include "OceanConstants.hlsl"

CBUFFER_START(CrestOceanSurfaceDrivenValues)
uint _LD_SliceIndex;
CBUFFER_END

struct CascadeParams
{
	float2 _posSnapped;
	float _scale;
	float _textureRes;
	float _oneOverTextureRes;
	float _texelWidth;
	float _weight;
};

// Create two sets of LOD data, which have overloaded meaning depending on use:
// * the ocean surface geometry always lerps from a more detailed LOD (0) to a less detailed LOD (1)
// * simulations (persistent lod data) read last frame's data from slot 0, and any current frame data from slot 1
// * any other use that does not fall into the previous categories can use either slot and generally use slot 0
StructuredBuffer<CascadeParams> _CascadeDataTgt;
StructuredBuffer<CascadeParams> _CascadeDataSrc;

struct PerCascadeInstanceData
{
	float _meshScaleLerp;
	float _farNormalsWeight;
	float _geoGridWidth;
	float2 _normalScrollSpeeds;
};

StructuredBuffer<PerCascadeInstanceData> _PerCascadeInstanceData;

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
Texture2DArray _LD_TexArray_SeaFloorDepth_Source;
Texture2DArray _LD_TexArray_ClipSurface_Source;
Texture2DArray _LD_TexArray_Foam_Source;
Texture2DArray _LD_TexArray_Flow_Source;
Texture2DArray _LD_TexArray_DynamicWaves_Source;
Texture2DArray _LD_TexArray_Shadow_Source;

#endif // CREST_INPUTS_DRIVEN_H
