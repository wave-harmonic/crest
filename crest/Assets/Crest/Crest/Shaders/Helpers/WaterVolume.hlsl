// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_WATER_VOLUME_INCLUDED
#define CREST_WATER_VOLUME_INCLUDED

TEXTURE2D_X(_CrestWaterVolumeFrontFaceTexture);
TEXTURE2D_X(_CrestWaterVolumeBackFaceTexture);

#if CREST_WATER_VOLUME
void ApplyVolumeToOceanSurface(const float4 i_positionCS)
{
#if CREST_WATER_VOLUME_HAS_BACKFACE
	// Discard ocean after volume or when not on pixel.
	float rawBackFaceZ = LOAD_DEPTH_TEXTURE_X(_CrestWaterVolumeBackFaceTexture, i_positionCS.xy);
	if (rawBackFaceZ == 0.0 || rawBackFaceZ > i_positionCS.z)
	{
		discard;
	}
#endif // CREST_WATER_VOLUME_VOLUME

	// Discard ocean before volume.
	float rawFrontFaceZ = LOAD_DEPTH_TEXTURE_X(_CrestWaterVolumeFrontFaceTexture, i_positionCS.xy);
	if (rawFrontFaceZ > 0.0 && rawFrontFaceZ < i_positionCS.z)
	{
		discard;
	}

#if CREST_WATER_VOLUME_2D
	// Discard ocean when plane is not in view.
	if (rawFrontFaceZ == 0.0)
	{
		discard;
	}
#endif // CREST_WATER_VOLUME_2D
}
#endif

#if CREST_WATER_VOLUME
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
