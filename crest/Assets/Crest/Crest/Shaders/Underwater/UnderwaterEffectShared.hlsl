// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_UNDERWATER_EFFECT_SHARED_INCLUDED
#define CREST_UNDERWATER_EFFECT_SHARED_INCLUDED

half3 _AmbientLighting;
half _DataSliceOffset;
float2 _HorizonNormal;

float4 DebugRenderOceanMask(const bool isOceanSurface, const bool isUnderwater, const float mask, const float3 sceneColour)
{
	if (isOceanSurface)
	{
		return float4(sceneColour * float3(mask == UNDERWATER_MASK_ABOVE_SURFACE, mask == UNDERWATER_MASK_BELOW_SURFACE, 0.0), 1.0);
	}
	else
	{
		return float4(sceneColour * float3(isUnderwater * 0.5, (1.0 - isUnderwater) * 0.5, 1.0), 1.0);
	}
}

float4 DebugRenderStencil(float3 sceneColour)
{
	float3 stencil = 1.0;
#if CREST_WATER_VOLUME_FRONT_FACE
	stencil = float3(1.0, 0.0, 0.0);
#elif CREST_WATER_VOLUME_BACK_FACE
	stencil = float3(0.0, 1.0, 0.0);
#elif CREST_WATER_VOLUME_FULL_SCREEN
	stencil = float3(0.0, 0.0, 1.0);
#endif
	return float4(sceneColour * stencil, 1.0);
}

float MeniscusSampleOceanMask(const float mask, const int2 positionSS, const float2 offset, const float magnitude, const float scale)
{
	float2 uv = positionSS + offset * magnitude
#if CREST_WATER_VOLUME
	* scale
#endif
	;

	float newMask = LOAD_TEXTURE2D_X(_CrestOceanMaskTexture, uv).r;
#if CREST_WATER_VOLUME
	// No mask means no underwater effect so ignore the value.
	return (newMask == UNDERWATER_MASK_NONE ? mask : newMask);
#endif
	return newMask;
}

half ComputeMeniscusWeight(const int2 positionSS, const float mask, const float2 horizonNormal, const float meniscusDepth)
{
	float weight = 1.0;
#if CREST_MENISCUS
#if !_FULL_SCREEN_EFFECT
	// Render meniscus by checking the mask along the horizon normal which is flipped using the surface normal from
	// mask. Adding the mask value will flip the UV when mask is below surface.
	float2 offset = (float2)-mask * horizonNormal;
	float multiplier = 0.9;

#if CREST_WATER_VOLUME
	// The meniscus at the boundary can be at a distance. We need to scale the offset as 1 pixel at a distance is much
	// larger than 1 pixel up close.
	const float scale = 1.0 - saturate(meniscusDepth / MENISCUS_MAXIMUM_DISTANCE);

	// Exit early.
	if (scale == 0.0)
	{
		return 1.0;
	}
#else
	// Dummy value.
	const float scale = 0.0;
#endif

	// Sample three pixels along the normal. If the sample is different than the current mask, apply meniscus.
	// Offset must be added to positionSS as floats.
	weight *= (MeniscusSampleOceanMask(mask, positionSS, offset, 1.0, scale) != mask) ? multiplier : 1.0;
	weight *= (MeniscusSampleOceanMask(mask, positionSS, offset, 2.0, scale) != mask) ? multiplier : 1.0;
	weight *= (MeniscusSampleOceanMask(mask, positionSS, offset, 3.0, scale) != mask) ? multiplier : 1.0;
#endif // _FULL_SCREEN_EFFECT
#endif // CREST_MENISCUS
	return weight;
}

void GetOceanSurfaceAndUnderwaterData
(
	const float4 positionCS,
	const int2 positionSS,
	const float rawOceanDepth,
	const float mask,
	inout float rawDepth,
	inout bool isOceanSurface,
	inout bool isUnderwater,
	inout float sceneZ,
	const float oceanDepthTolerance
)
{
	isOceanSurface = false;
	isUnderwater = mask == UNDERWATER_MASK_BELOW_SURFACE;

#if defined(CREST_WATER_VOLUME_HAS_BACKFACE) || defined(CREST_WATER_VOLUME_BACK_FACE)
	const float rawGeometryDepth =
#if CREST_WATER_VOLUME_HAS_BACKFACE
	// 3D has a back face texture for the depth.
	LOAD_DEPTH_TEXTURE_X(_CrestWaterVolumeBackFaceTexture, positionSS);
#else
	// Volume is rendered using the back face so that is the depth.
	positionCS.z;
#endif // CREST_WATER_VOLUME_HAS_BACKFACE
	;

	// Use backface depth if closest.
	if (rawDepth < rawGeometryDepth && rawOceanDepth < rawGeometryDepth)
	{
		// Cancels out caustics.
		isOceanSurface = true;
		rawDepth = rawGeometryDepth;
		// No need to multi-sample.
		sceneZ = CrestLinearEyeDepth(rawDepth);
		return;
	}
#endif // CREST_WATER_VOLUME

	// Merge ocean depth with scene depth.
	if (rawDepth < rawOceanDepth + oceanDepthTolerance)
	{
		isOceanSurface = true;
		rawDepth = rawOceanDepth;
		sceneZ = CrestLinearEyeDepth(CREST_MULTILOAD_DEPTH(_CrestOceanMaskDepthTexture, positionSS, rawDepth));
	}
	else
	{
#if CREST_WATER_VOLUME_HAS_BACKFACE
		// If multi-loaded, the deepest depth could be behind the back face. Need a combined depth. But would also need
		// to redesign the passes to not render caustics on the back face.
		sceneZ = CrestLinearEyeDepth(rawDepth);
#else
		// For small water volumes this will have the opposite effect.
		sceneZ = CrestLinearEyeDepth(CREST_MULTILOAD_SCENE_DEPTH(positionSS, rawDepth));
#endif
	}
}

