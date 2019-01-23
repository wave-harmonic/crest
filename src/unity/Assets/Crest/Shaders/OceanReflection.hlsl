// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#if _PROCEDURALSKY_ON
uniform half3 _SkyBase, _SkyAwayFromSun, _SkyTowardsSun;
uniform half _SkyDirectionality;

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
uniform sampler2D _ReflectionTex;

half3 PlanarReflection(in const half4 i_screenPos, in const half3 i_n_pixel)
{
	half4 screenPos = i_screenPos;
	screenPos.xy += i_n_pixel.xz;
	return tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(screenPos)).xyz;
}
#endif // _PLANARREFLECTIONS_ON


uniform half _FresnelPower;
uniform float  _RefractiveIndexOfAir;
uniform float  _RefractiveIndexOfWater;


#if _COMPUTEDIRECTIONALLIGHT_ON
uniform half _DirectionalLightFallOff;
uniform half _DirectionalLightBoost;
#endif

#if !_PLANARREFLECTIONS_ON
uniform samplerCUBE _Skybox;
#endif

float CalculateFresnelReflectionCoeffecient(float cosTheta)
{
	// Fresnel calculated using Schlick's approximation
	// See: http://www.cs.virginia.edu/~jdl/bib/appearance/analytic%20models/schlick94b.pdf
	// reflectance at facing angle
	float R_0 = (_RefractiveIndexOfAir - _RefractiveIndexOfWater) / (_RefractiveIndexOfAir + _RefractiveIndexOfWater); R_0 *= R_0;
	const float R_theta = R_0 + (1.0 - R_0) * pow(1.0 - cosTheta, _FresnelPower);
	return R_theta;
}

void ApplyReflectionSky(in const half3 i_view, in const half3 i_n_pixel, in const half3 i_lightDir, in const half i_shadow, in const half4 i_screenPos, inout half3 io_col)
{
	// Reflection
	half3 refl = reflect(-i_view, i_n_pixel);
	half3 skyColour;

#if _PLANARREFLECTIONS_ON
	skyColour = PlanarReflection(i_screenPos, i_n_pixel);
#elif _PROCEDURALSKY_ON
	skyColour = SkyProceduralDP(refl, i_lightDir);
#else
	skyColour = texCUBE(_Skybox, refl).rgb;
#endif

	// Add primary light to boost it
#if _COMPUTEDIRECTIONALLIGHT_ON
	skyColour += pow(max(0., dot(refl, i_lightDir)), _DirectionalLightFallOff) * _DirectionalLightBoost * _LightColor0 * i_shadow;
#endif

	// Fresnel
	float R_theta = CalculateFresnelReflectionCoeffecient(max(dot(i_n_pixel, i_view), 0.));
	io_col = lerp(io_col, skyColour, R_theta);
}

#if _UNDERWATER_ON
void ApplyReflectionUnderwater(in const half3 i_view, in const half3 i_n_pixel, in const half3 i_lightDir, in const half i_shadow, in const half4 i_screenPos, half3 scatterCol, inout half3 io_col)
{
	const half3 underwaterColor = scatterCol;
	// The the angle of outgoing light from water's surface
	// (whether refracted form outside or internally reflected)
	const float cosOutgoingAngle = max(dot(i_n_pixel, i_view), 0.);

	// calculate the amount of incident light from the outside world (io_col)
	{
		// have to calculate the incident angle of incoming light to water
		// surface based on how it would be refracted so as to hit the camera
		const float cosIncomingAngle = cos(asin((_RefractiveIndexOfWater * sin(acos(cosOutgoingAngle)))/_RefractiveIndexOfAir));
		const float reflectionCoefficient = CalculateFresnelReflectionCoeffecient(cosIncomingAngle);
		io_col = io_col * (1 - reflectionCoefficient);
		io_col = max(io_col, 0);
	}

	// calculate the amount of light reflected from below the water
	{
		// angle of incident is angle of reflection
		const float cosIncomingAngle = cosOutgoingAngle;
		const float reflectionCoefficient = CalculateFresnelReflectionCoeffecient(cosIncomingAngle);
		io_col = io_col + (underwaterColor * reflectionCoefficient);
	}
}
#endif
