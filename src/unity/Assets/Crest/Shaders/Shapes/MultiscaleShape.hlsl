// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// how many samples we want in one wave. trade quality for perf.
uniform float _TexelsPerWave;
uniform float _MaxWavelength;
uniform float _ViewerAltitudeLevelAlpha;
uniform float _GridSize;

// assumes orthographic camera. uses camera dimensions, target resolution, and texels-per-wave quality setting
// to give the min supported wavelength for the current render.
float MinWavelengthForCurrentOrthoCamera()
{
	return _GridSize * _TexelsPerWave;
}

bool SamplingIsAppropriate(float wavelengthInShape, out float wt)
{
	// weight for this shape to blend into target - 1 by default
	wt = 1.;

	const float minWavelength = MinWavelengthForCurrentOrthoCamera();

	const bool largeEnough = wavelengthInShape >= minWavelength;
	if (!largeEnough) return false;

	const bool smallEnough = wavelengthInShape < 2.*minWavelength;
	if (smallEnough) return true;

	const bool shapeTooBigForAllLods = wavelengthInShape > _MaxWavelength;
	if (!shapeTooBigForAllLods) return false;

	// wavelengths that are too big for the LOD hierarchy we still accumulated into the last LODs, because losing them
	// changes the shape dramatically (unlike wavelengths that are too small for LOD0, as these do not make a big difference to overall shape).

	// however simply adding large wavelengths into the biggest LOD results in a pop when camera changes altitude, when the accumulated
	// shape suddenly moves from LODx to LODy

	// to solve this, we blend large wavelengths across the last two LODs. this means they have to be evaluated twice, but the result is smooth.

	const bool notRenderingIntoLast2Lods = minWavelength * 4.01 < _MaxWavelength;
	if (notRenderingIntoLast2Lods) return false;

	const bool renderingIntoLastLod = minWavelength * 2.01 > _MaxWavelength;
	wt = renderingIntoLastLod ? _ViewerAltitudeLevelAlpha : 1. - _ViewerAltitudeLevelAlpha;

	return true;
}

// this is similar to the above code but broken out shapes that are known to be assigned to the correct LODS before render, as these do not
// need the full set of checks. a current example is gerstner waves which are assigned a layer based on their wavelength in ShapeGerstner.cs.
// large wavelengths that do not fit in the lod chain naturally are rendered into the largest lods - this is inefficient but ensures their
// (important) contribution to the shape is always present. to 100% avoid pops, they are shifted smoothly between the last two lods so there
// is not pop when the lod chain scale changes due to sampling changes when a set of waves suddenly moves from one sampling resolution to another.
float ComputeSortedShapeWeight(float wavelengthInShape, float minWavelength)
{
	const bool renderingIntoLastTwoLods = minWavelength * 4.01 > _MaxWavelength;
	if (!renderingIntoLastTwoLods)
	{
		// no special weighting needed for any lods except the last 2
		return 1.;
	}

	const bool renderingIntoLastLod = minWavelength * 2.01 > _MaxWavelength;
	if (renderingIntoLastLod)
	{
		// example: fade out the last lod as viewer drops in altitude, so there is no pop when the lod chain shifts in scale
		return _ViewerAltitudeLevelAlpha;
	}

	// rendering to second-to-last lod. nothing required unless we are dealing with large wavelengths, which we want to transition into
	// this second-to-last lod when the viewer drops in altitude, ready for a seemless transition when the lod chain shifts in scale
	return wavelengthInShape < 2.*minWavelength ? 1. : 1. - _ViewerAltitudeLevelAlpha;
}

float ComputeWaveSpeed( float wavelength, float g )
{
	// wave speed of deep sea ocean waves: https://en.wikipedia.org/wiki/Wind_wave
	// https://en.wikipedia.org/wiki/Dispersion_(water_waves)#Wave_propagation_and_dispersion
	//float g = 9.81; float k = 2. * 3.141593 / wavelength; float cp = sqrt(g / k); return cp;
	const float one_over_2pi = 0.15915494;
	return sqrt(wavelength*g*one_over_2pi);
}
