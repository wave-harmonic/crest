// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

#define MAX_UPDOWN_AMOUNT 0.8

float3 IntersectRayWithWaterSurface(const float3 pos, const float3 normal)
{
	// Find intersection of the near plane and the water surface at this vert using FPI. See here for info about
	// FPI http://www.huwbowles.com/fpi-gdc-2016/
	float2 sampleXZ = pos.xz;
	float3 disp;
	for (int i = 0; i < 6; i++)
	{
		// Sample displacement textures, add results to current world pos / normal / foam
		disp = float3(sampleXZ.x, _OceanCenterPosWorld.y, sampleXZ.y);
		SampleDisplacements(_LD_Sampler_AnimatedWaves_0, LD_0_WorldToUV(sampleXZ), 1.0, _LD_Params_0.w, _LD_Params_0.x, disp);
		const float3 nearestPointOnUp = pos + normal * dot(disp - pos, normal);
		const float2 error = disp.xz - nearestPointOnUp.xz;
		sampleXZ -= error;
	}

	return disp;
}

