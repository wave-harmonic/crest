
// how many samples we want in one wave. trade quality for perf.
uniform float _TexelsPerWave;
uniform float _MaxWavelength;

bool SamplingIsAppropriate(float wavelengthInShape)
{
	const float cameraWidth = 2. * unity_OrthoParams.x;
	const float renderTargetRes = _ScreenParams.x;
	const float texSize = cameraWidth / renderTargetRes;
	const float minWavelength = texSize * _TexelsPerWave;

	const bool largeEnough = wavelengthInShape >= minWavelength;
	bool smallEnough = wavelengthInShape < 2.*minWavelength;

	// if this shape is the last (biggest) lod, then accept any remaining long wavelengths
	if (minWavelength*2.01 > _MaxWavelength)
		smallEnough = true;

	return largeEnough && smallEnough;
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
