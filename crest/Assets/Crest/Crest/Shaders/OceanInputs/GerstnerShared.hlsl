// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatched.cs, both must be updated together!
#define BATCH_SIZE 32

#define PI 3.141593

// Caution - this exploded on vulkan due to a collision with 'CrestPerObject' cbuffer in OceanInput
CBUFFER_START(GerstnerUniforms)
half _Weight;
half _AttenuationInShallows;
uint _NumWaveVecs;

half4 _TwoPiOverWavelengths[BATCH_SIZE / 4];
half4 _Amplitudes[BATCH_SIZE / 4];
half4 _WaveDirX[BATCH_SIZE / 4];
half4 _WaveDirZ[BATCH_SIZE / 4];
half4 _Phases[BATCH_SIZE / 4];
half4 _ChopAmps[BATCH_SIZE / 4];

float4 _TargetPointData;
CBUFFER_END

half4 ComputeGerstner(float2 worldPosXZ, float3 uv_slice, half depth)
{
	float2 displacementNormalized = 0.0;

	// Preferred wave directions
#if CREST_DIRECT_TOWARDS_POINT_INTERNAL
	float2 offset = worldPosXZ - _TargetPointData.xy;
	float preferDist = length(offset);
	float preferWt = smoothstep(_TargetPointData.w, _TargetPointData.z, preferDist);
	half2 preferredDir = preferWt * offset / preferDist;
	half4 preferredDirX = preferredDir.x;
	half4 preferredDirZ = preferredDir.y;
#endif

	half3 result = (half3)0.0;

	// attenuate waves based on ocean depth. if depth is greater than 0.5*wavelength, water is considered Deep and wave is
	// unaffected. if depth is less than this, wave velocity decreases. waves will then bunch up and grow in amplitude and
	// eventually break. i model "Deep" water, but then simply ramp down waves in non-deep water with a linear multiplier.
	// http://hyperphysics.phy-astr.gsu.edu/hbase/Waves/watwav2.html
	// http://hyperphysics.phy-astr.gsu.edu/hbase/watwav.html#c1
	// optimisation - do this outside the loop below - take the median wavelength for depth weighting, intead of computing
	// per component. computing per component makes little difference to the end result
	half depth_wt = saturate(depth * _TwoPiOverWavelengths[_NumWaveVecs / 2].x / PI);
	half4 wt = _AttenuationInShallows * depth_wt + (1.0 - _AttenuationInShallows);

	// gerstner computation is vectorized - processes 4 wave components at once
	for (uint vi = 0; vi < _NumWaveVecs; vi++)
	{
		// direction
		half4 Dx = _WaveDirX[vi];
		half4 Dz = _WaveDirZ[vi];

		// Peferred wave direction
#if CREST_DIRECT_TOWARDS_POINT_INTERNAL
		wt *= max((1.0 + Dx * preferredDirX + Dz * preferredDirZ) / 2.0, 0.1);
#endif

		// wave number
		half4 k = _TwoPiOverWavelengths[vi];
		// spatial location
		half4 x = Dx * worldPosXZ.x + Dz * worldPosXZ.y;
		half4 angle = k * x + _Phases[vi];

		// dx and dz could be baked into _ChopAmps
		half4 disp = _ChopAmps[vi] * sin(angle);
		half4 resultx = disp * Dx;
		half4 resultz = disp * Dz;

		half4 resulty = _Amplitudes[vi] * cos(angle);

		// sum the vector results
		result.x += dot(resultx, wt);
		result.y += dot(resulty, wt);
		result.z += dot(resultz, wt);

		half4 sssFactor = min(1.0, _TwoPiOverWavelengths[vi]);
		displacementNormalized.x += dot(resultx * sssFactor, wt);
		displacementNormalized.y += dot(resultz * sssFactor, wt);
	}

	half sss = length(displacementNormalized);

	return _Weight * half4(result, sss);
}

half4 ComputeShorelineGerstner(float2 worldPosXZ, float3 uv_slice, half distance, half depth, half2 direction)
{
	float2 displacementNormalized = 0.0;
	half3 result = (half3)0.0;

	if(depth > 0.5)
	{
		float PIS = 3.141;

		float waveLength = 5.0;
		float period = 0.1;
		float amplitude = 0.12;

		float theta = 2 * PIS * (((sqrt(distance)*2.0)/(waveLength)) + (_Time/period));
		result.y = amplitude * cos(theta);
		result.xz = direction * sin(theta) * (1.0/(1.0+distance));
	}
	return _Weight * half4(result, 0.0);
}
