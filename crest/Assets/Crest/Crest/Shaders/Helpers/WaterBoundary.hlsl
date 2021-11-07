// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_WATER_BOUNDARY_INCLUDED
#define CREST_WATER_BOUNDARY_INCLUDED

TEXTURE2D_X(_CrestWaterBoundaryGeometryFrontFaceTexture);
TEXTURE2D_X(_CrestWaterBoundaryGeometryBackFaceTexture);

#if CREST_BOUNDARY
void ApplyBoundaryToOceanSurface(const float4 i_positionCS)
{
#if CREST_BOUNDARY_HAS_BACKFACE
	// Discard ocean after boundary or when not on pixel.
	float rawBackFaceBoundaryDepth = LOAD_DEPTH_TEXTURE_X(_CrestWaterBoundaryGeometryBackFaceTexture, i_positionCS.xy);
	if (rawBackFaceBoundaryDepth == 0.0 || rawBackFaceBoundaryDepth > i_positionCS.z)
	{
		discard;
	}
#endif // CREST_BOUNDARY_VOLUME

	// Discard ocean before boundary.
	float rawFrontFaceBoundaryDepth = LOAD_DEPTH_TEXTURE_X(_CrestWaterBoundaryGeometryFrontFaceTexture, i_positionCS.xy);
	if (rawFrontFaceBoundaryDepth > 0.0 && rawFrontFaceBoundaryDepth < i_positionCS.z)
	{
		discard;
	}

#if CREST_BOUNDARY_2D
	// Discard ocean when plane is not in view.
	if (rawFrontFaceBoundaryDepth == 0.0)
	{
		discard;
	}
#endif // CREST_BOUNDARY_2D
}
#endif

#if CREST_BOUNDARY
void ApplyBoundaryToOceanMask(const float4 i_positionCS)
{
#if CREST_BOUNDARY_HAS_BACKFACE
	// If no geometry in view, do not render otherwise meniscus will appear at edges.
	if (LOAD_DEPTH_TEXTURE_X(_CrestWaterBoundaryGeometryBackFaceTexture, i_positionCS.xy) == 0.0)
	{
		discard;
	}
#endif // CREST_BOUNDARY_HAS_BACKFACE

	// Discard any pixels in front of the boundary geometry otherwise the mask will be incorrect at eye level.
	float rawFrontFace = LOAD_DEPTH_TEXTURE_X(_CrestWaterBoundaryGeometryFrontFaceTexture, i_positionCS.xy);
	if (rawFrontFace > 0 && rawFrontFace < i_positionCS.z)
	{
		discard;
	}
}
#endif

void ApplyWaterBoundaryToOceanHorizon(const float4 i_positionCS)
{
#if CREST_BOUNDARY_HAS_BACKFACE
	if (LOAD_DEPTH_TEXTURE_X(_CrestWaterBoundaryGeometryBackFaceTexture, i_positionCS.xy) == 0.0)
	{
		// We need zero mask for the meniscus. Otherwise, it will appear at geometry edges.
		discard;
	}
#endif // CREST_BOUNDARY_HAS_BACKFACE
}

#endif // CREST_WATER_BOUNDARY_INCLUDED
