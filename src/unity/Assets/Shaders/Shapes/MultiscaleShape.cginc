
// how many samples we want in one wave. trade quality for perf.
uniform float _TexelsPerWave;

bool SamplingIsAppropriate(float wavelengthInShape)
{
	const float cameraWidth = 2. * unity_OrthoParams.x;
	const float renderTargetRes = _ScreenParams.x;
	const float texSize = cameraWidth / renderTargetRes;
	const float minWavelength = texSize * _TexelsPerWave;
	return wavelengthInShape > minWavelength && wavelengthInShape <= 2.*minWavelength;
}

float ComputeWaveSpeed( float wavelength )
{
	// snap to nearest power of two
	float wavelength2 = exp2(floor(log2(wavelength)));

	// wave speed of deep sea ocean waves: https://en.wikipedia.org/wiki/Wind_wave
	// C = sqrt( gL/2pi ), where L is wavelength
	float g = 9.81;
	float L = 1.5 * wavelength2; // take middle wavelength of band [ wavelength2, 2 * wavelength2 )
	float C = sqrt(g * L / 6.28318);

	return C;
}
