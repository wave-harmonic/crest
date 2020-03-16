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

half4 ComputeGerstner(float2 worldPosXZ, float3 uv_slice)
{
	float2 displacementNormalized = 0.0;

	// sample ocean depth (this render target should 1:1 match depth texture, so UVs are trivial)
	const half depth = 100.0; // _LD_TexArray_SeaFloorDepth.Sample(LODData_linear_clamp_sampler, uv_slice).x;

	half3 result = (half3)0.0;

	// attenuate waves based on ocean depth. if depth is greater than 0.5*wavelength, water is considered Deep and wave is
	// unaffected. if depth is less than this, wave velocity decreases. waves will then bunch up and grow in amplitude and
	// eventually break. i model "Deep" water, but then simply ramp down waves in non-deep water with a linear multiplier.
	// http://hyperphysics.phy-astr.gsu.edu/hbase/Waves/watwav2.html
	// http://hyperphysics.phy-astr.gsu.edu/hbase/watwav.html#c1
	// optimisation - do this outside the loop below - take the median wavelength for depth weighting, intead of computing
	// per component. computing per component makes little difference to the end result
	half depth_wt = saturate(depth * _TwoPiOverWavelengths[_NumWaveVecs / 2].x / PI);
	half4 wt = 1.0; // _AttenuationInShallows * depth_wt + (1.0 - _AttenuationInShallows);

	// gerstner computation is vectorized - processes 4 wave components at once
	for (uint vi = 0; vi < 1; vi++)
	{
		// direction
		half4 Dx = 1.0; // _WaveDirX[vi];
		half4 Dz = 0.0; // _WaveDirZ[vi];

		// wave number
		half4 k = 0.5; // _TwoPiOverWavelengths[vi];
		// spatial location
		half4 x = Dx * worldPosXZ.x + Dz * worldPosXZ.y;
		half4 angle = k * x + vi; // +_Phases[vi];

		// dx and dz could be baked into _ChopAmps
		half4 disp = 0.125 * sin(angle);
		half4 resultx = disp * Dx;
		half4 resultz = disp * Dz;

		half4 resulty = 0.125 * cos(angle);

		// sum the vector results
		result.x += dot(resultx, wt);
		result.y += dot(resulty, wt);
		result.z += dot(resultz, wt);

		//half4 sssFactor = 1.0; // min(1.0, _TwoPiOverWavelengths[vi]);
		//displacementNormalized.x += dot(resultx * sssFactor, wt);
		//displacementNormalized.y += dot(resultz * sssFactor, wt);
	}

	//half sss = length(displacementNormalized);

	return _Weight * half4(result, 0.1);
}
