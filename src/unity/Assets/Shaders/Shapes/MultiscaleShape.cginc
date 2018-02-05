
// how many samples we want in one wave. trade quality for perf.
uniform float _TexelsPerWave;
uniform float _MaxWavelength;
uniform float _ViewerAltitudeLevelAlpha;

bool SamplingIsAppropriate(float wavelengthInShape, out float wt)
{
	// weight for this shape to blend into target - 1 by default
	wt = 1.;

	const float cameraWidth = 2. * unity_OrthoParams.x;
	const float renderTargetRes = _ScreenParams.x;
	const float texSize = cameraWidth / renderTargetRes;
	const float minWavelength = texSize * _TexelsPerWave;

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

float ComputeWaveSpeed( float wavelength )
{
	// wave speed of deep sea ocean waves: https://en.wikipedia.org/wiki/Wind_wave
	// https://en.wikipedia.org/wiki/Dispersion_(water_waves)#Wave_propagation_and_dispersion
	float g = 9.81;
	float k = 2. * 3.141593 / wavelength;
	float cp = sqrt( g / k );
	return cp;
}
