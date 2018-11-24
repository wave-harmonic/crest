// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if _PROCEDURALSKY_ON
uniform half3 _SkyBase, _SkyAwayFromSun, _SkyTowardsSun;
uniform half _SkyDirectionality;

half3 SkyProceduralDP(half3 refl, half3 lightDir)
{
	half dp = dot(refl, lightDir);

	if (dp > _SkyDirectionality)
	{
		dp = (dp - _SkyDirectionality) / (1. - _SkyDirectionality);
		return lerp(_SkyBase, _SkyTowardsSun, dp);
	}

	dp = (dp - -1.0) / (_SkyDirectionality - -1.0);
	return lerp(_SkyAwayFromSun, _SkyBase, dp);
}
#endif

#if _PLANARREFLECTIONS_ON
uniform sampler2D _ReflectionTex;

half3 PlanarReflection(half3 refl, half4 i_screenPos, half3 n_pixel)
{
	i_screenPos.xy += n_pixel.xz;
	return tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(i_screenPos)).xyz;
}
#endif // _PLANARREFLECTIONS_ON


uniform half _FresnelPower;

#if _COMPUTEDIRECTIONALLIGHT_ON
uniform half _DirectionalLightFallOff;
uniform half _DirectionalLightBoost;
#endif

#if !_PLANARREFLECTIONS_ON
uniform samplerCUBE _Skybox;
#endif

void ApplyReflectionSky(half3 view, half3 n_pixel, half3 lightDir, half shadow, half4 i_screenPos, inout half3 col)
{
	// Reflection
	half3 refl = reflect(-view, n_pixel);
	half3 skyColour;

#if _PLANARREFLECTIONS_ON
	skyColour = PlanarReflection(refl, i_screenPos, n_pixel);
#elif _PROCEDURALSKY_ON
	skyColour = SkyProceduralDP(refl, lightDir);
#else
	skyColour = texCUBE(_Skybox, refl).rgb;
#endif

	// Add primary light to boost it
#if _COMPUTEDIRECTIONALLIGHT_ON
	skyColour += pow(max(0., dot(refl, lightDir)), _DirectionalLightFallOff) * _DirectionalLightBoost * _LightColor0 * shadow;
#endif

	// Fresnel
	const float IOR_AIR = 1.0;
	const float IOR_WATER = 1.33;
	// reflectance at facing angle
	float R_0 = (IOR_AIR - IOR_WATER) / (IOR_AIR + IOR_WATER); R_0 *= R_0;
	// schlick's approximation
	float R_theta = R_0 + (1.0 - R_0) * pow(1.0 - max(dot(n_pixel, view), 0.), _FresnelPower);
	col = lerp(col, skyColour, R_theta);
}

// disabling for now as this is WIP
#if 0
void ApplyReflectionUnderwater(half3 view, half3 n_pixel, half3 lightDir, half shadow, half4 i_screenPos, inout half3 col)
{
	// Reflection
	half3 refl = reflect(-view, n_pixel);

	// TODO: calculate what will be reflected back from the deep
	half3 underwaterColor = half3(0, 0, .01);

	// Total Internal Reflection
	const float IOR_AIR = 1.0;
	const float IOR_WATER = 1.33;
	const float CRITICAL_ANGLE = asin(IOR_AIR/IOR_WATER);

	float angle = acos(dot(n_pixel, view));

	float transitionDelta = .1;
	// a hack to interpolate from refraction to TIR.
	float lerpFactor = pow(((angle + transitionDelta) - CRITICAL_ANGLE)/transitionDelta, 5);
	// TODO: look at http://habib.wikidot.com/projected-grid-ocean-shader-full-html-version#toc9
	if (angle > CRITICAL_ANGLE)
	{
		col = underwaterColor;
	}
	else if (angle > CRITICAL_ANGLE - transitionDelta)
	{
		col = lerp(col, underwaterColor, lerpFactor);
	}
}
#endif
