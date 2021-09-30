// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#ifndef CREST_OCEAN_FOAM_INCLUDED
#define CREST_OCEAN_FOAM_INCLUDED

#if _FOAM_ON

half WhiteFoamTexture
(
	half i_foam,
	float2 i_worldXZUndisplaced,
	half lodVal,
	in const CascadeParams cascadeData0,
	in const CascadeParams cascadeData1
)
{
	half ft = lerp(
		tex2D(_FoamTexture, (1.25*i_worldXZUndisplaced + _CrestTime / 10.) / (4.*cascadeData0._texelWidth*_FoamScale)).r,
		tex2D(_FoamTexture, (1.25*i_worldXZUndisplaced + _CrestTime / 10.) / (4.*cascadeData1._texelWidth*_FoamScale)).r,
		lodVal);

	// black point fade
	i_foam = saturate(1. - i_foam);
	return smoothstep(i_foam, i_foam + _WaveFoamFeather, ft);
}

half BubbleFoamTexture
(
	float2 i_worldXZ,
	float2 i_worldXZUndisplaced,
	half3 i_n,
	half3 i_view,
	half lodVal,
	in const CascadeParams cascadeData0,
	in const CascadeParams cascadeData1
)
{
	float2 windDir = float2(0.866, 0.5);
	float2 foamUVBubbles = (lerp(i_worldXZUndisplaced, i_worldXZ, 0.7) + 0.5 * _CrestTime * windDir) / _FoamScale + 0.125 * i_n.xz;
	float2 parallaxOffset = -_FoamBubbleParallax * i_view.xz / dot(i_n, i_view);
	half ft = lerp(
		tex2Dlod(_FoamTexture, float4((0.74 * foamUVBubbles + parallaxOffset) / (4.0*cascadeData0._texelWidth), 0., 3.)).r,
		tex2Dlod(_FoamTexture, float4((0.74 * foamUVBubbles + parallaxOffset) / (4.0*cascadeData1._texelWidth), 0., 3.)).r,
		lodVal);

	return ft;
}

void ComputeFoam
(
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
	half foamAmount = i_foam;

#if _TRANSPARENCY_ON
	// feather foam very close to shore
	foamAmount *= saturate((i_sceneZ - i_pixelZ) / _ShorelineFoamMinDepth);
#endif

	// Additive underwater foam - use same foam texture but add mip bias to blur for free
	half bubbleFoamTexValue = BubbleFoamTexture(i_worldXZ, i_worldXZUndisplaced, i_n, i_view, lodVal, cascadeData0, cascadeData1);
	o_bubbleCol = (half3)bubbleFoamTexValue * _FoamBubbleColor.rgb * saturate(i_foam * _WaveFoamBubblesCoverage) * AmbientLight();

	// White foam on top, with black-point fading
	half whiteFoam = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced, lodVal, cascadeData0, cascadeData1);

#if _FOAM3DLIGHTING_ON
	// Scale up delta by Z - keeps 3d look better at distance. better way to do this?
	float2 dd = float2(0.25 * i_pixelZ * _FoamTexture_TexelSize.x, 0.);
	half whiteFoam_x = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced + dd.xy, lodVal, cascadeData0, cascadeData1);
	half whiteFoam_z = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced + dd.yx, lodVal, cascadeData0, cascadeData1);

	// compute a foam normal
	half dfdx = whiteFoam_x - whiteFoam, dfdz = whiteFoam_z - whiteFoam;
	half3 fN = normalize(i_n + _WaveFoamNormalStrength * half3(-dfdx, 0., -dfdz));
	// do simple NdL and phong lighting
	half foamNdL = max(0., dot(fN, i_lightDir));
	o_whiteFoamCol.rgb = _FoamWhiteColor.rgb * (AmbientLight() + _WaveFoamLightScale * _LightColor0 * foamNdL * i_shadow);
	half3 refl = reflect(-i_view, fN);
	o_whiteFoamCol.rgb += pow(max(0., dot(refl, i_lightDir)), _WaveFoamSpecularFallOff) * _WaveFoamSpecularBoost * _LightColor0 * i_shadow;
#else // _FOAM3DLIGHTING_ON
	o_whiteFoamCol.rgb = _FoamWhiteColor.rgb * (AmbientLight() + _WaveFoamLightScale * _LightColor0 * i_shadow);
#endif // _FOAM3DLIGHTING_ON

	o_whiteFoamCol.a = _FoamWhiteColor.a * whiteFoam;
}

void ComputeFoamWithFlow
(
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

	ComputeFoam(i_foam, i_worldXZUndisplaced - (flow * sample1_offset), i_worldXZ, i_n, i_pixelZ, i_sceneZ, i_view, i_lightDir, i_shadow, lodVal, o_bubbleCol1, o_whiteFoamCol1, cascadeData0, cascadeData1);
	ComputeFoam(i_foam, i_worldXZUndisplaced - (flow * sample2_offset), i_worldXZ, i_n, i_pixelZ, i_sceneZ, i_view, i_lightDir, i_shadow, lodVal, o_bubbleCol2, o_whiteFoamCol2, cascadeData0, cascadeData1);
	o_bubbleCol = (sample1_weight * o_bubbleCol1) + (sample2_weight * o_bubbleCol2);
	o_whiteFoamCol = (sample1_weight * o_whiteFoamCol1) + (sample2_weight * o_whiteFoamCol2);
}

#endif // _FOAM_ON

#endif // CREST_OCEAN_FOAM_INCLUDED