#ifdef CREST_OCEAN_EMISSION_INCLUDED
half3 ApplyUnderwaterEffect
(
	const int2 i_positionSS,
	const float3 scenePos,
	half3 sceneColour,
	const half3 lightCol,
	const float3 lightDir,
	const float rawDepth,
	const float sceneZ,
	const float fogDistance,
	const half3 view,
	const bool isOceanSurface
)
{
	half3 scatterCol = 0.0;
	int sliceIndex = clamp(_DataSliceOffset, 0, _SliceCount - 2);
	{
		// Offset slice so that we dont get high freq detail. But never use last lod as this has crossfading.
		const float3 uv_slice = WorldToUV(_WorldSpaceCameraPos.xz, _CrestCascadeData[sliceIndex], sliceIndex);

		half shadow = 1.0;
#if _SHADOWS_ON
		{
			// Camera should be at center of LOD system so no need for blending (alpha, weights, etc). This might not be
			// the case if there is large horizontal displacement, but the _DataSliceOffset should help by setting a
			// large enough slice as minimum.
			shadow = _LD_TexArray_Shadow.SampleLevel(LODData_linear_clamp_sampler, uv_slice, 0.0).x;
			shadow = saturate(1.0 - shadow);
		}
#endif // _SHADOWS_ON

		half seaFloorDepth = CREST_OCEAN_DEPTH_BASELINE;
#if _SUBSURFACESHALLOWCOLOUR_ON
		{
			// compute scatter colour from cam pos. two scenarios this can be called:
			// 1. rendering ocean surface from bottom, in which case the surface may be some distance away. use the scatter
			//    colour at the camera, not at the surface, to make sure its consistent.
			// 2. for the underwater skirt geometry, we don't have the lod data sampled from the verts with lod transitions etc,
			//    so just approximate by sampling at the camera position.
			// this used to sample LOD1 but that doesnt work in last LOD, the data will be missing.
			SampleSeaDepth(_LD_TexArray_SeaFloorDepth, uv_slice, 1.0, seaFloorDepth);
		}
#endif // _SUBSURFACESHALLOWCOLOUR_ON

		{
			scatterCol = ScatterColour
			(
				seaFloorDepth,
				shadow,
				1.0, // SSS is not used for underwater yet. Calculated in SampleDisplacementsNormals which is costly.
				view,
				_AmbientLighting,
				lightDir,
				lightCol,
				true
			);
		}
	}

#if _CAUSTICS_ON
	if (rawDepth != 0.0 && !isOceanSurface)
	{
		ApplyCaustics
		(
			_CausticsTiledTexture,
			_CausticsDistortionTiledTexture,
			i_positionSS,
			scenePos,
			lightDir,
			sceneZ,
			true,
			sceneColour,
			_CrestCascadeData[sliceIndex],
			_CrestCascadeData[sliceIndex + 1]
		);
	}
#endif // _CAUSTICS_ON

	return lerp(sceneColour, scatterCol, saturate(1.0 - exp(-_DepthFogDensity.xyz * fogDistance)));
}
#endif // CREST_OCEAN_EMISSION_INCLUDED

void ApplyWaterVolumeToUnderwaterFogAndMeniscus(float4 positionCS, inout float fogDistance, inout float meniscusDepth)
{
#if CREST_WATER_VOLUME_FRONT_FACE
	float depth = CrestLinearEyeDepth(positionCS.z);
	// Meniscus is rendered at the boundary so use the geometry z.
	meniscusDepth = depth;
	fogDistance -= depth;
#endif
}

#endif // CREST_UNDERWATER_EFFECT_SHARED_INCLUDED
