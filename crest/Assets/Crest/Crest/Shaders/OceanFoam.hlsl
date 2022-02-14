// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_OCEAN_FOAM_INCLUDED
#define CREST_OCEAN_FOAM_INCLUDED

#if _FOAM_ON

struct FoamWorldXZ
{
	float2 displaced1;
	float2 displaced0;
	float2 undisplaced0;
	float2 undisplaced1;
};

half WhiteFoamTexture
(
	in const WaveHarmonic::Crest::TiledTexture i_texture,
	half i_foam,
	in const FoamWorldXZ i_worldXZ,
	float2 offset,
	half lodVal,
	in const CascadeParams cascadeData0,
	in const CascadeParams cascadeData1
)
{
	half ft = lerp
	(
		i_texture.Sample((1.25 * i_worldXZ.undisplaced0 + offset + _CrestTime / 10.0) / (4.0 * cascadeData0._texelWidth * i_texture._scale)).r,
		i_texture.Sample((1.25 * i_worldXZ.undisplaced1 + offset + _CrestTime / 10.0) / (4.0 * cascadeData1._texelWidth * i_texture._scale)).r,
		lodVal
	);

	// black point fade
	i_foam = saturate(1. - i_foam);
	return smoothstep(i_foam, i_foam + _WaveFoamFeather, ft);
}

half BubbleFoamTexture
(
	in const WaveHarmonic::Crest::TiledTexture i_texture,
	in const FoamWorldXZ i_worldXZ,
	half3 i_n,
	half3 i_view,
	half lodVal,
	in const CascadeParams cascadeData0,
	in const CascadeParams cascadeData1
)
{
	float scale = 0.74;
#if CREST_FLOATING_ORIGIN
	// This value causes no pops.
	scale = 0.75;
#endif
	float2 windDir = float2(0.866, 0.5);
	float2 foamUVBubbles0 = (lerp(i_worldXZ.undisplaced0, i_worldXZ.displaced0, 0.7) + 0.5 * _CrestTime * windDir) / i_texture._scale + 0.125 * i_n.xz;
	float2 foamUVBubbles1 = (lerp(i_worldXZ.undisplaced1, i_worldXZ.displaced1, 0.7) + 0.5 * _CrestTime * windDir) / i_texture._scale + 0.125 * i_n.xz;
	float2 parallaxOffset = -_FoamBubbleParallax * i_view.xz / dot(i_n, i_view);
	half ft = lerp
	(
		i_texture.SampleLevel((scale * foamUVBubbles0 + parallaxOffset) / (4.0 * cascadeData0._texelWidth), 3.0).r,
		i_texture.SampleLevel((scale * foamUVBubbles1 + parallaxOffset) / (4.0 * cascadeData1._texelWidth), 3.0).r,
		lodVal
	);

	return ft;
}

