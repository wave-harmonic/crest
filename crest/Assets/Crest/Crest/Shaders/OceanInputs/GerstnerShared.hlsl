// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// IMPORTANT - this mirrors the constant with the same name in ShapeGerstnerBatched.cs, both must be updated together!
#define BATCH_SIZE 32

#define PI 3.141593

// Caution - this exploded on vulkan due to a collision with 'CrestPerObject' cbuffer in OceanInput
CBUFFER_START(GerstnerUniforms)
half _Weight;
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
	const half depth = _LD_TexArray_SeaFloorDepth.Sample(LODData_linear_clamp_sampler, uv_slice).x;

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

	float factor = 4.0;
	float L = _CrestCascadeData[_LD_SliceIndex]._texelWidth * _CrestCascadeData[_LD_SliceIndex]._textureRes / factor;
	if( uv_slice.x > 1.0 / factor ) return result.xyzz;
	if( uv_slice.y > 1.0 / factor ) return result.xyzz;

	// gerstner computation is vectorized - processes 4 wave components at once
	for (uint vi = 0; vi < _NumWaveVecs; vi++)
	{
		// direction
		half4 Dx = _WaveDirX[vi];
		half4 Dz = _WaveDirZ[vi];

		half4 wt = 1.0;

		// Peferred wave direction
#if CREST_DIRECT_TOWARDS_POINT_INTERNAL
		wt *= max((1.0 + Dx * preferredDirX + Dz * preferredDirZ) / 2.0, 0.1);
#endif

		// wave number
		half4 k = _TwoPiOverWavelengths[vi];

		half4 kx = k * Dx;
		half4 kz = k * Dz;
		// http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.161.9102&rep=rep1&type=pdf eqn 19
		// kx = 2 pi n / L
		// kx / (2pi/L) = n
		float4 n = kx / (2.0 * 3.141592654 / L);
		float4 m = kz / (2.0 * 3.141592654 / L);
		n = round( n );
		m = round( m );
#if 1
		kx = 2.0 * 3.141592654 * n / L;
		kz = 2.0 * 3.141592654 * m / L;
#endif

		// spatial location
		float4 x = kx * worldPosXZ.x + kz * worldPosXZ.y;
		half4 angle = x + _Phases[vi];


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
