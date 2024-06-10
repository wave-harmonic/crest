// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_WATER_VOLUME_INCLUDED
#define CREST_WATER_VOLUME_INCLUDED

TEXTURE2D_X(_CrestWaterVolumeFrontFaceTexture);
TEXTURE2D_X(_CrestWaterVolumeBackFaceTexture);

#if CREST_WATER_VOLUME
bool ApplyVolumeToOceanSurface(const float4 i_positionCS, out float o_rawFrontFaceZ, inout float io_rawSceneDepth)
{
	bool backface = false;
#if CREST_WATER_VOLUME_HAS_BACKFACE
	// Discard ocean after volume or when not on pixel.
	float rawBackFaceZ = LOAD_DEPTH_TEXTURE_X(_CrestWaterVolumeBackFaceTexture, i_positionCS.xy);
	if (rawBackFaceZ == 0.0 || rawBackFaceZ > i_positionCS.z)
	{
		discard;
	}
	else if (rawBackFaceZ > io_rawSceneDepth)
	{
		backface = true;
		io_rawSceneDepth = rawBackFaceZ;
	}
#endif // CREST_WATER_VOLUME_VOLUME

	// Discard ocean before volume.
	o_rawFrontFaceZ = LOAD_DEPTH_TEXTURE_X(_CrestWaterVolumeFrontFaceTexture, i_positionCS.xy);
	if (o_rawFrontFaceZ > 0.0 && o_rawFrontFaceZ < i_positionCS.z)
	{
		discard;
	}

#if CREST_WATER_VOLUME_2D
	// Discard ocean when plane is not in view.
	if (o_rawFrontFaceZ == 0.0)
	{
		discard;
	}
#endif // CREST_WATER_VOLUME_2D

	return backface;
}

#if CREST_WATER_VOLUME_HAS_BACKFACE
bool ApplyVolumeToOceanSurfaceRefractions
(
	const float2 i_refractedPositionNDC,
	const float i_sceneZRaw,
	const bool i_underwater,
	inout float io_refractedSceneDepthRaw,
	inout bool io_caustics
)
{
	bool backface = false;

	if (i_underwater)
	{
		return backface;
	}

	const float backFace = LOAD_DEPTH_TEXTURE_X(_CrestWaterVolumeBackFaceTexture, i_refractedPositionNDC * _ScreenSize.xy);

	// If back-face is closer.
	if (backFace > io_refractedSceneDepthRaw)
	{
		io_refractedSceneDepthRaw = backFace;
		io_caustics = false;
		backface = true;
	}

	// Sample has landed off the volume (UV wise). Cancel refraction otherwise distance
	// could be too large (refraction artifact).
	if (backFace == 0.0)
	{
		io_refractedSceneDepthRaw = i_sceneZRaw;
		backface = false;
	}

	return backface;
}
#endif // CREST_WATER_VOLUME_HAS_BACKFACE

void ApplyVolumeToOceanMask(const float4 i_positionCS)
{
	// Discard any pixels in front of the volume geometry otherwise the mask will be incorrect at eye level.
	float rawFrontFaceZ = LOAD_DEPTH_TEXTURE_X(_CrestWaterVolumeFrontFaceTexture, i_positionCS.xy);
	if (rawFrontFaceZ > 0.0 && rawFrontFaceZ < i_positionCS.z)
	{
		discard;
	}
}
#endif

#endif // CREST_WATER_VOLUME_INCLUDED