void ComputeFoam
(
	in const WaveHarmonic::Crest::TiledTexture i_texture,
	half i_foam,
	float2 i_worldXZ,
	float2 i_worldXZUndisplaced,
	float2 i_flow,
	half3 i_n,
	float i_pixelZ,
	float i_sceneZ,
	half3 i_view,
	float3 i_lightDir,
	half i_shadow,
	half lodVal,
	out half3 o_bubbleCol,
	out half4 o_whiteFoamCol,
	in const CascadeParams cascadeData0,
	in const CascadeParams cascadeData1
)
{
	FoamWorldXZ worldXZ;
	worldXZ.displaced0   = i_worldXZ;
	worldXZ.displaced1   = i_worldXZ;
	worldXZ.undisplaced0 = i_worldXZUndisplaced;
	worldXZ.undisplaced1 = i_worldXZUndisplaced;

#if CREST_FLOATING_ORIGIN
	// Apply tiled floating origin offset. Only needed if:
	//  - _FoamScale is a non integer value
	//  - _FoamScale is over 48
	worldXZ.displaced0   -= i_texture.FloatingOriginOffset(cascadeData0);
	worldXZ.displaced1   -= i_texture.FloatingOriginOffset(cascadeData1);
	worldXZ.undisplaced0 -= i_texture.FloatingOriginOffset(cascadeData0);
	worldXZ.undisplaced1 -= i_texture.FloatingOriginOffset(cascadeData1);
#endif // CREST_FLOATING_ORIGIN

#if _FLOW_ON
	// Apply flow offset.
	worldXZ.undisplaced0 -= i_flow;
	worldXZ.undisplaced1 -= i_flow;
#endif // _FLOW_ON

	half foamAmount = i_foam;

#if _TRANSPARENCY_ON
	// feather foam very close to shore
	foamAmount *= saturate((i_sceneZ - i_pixelZ) / _ShorelineFoamMinDepth);
#endif

	// Additive underwater foam - use same foam texture but add mip bias to blur for free
	half bubbleFoamTexValue = BubbleFoamTexture(i_texture, worldXZ, i_n, i_view, lodVal, cascadeData0, cascadeData1);
	o_bubbleCol = (half3)bubbleFoamTexValue * _FoamBubbleColor.rgb * saturate(i_foam * _WaveFoamBubblesCoverage) * WaveHarmonic::Crest::AmbientLight();

	// White foam on top, with black-point fading
	half whiteFoam = WhiteFoamTexture(i_texture, foamAmount, worldXZ, 0.0, lodVal, cascadeData0, cascadeData1);

#if _FOAM3DLIGHTING_ON
	// Scale up delta by Z - keeps 3d look better at distance. better way to do this?
	float2 dd = float2(0.25 * i_pixelZ * i_texture._texel, 0.0);
	half whiteFoam_x = WhiteFoamTexture(i_texture, foamAmount, worldXZ, dd.xy, lodVal, cascadeData0, cascadeData1);
	half whiteFoam_z = WhiteFoamTexture(i_texture, foamAmount, worldXZ, dd.yx, lodVal, cascadeData0, cascadeData1);

	// compute a foam normal
	half dfdx = whiteFoam_x - whiteFoam, dfdz = whiteFoam_z - whiteFoam;
	half3 fN = normalize(i_n + _WaveFoamNormalStrength * half3(-dfdx, 0., -dfdz));
	// do simple NdL and phong lighting
	half foamNdL = max(0., dot(fN, i_lightDir));
	o_whiteFoamCol.rgb = _FoamWhiteColor.rgb * (WaveHarmonic::Crest::AmbientLight() + _WaveFoamLightScale * _LightColor0 * foamNdL * i_shadow);
	half3 refl = reflect(-i_view, fN);
	o_whiteFoamCol.rgb += pow(max(0., dot(refl, i_lightDir)), _WaveFoamSpecularFallOff) * _WaveFoamSpecularBoost * _LightColor0 * i_shadow;
#else // _FOAM3DLIGHTING_ON
	o_whiteFoamCol.rgb = _FoamWhiteColor.rgb * (WaveHarmonic::Crest::AmbientLight() + _WaveFoamLightScale * _LightColor0 * i_shadow);
#endif // _FOAM3DLIGHTING_ON

	o_whiteFoamCol.a = _FoamWhiteColor.a * whiteFoam;
}

void ComputeFoamWithFlow
(
	in const WaveHarmonic::Crest::TiledTexture i_texture,
	half2 flow,
	half i_foam,
	float2 i_worldXZUndisplaced,
	float2 i_worldXZ,
	half3 i_n,
	float i_pixelZ,
	float i_sceneZ,
	half3 i_view,
	float3 i_lightDir,
	half i_shadow,
	half lodVal,
	out half3 o_bubbleCol,
	out half4 o_whiteFoamCol,
	in const CascadeParams cascadeData0,
	in const CascadeParams cascadeData1
)
{
	const float half_period = 1;
	const float period = half_period * 2;
	float sample1_offset = fmod(_CrestTime, period);
	float sample1_weight = sample1_offset / half_period;
	if (sample1_weight > 1.0) sample1_weight = 2.0 - sample1_weight;
	float sample2_offset = fmod(_CrestTime + half_period, period);
	float sample2_weight = 1.0 - sample1_weight;

	// In order to prevent flow from distorting the UVs too much,
	// we fade between two samples of normal maps so that for each
	// sample the UVs can be reset
	half3 o_bubbleCol1 = half3(0, 0, 0);
	half4 o_whiteFoamCol1 = half4(0, 0, 0, 0);
	half3 o_bubbleCol2 = half3(0, 0, 0);
	half4 o_whiteFoamCol2 = half4(0, 0, 0, 0);

	ComputeFoam(i_texture, i_foam, i_worldXZ, i_worldXZUndisplaced, flow * sample1_offset, i_n, i_pixelZ, i_sceneZ, i_view, i_lightDir, i_shadow, lodVal, o_bubbleCol1, o_whiteFoamCol1, cascadeData0, cascadeData1);
	ComputeFoam(i_texture, i_foam, i_worldXZ, i_worldXZUndisplaced, flow * sample2_offset, i_n, i_pixelZ, i_sceneZ, i_view, i_lightDir, i_shadow, lodVal, o_bubbleCol2, o_whiteFoamCol2, cascadeData0, cascadeData1);
	o_bubbleCol = (sample1_weight * o_bubbleCol1) + (sample2_weight * o_bubbleCol2);
	o_whiteFoamCol = (sample1_weight * o_whiteFoamCol1) + (sample2_weight * o_whiteFoamCol2);
}

#endif // _FOAM_ON

#endif // CREST_OCEAN_FOAM_INCLUDED
