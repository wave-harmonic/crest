
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

// assumes x >= 0
float tanh_clamped(float x)
{
	// tanh(10.) = 0.999999995878 which is 1 for floats.
	// leaving this unbounded gives me crazy instabilities in the sim which took a long time to track down.
	if (x > 10.) return 1.;
	return tanh(x);
}

float ComputeWaveSpeed( float wavelength, float depth )
{
	//// snap to nearest power of two
	float wavelength2 = wavelength;// exp2(floor(log2(wavelength)));
	float L = 1.5 * wavelength2; // take middle wavelength of band [ wavelength2, 2 * wavelength2 )

	// wave speed of deep sea ocean waves: https://en.wikipedia.org/wiki/Wind_wave
	// https://en.wikipedia.org/wiki/Dispersion_(water_waves)#Wave_propagation_and_dispersion
	float g = 9.81;
	float k = 2. * 3.141593 / wavelength;
	float h = max(depth,0.01);
	float cp = sqrt(abs(tanh_clamped(h*k)) * g / k);
	return cp;
}

// when driving waves into the sim, it seems the driving wave needs to be significantly faster than the
// wave speed specified in the simulation. see WaveDriverVel.xlsx.
float ComputeDriverWaveSpeed(float wavelength, float depth)
{
	float lod = floor(log2(wavelength));
	float lodbase = exp2(lod);
	float inlod = (wavelength - lodbase) / lodbase;

	float reg_start_start = 0.96;
	float reg_start_div = 80.0;
	float reg_start_exponent = 1.9;

	float reg_end_start = 1.03;
	float reg_end_div = 25.0;
	float reg_end_exponent = 1.92;

	float reg_start = reg_start_start + pow(reg_start_exponent, lod) / reg_start_div;
	float reg_end = reg_end_start + pow(reg_end_exponent, lod) / reg_end_div;

	float speed_mul = lerp(reg_start, reg_end, inlod);

	return speed_mul * ComputeWaveSpeed(wavelength, depth);
}
