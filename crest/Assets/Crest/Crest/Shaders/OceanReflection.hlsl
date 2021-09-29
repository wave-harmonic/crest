// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if _PROCEDURALSKY_ON
half3 SkyProceduralDP(in const half3 i_refl, in const half3 i_lightDir)
{
	half dp = dot(i_refl, i_lightDir);

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
void PlanarReflection(in const half4 i_screenPos, in const half3 i_n_pixel, inout half3 io_colour)
{
	half4 screenPos = i_screenPos;
	screenPos.xy += _PlanarReflectionNormalsStrength * i_n_pixel.xz;
	half4 refl = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(screenPos));
	// If more than four layers are used on terrain, they will appear black if HDR is enabled on the planar reflection
	// camera. Reflection alpha is probably a negative value.
	io_colour = lerp(io_colour, refl.rgb, _PlanarReflectionIntensity * saturate(refl.a));
}
#endif // _PLANARREFLECTIONS_ON

float CalculateFresnelReflectionCoefficient(float cosTheta)
{
	// Fresnel calculated using Schlick's approximation
	// See: http://www.cs.virginia.edu/~jdl/bib/appearance/analytic%20models/schlick94b.pdf
	// reflectance at facing angle
	float R_0 = (_RefractiveIndexOfAir - _RefractiveIndexOfWater) / (_RefractiveIndexOfAir + _RefractiveIndexOfWater); R_0 *= R_0;
	const float R_theta = R_0 + (1.0 - R_0) * pow(max(0.,1.0 - cosTheta), _FresnelPower);
	return R_theta;
}

void ApplyReflectionSky
(
	in const half3 i_view,
	in const half3 i_n_pixel,
	in const half3 i_lightDir,
	in const half i_shadow,
	in const half4 i_screenPos,
	in const float i_pixelZ,
	in const half i_weight,
	inout half3 io_col
)
{
	// Reflection
	half3 refl = reflect(-i_view, i_n_pixel);
	// Dont reflect below horizon
	refl.y = max(refl.y, 0.0);

	half3 skyColour;


#if _PROCEDURALSKY_ON
	// procedural sky cubemap
	skyColour = SkyProceduralDP(refl, i_lightDir);
#else

	// sample sky cubemap
#if _OVERRIDEREFLECTIONCUBEMAP_ON
	// User-provided cubemap
	half4 val = texCUBE(_ReflectionCubemapOverride, refl);
	skyColour = val.rgb;
#else
	Unity_GlossyEnvironmentData envData;
	envData.roughness = _Roughness;
	envData.reflUVW = refl;
	float3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
	#if UNITY_SPECCUBE_BLENDING
	float interpolator = unity_SpecCube0_BoxMin.w;
	// Branch optimization recommended by: https://catlikecoding.com/unity/tutorials/rendering/part-8/
	UNITY_BRANCH
	if (interpolator < 0.99999)
	{
		float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
		skyColour = lerp(probe1, probe0, interpolator);
	}
	else
	{
		skyColour = probe0;
	}
	#else
	skyColour = probe0;
	#endif
#endif

#endif

	// Override with anything in the planar reflections
#if _PLANARREFLECTIONS_ON
	PlanarReflection(i_screenPos, i_n_pixel, skyColour);
#endif

	// Add primary light
#if _COMPUTEDIRECTIONALLIGHT_ON
#if _DIRECTIONALLIGHTVARYROUGHNESS_ON
	half fallOffAlpha = saturate(i_pixelZ / _DirectionalLightFarDistance);
	fallOffAlpha = sqrt(fallOffAlpha);
	half fallOff = lerp(_DirectionalLightFallOff, _DirectionalLightFallOffFar, fallOffAlpha);
#else
	half fallOff = _DirectionalLightFallOff;
#endif

	skyColour += pow(max(0., dot(refl, i_lightDir)), fallOff) * _DirectionalLightBoost * _LightColor0 * i_shadow;
#endif

	// Fresnel
	float R_theta = CalculateFresnelReflectionCoefficient(max(dot(i_n_pixel, i_view), 0.0));
	io_col = lerp(io_col, skyColour, R_theta * _Specular * i_weight);
}

#if _UNDERWATER_ON
void ApplyReflectionUnderwater
(
	in const half3 i_view,
	in const half3 i_n_pixel,
	in const half3 i_lightDir,
	in const half i_shadow,
	in const half4 i_screenPos,
	half3 scatterCol,
	in const half i_weight,
	inout half3 io_col
)
{
	const half3 underwaterColor = scatterCol;
	// The the angle of outgoing light from water's surface
	// (whether refracted form outside or internally reflected)
	const float cosOutgoingAngle = max(dot(i_n_pixel, i_view), 0.);

	// calculate the amount of incident light from the outside world (io_col)
	{
		// have to calculate the incident angle of incoming light to water
		// surface based on how it would be refracted so as to hit the camera
		const float cosIncomingAngle = cos(asin(clamp( (_RefractiveIndexOfWater * sin(acos(cosOutgoingAngle))) / _RefractiveIndexOfAir, -1.0, 1.0) ));
		const float reflectionCoefficient = CalculateFresnelReflectionCoefficient(cosIncomingAngle) * i_weight;
		io_col *= (1.0 - reflectionCoefficient);
		io_col = max(io_col, 0.0);
	}

	// calculate the amount of light reflected from below the water
	{
		// angle of incident is angle of reflection
		const float cosIncomingAngle = cosOutgoingAngle;
		const float reflectionCoefficient = CalculateFresnelReflectionCoefficient(cosIncomingAngle) * i_weight;
		io_col += (underwaterColor * reflectionCoefficient);
	}
}
#endif
