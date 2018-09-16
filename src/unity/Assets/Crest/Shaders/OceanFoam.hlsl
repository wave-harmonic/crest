// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if _FOAM_ON

uniform sampler2D _FoamTexture;
uniform half _FoamScale;
uniform float4 _FoamTexture_TexelSize;
uniform half4 _FoamWhiteColor;
uniform half4 _FoamBubbleColor;
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

half WhiteFoamTexture(half i_foam, float2 i_worldXZUndisplaced)
{
	half ft = lerp(
		tex2D(_FoamTexture, (1.25*i_worldXZUndisplaced + _Time.y / 10.) / _FoamScale).r,
		tex2D(_FoamTexture, (3.00*i_worldXZUndisplaced - _Time.y / 10.) / _FoamScale).r,
		0.5);

	// black point fade
	i_foam = saturate(1. - i_foam);
	return smoothstep(i_foam, i_foam + _WaveFoamFeather, ft);
}

void ComputeFoam(half i_foam, float2 i_worldXZUndisplaced, float2 i_worldXZ, half3 i_n, float i_pixelZ, float i_sceneZ, half3 i_view, float3 i_lightDir, half i_shadow, out half3 o_bubbleCol, out half4 o_whiteFoamCol)
{
	half foamAmount = i_foam;

	// feather foam very close to shore
	foamAmount *= saturate((i_sceneZ - i_pixelZ) / _ShorelineFoamMinDepth);

	// Additive underwater foam - use same foam texture but add mip bias to blur for free
	float2 foamUVBubbles = (lerp(i_worldXZUndisplaced, i_worldXZ, 0.05) + 0.5 * _Time.y * _WindDirXZ) / _FoamScale + 0.125 * i_n.xz;
	half bubbleFoamTexValue = tex2Dlod(_FoamTexture, float4(.74 * foamUVBubbles - .05*i_view.xz / i_view.y, 0., 5.)).r;
	o_bubbleCol = (half3)bubbleFoamTexValue * _FoamBubbleColor.rgb * saturate(i_foam * _WaveFoamBubblesCoverage);

	// White foam on top, with black-point fading
	half whiteFoam = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced);

#if _FOAM3DLIGHTING_ON
	// Scale up delta by Z - keeps 3d look better at distance. better way to do this?
	float2 dd = float2(0.25 * i_pixelZ * _FoamTexture_TexelSize.x, 0.);
	half whiteFoam_x = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced + dd.xy);
	half whiteFoam_z = WhiteFoamTexture(foamAmount, i_worldXZUndisplaced + dd.yx);

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

#endif // _FOAM_ON
