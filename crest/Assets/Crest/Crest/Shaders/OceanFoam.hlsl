// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if _FOAM_ON

uniform sampler2D _FoamTexture;
uniform half _FoamScale;
uniform float4 _FoamTexture_TexelSize;
uniform half4 _FoamWhiteColor;
uniform half4 _FoamBubbleColor;
uniform half _FoamBubbleParallax;
uniform half _ShorelineFoamMinDepth;
uniform half _WaveFoamFeather;
uniform half _WaveFoamBubblesCoverage;
uniform half _WaveFoamNormalStrength;
uniform half _WaveFoamSpecularFallOff;
uniform half _WaveFoamSpecularBoost;
uniform half _WaveFoamLightScale;
uniform half2 _WindDirXZ;

half3 AmbientLight()
{
	return half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
}

half WhiteFoamTexture(half i_foam, float2 i_worldXZUndisplaced0, float2 i_worldXZUndisplaced1, float wt1, half lodVal)
{
	float t = _CrestTime / 10.;
	float2 pos0 = i_worldXZUndisplaced0 + t;
	float2 pos1 = i_worldXZUndisplaced1 + t;

	float scale0 = _LD_Params[_LD_SliceIndex].x*_FoamScale;
	float scale1 = _LD_Params[_LD_SliceIndex + 1].x*_FoamScale;

	half ft0 = lerp(
		tex2D(_FoamTexture, pos0 / scale0).r,
		tex2D(_FoamTexture, pos0 / scale1).r,
		lodVal);
	half ft1 = lerp(
		tex2D(_FoamTexture, pos1 / scale0).r,
		tex2D(_FoamTexture, pos1 / scale1).r,
		lodVal);
	half ft = lerp(ft0, ft1, wt1);

	// black point fade
	i_foam = saturate(1. - i_foam);
	return smoothstep(i_foam, i_foam + _WaveFoamFeather, ft);
}

half BubbleFoamTexture(float2 i_worldXZ, float2 i_worldXZUndisplaced, half3 i_n, half3 i_view, half lodVal)
{
	// TODO - implement flow here too
	float2 foamUVBubbles = (lerp(i_worldXZUndisplaced, i_worldXZ, 0.7) + 0.5 * _CrestTime * _WindDirXZ) / _FoamScale + 0.125 * i_n.xz;
	float2 parallaxOffset = -_FoamBubbleParallax * i_view.xz / dot(i_n, i_view);
	half ft = lerp(
		tex2Dlod(_FoamTexture, float4((0.74 * foamUVBubbles + parallaxOffset) / (4.0*_LD_Params[_LD_SliceIndex].x), 0., 3.)).r,
		tex2Dlod(_FoamTexture, float4((0.74 * foamUVBubbles + parallaxOffset) / (4.0*_LD_Params[_LD_SliceIndex + 1].x), 0., 3.)).r,
		lodVal);

	return ft;
}

void ComputeFoam(half i_foam, float2 i_worldXZUndisplaced0, float2 i_worldXZUndisplaced1, float wt1, float2 i_worldXZ, half3 i_n, float i_pixelZ, float i_sceneZ, half3 i_view, float3 i_lightDir, half i_shadow, half lodVal, out half3 o_bubbleCol, out half4 o_whiteFoamCol)
{
	half foamAmount = i_foam;

	// feather foam very close to shore
	foamAmount *= saturate((i_sceneZ - i_pixelZ) / _ShorelineFoamMinDepth);

	// Additive underwater foam - use same foam texture but add mip bias to blur for free
	// TODO - knocked out here for laziness reasons
	//half bubbleFoamTexValue = BubbleFoamTexture(i_worldXZ, i_worldXZUndisplaced, i_n, i_view, lodVal);
	o_bubbleCol = 0.;// (half3)bubbleFoamTexValue * _FoamBubbleColor.rgb * saturate(i_foam * _WaveFoamBubblesCoverage) * AmbientLight();

	// White foam on top, with black-point fading
	half whiteFoam = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced0, i_worldXZUndisplaced1, wt1, lodVal);

#if _FOAM3DLIGHTING_ON
	// Scale up delta by Z - keeps 3d look better at distance. better way to do this?
	float2 dd = float2(0.25 * i_pixelZ * _FoamTexture_TexelSize.x, 0.);
	half whiteFoam_x = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced0 + dd.xy, i_worldXZUndisplaced1 + dd.xy, wt1, lodVal);
	half whiteFoam_z = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced0 + dd.yx, i_worldXZUndisplaced1 + dd.yx, wt1, lodVal);

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

void ComputeFoamWithFlow(half2 flow, half i_foam, float2 i_worldXZUndisplaced, float2 i_worldXZ, half3 i_n, float i_pixelZ, float i_sceneZ, half3 i_view, float3 i_lightDir, half i_shadow, half lodVal, out half3 o_bubbleCol, out half4 o_whiteFoamCol)
{
	float t = _CrestTime;

	const float half_period = 1;
	const float period = half_period * 2;
	float sample1_offset = fmod(t, period);
	float sample1_weight = sample1_offset / half_period;
	if (sample1_weight > 1.0) sample1_weight = 2.0 - sample1_weight;
	float sample2_offset = fmod(t + half_period, period);
	float sample2_weight = 1.0 - sample1_weight;

	ComputeFoam(i_foam, i_worldXZUndisplaced - (flow * sample1_offset), i_worldXZUndisplaced - (flow * sample2_offset), sample2_weight, i_worldXZ, i_n, i_pixelZ, i_sceneZ, i_view, i_lightDir, i_shadow, lodVal, o_bubbleCol, o_whiteFoamCol);
}

#endif // _FOAM_ON
