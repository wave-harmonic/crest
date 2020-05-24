// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)



// We put all of this functionality in a centralised place to ensure that the different possible defines are easier
// to keep consistent.

// TODO(TRC):Now @optimisation pack opaque information in the underwater mask texture, only enable
// parts of this based on which underwater features we have enabled.
sampler2D _CrestOceanOccluderMaskTexture;
sampler2D _CrestOceanOccluderMaskDepthTexture;

void DiscardOceanSurfaceFromOccluderMask(float2 uv, float z)
{
	// TODO(TRC):Now @optimisation pack opaque information in the underwater mask texture, only enable
	// parts of this based on which underwater features we have enabled.
	float overrideMask = tex2D(_CrestOceanOccluderMaskTexture, uv).x;
	float overrideDepth = tex2D(_CrestOceanOccluderMaskDepthTexture, uv).x;
	if(
		(overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE) ||
		(overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE_FRONT && overrideDepth < z) ||
		(overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE_BACK && overrideDepth >= z)
	) {
		discard;
	}
}

void DiscardOceanMaskFromOccluderMask(float2 uv, float z)
{
	float overrideMask = tex2D(_CrestOceanOccluderMaskTexture, uv).x;
	float overrideDepth = tex2D(_CrestOceanOccluderMaskDepthTexture, uv).x;

	// TODO(TRC):Now @optimisation pack opaque information in the underwater mask texture, only enable
	// parts of this based on which underwater features we have enabled.
	if(overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE || (overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE_FRONT && overrideDepth < z))
	{
		discard;
	}
}

float PostProcessHandleOccluderMask(float2 uv, float oceanMask, float oceanDepth01)
{
	const float overrideMask = tex2D(_CrestOceanOccluderMaskTexture, uv).x;
	const float overrideDepth01 = tex2D(_CrestOceanOccluderMaskDepthTexture, uv).x;
	{
		const bool disableWaterBehindOverrideMask = (overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE_BACK && (oceanDepth01 < overrideDepth01));
		oceanDepth01 = disableWaterBehindOverrideMask ? overrideDepth01 : oceanDepth01;
	}

	// TODO(TRC):Now @optimisation pack opaque information in the underwater mask texture, only enable
	// parts of this based on which underwater features we have enabled.
	if(overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE || overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE_FRONT)
	{
		oceanMask = UNDERWATER_MASK_WATER_SURFACE_ABOVE;
	}
	else if(overrideMask == OVERRIDE_MASK_UNDERWATER_DISABLE_BACK)
	{
		// We want to disable underwater behind the override surface,
		// but therefore if we are already underwater, we need to ensure that we apply for to the appropriate pixels.
		oceanMask = UNDERWATER_MASK_WATER_SURFACE_BELOW;
	}
	return oceanMask;
}
