// Crest Ocean System

// Copyright 2022 Wave Harmonic Ltd

#if _MASK_MASKED
#ifndef SHADERGRAPH_PREVIEW
TEXTURE2D_X(_CrestWaterVolumeFrontFaceTexture);
SAMPLER(sampler_CrestWaterVolumeFrontFaceTexture);
TEXTURE2D_X(_CrestWaterVolumeBackFaceTexture);
SAMPLER(sampler_CrestWaterVolumeBackFaceTexture);
TEXTURE2D_X(_CrestOceanMaskTexture);
SAMPLER(sampler_CrestOceanMaskTexture);
#endif
#endif

#if _MASK_FILL
#ifndef SHADERGRAPH_PREVIEW
TEXTURE2D_X(_FillTexture);
SAMPLER(sampler_FillTexture);
#endif
#endif

void CrestExamplesNodeMasked_float(const float4 i_screenPosition, out bool io_clipped)
{
	float2 positionNDC = i_screenPosition.xy / i_screenPosition.w;
	float deviceDepth = i_screenPosition.z / i_screenPosition.w;

	io_clipped = false;

#if _MASK_MASKED
#ifndef SHADERGRAPH_PREVIEW
	float rawFrontFaceZ = SAMPLE_TEXTURE2D_X(_CrestWaterVolumeFrontFaceTexture, sampler_CrestWaterVolumeFrontFaceTexture, positionNDC).r;
#endif
	if (rawFrontFaceZ > 0.0 && rawFrontFaceZ < deviceDepth)
	{
		io_clipped = true;
		return;
	}

#ifndef SHADERGRAPH_PREVIEW
	float rawBackFaceZ = SAMPLE_TEXTURE2D_X(_CrestWaterVolumeBackFaceTexture, sampler_CrestWaterVolumeBackFaceTexture, positionNDC).r;
#endif
	if (rawBackFaceZ > 0.0 && rawBackFaceZ > deviceDepth)
	{
		io_clipped = true;
		return;
	}

#ifndef SHADERGRAPH_PREVIEW
	float mask = SAMPLE_TEXTURE2D_X(_CrestOceanMaskTexture, sampler_CrestOceanMaskTexture, positionNDC).r;
#endif
	if (mask == 0.0)
	{
		io_clipped = true;
		return;
	}
#endif // _MASK_MASKED

#if _MASK_FILL
#ifndef SHADERGRAPH_PREVIEW
	if (SAMPLE_TEXTURE2D_X(_FillTexture, sampler_FillTexture, positionNDC).r == 0.0)
#endif
	{
		io_clipped = true;
		return;
	}
#endif // _MASK_FILL
}
