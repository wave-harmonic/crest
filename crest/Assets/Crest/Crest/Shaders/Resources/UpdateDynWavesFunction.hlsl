// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// unifies traditional and compute shader implementation code

float ComputeWaveSpeed(float wavelength, float g)
{
	// wave speed of deep sea ocean waves: https://en.wikipedia.org/wiki/Wind_wave
	// https://en.wikipedia.org/wiki/Dispersion_(water_waves)#Wave_propagation_and_dispersion
	//float g = 9.81; float k = 2. * 3.141593 / wavelength; float cp = sqrt(g / k); return cp;
	const float one_over_2pi = 0.15915494;
	return sqrt(wavelength*g*one_over_2pi);
}

half2 UpdateDynWavesFunction(
	const float2 input_uv,
	const float2 worldPosXZ
)
{
	const float dt = _SimDeltaTime;
	const float dtp = _SimDeltaTimePrev;

	half2 velocity = tex2Dlod(_LD_Sampler_Flow_1, float4(input_uv, 0.0, 0.0)).xy;
	float2 uv_lastframe = LD_0_WorldToUV(worldPosXZ - (dt * velocity));
	float4 uv_lastframe4 = float4(uv_lastframe, 0.0, 0.0);

	half2 ft_ftm = tex2Dlod(_LD_Sampler_DynamicWaves_0, uv_lastframe4).xy;

	float ft = ft_ftm.x; // t - current value before update
	float ftm = ft_ftm.y; // t minus - previous value

	// compute axes of laplacian kernel - rotated every frame
	float e = _LD_Params_0.w; // assumes square RT
	float4 X = float4(_LaplacianAxisX, 0.0, 0.0);
	float4 Y = float4(-X.y, X.x, 0.0, 0.0);
	float fxm = tex2Dlod(_LD_Sampler_DynamicWaves_0, uv_lastframe4 - e*X).x; // x minus
	float fym = tex2Dlod(_LD_Sampler_DynamicWaves_0, uv_lastframe4 - e*Y).x; // y minus
	float fxp = tex2Dlod(_LD_Sampler_DynamicWaves_0, uv_lastframe4 + e*X).x; // x plus
	float fyp = tex2Dlod(_LD_Sampler_DynamicWaves_0, uv_lastframe4 + e*Y).x; // y plus

	// average wavelength for this scale
	float wavelength = 1.5 * _TexelsPerWave * _GridSize;
	// could make velocity depend on waves
	//float h = max(waterSignedDepth + ft, 0.);
	float c = ComputeWaveSpeed(wavelength, _Gravity);

	// wave propagation
	// velocity is implicit
	float v = dtp > MIN_DT ? (ft - ftm) / dtp : 0.0;

	// damping
	v *= 1.0 - min(1.0, _Damping * dt);

	// wave equation
	float ftp = ft + dt*v + dt*dt*c*c*(fxm + fxp + fym + fyp - 4.0*ft) / (_GridSize*_GridSize);

	// open boundary condition, from: http://hplgit.github.io/wavebc/doc/pub/._wavebc_cyborg002.html .
	// this actually doesn't work perfectly well - there is some minor reflections of high frequencies.
	// dudt + c*dudx = 0
	// (ftp - ft)   +   c*(ft-fxm) = 0.
	if (uv_lastframe.x + e >= 1.0) ftp = -dt*c*(ft - fxm) + ft;
	else if (uv_lastframe.x - e <= 0.0) ftp = dt * c*(fxp - ft) + ft;
	if (uv_lastframe.y + e >= 1.0) ftp = -dt*c*(ft - fym) + ft;
	else if (uv_lastframe.y - e <= 0.0) ftp = dt*c*(fyp - ft) + ft;

	// attenuate waves based on ocean depth. if depth is greater than 0.5*wavelength, water is considered Deep and wave is
	// unaffected. if depth is less than this, wave velocity decreases. waves will then bunch up and grow in amplitude and
	// eventually break. i model "Deep" water, but then simply ramp down waves in non-deep water with a linear multiplier.
	// http://hyperphysics.phy-astr.gsu.edu/hbase/Waves/watwav2.html
	// http://hyperphysics.phy-astr.gsu.edu/hbase/watwav.html#c1
	float waterSignedDepth = CREST_OCEAN_DEPTH_BASELINE - tex2Dlod(_LD_Sampler_SeaFloorDepth_1, float4(input_uv, 0, 0)).x;
	float depthMul = 1.0 - (1.0 - saturate(2.0 * waterSignedDepth / wavelength)) * dt * 2.0;
	ftp *= depthMul;
	ft *= depthMul;

	return half2(ftp, ft);
}
