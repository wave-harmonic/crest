// See header/license in SOURCE.txt file accompanying this shader.

// Trivial modifications made to the code to translate it to HLSL by Huw Bowles

#ifndef CREST_GPU_NOISE_INCLUDED
#define CREST_GPU_NOISE_INCLUDED

uint baseHash(uint3 p)
{
	p = 1103515245U * ((p.xyz >> 1U) ^ (p.yzx));
	uint h32 = 1103515245U * ((p.x^p.z) ^ (p.y >> 3U));
	return h32 ^ (h32 >> 16);
}

float hash13(uint3 x)
{
	uint n = baseHash(x);
	return float(n)*(1.0 / float(0xffffffffU));
}

float2 hash23(float3 x)
{
	uint n = baseHash(x);
	uint2 rz = uint2(n, n * 48271U); //see: http://random.mat.sbg.ac.at/results/karl/server/node4.html
	return float2(rz.xy & (uint2)0x7fffffffU) / float(0x7fffffff);
}

float3 hash33(uint3 x)
{
	uint n = baseHash(x);
	uint3 rz = uint3(n, n * 16807U, n * 48271U); //see: http://random.mat.sbg.ac.at/results/karl/server/node4.html
	return float3(rz & (uint3)0x7fffffffU) / float(0x7fffffff);
}

#endif // CREST_GPU_NOISE_INCLUDED
