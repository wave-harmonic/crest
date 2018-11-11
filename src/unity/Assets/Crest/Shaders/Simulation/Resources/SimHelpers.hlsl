// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Helpers / shared code for simulation shaders

// It seems that unity_DeltaTime.x is always >= 0.005! So Crest adds its own dts
uniform float _SimDeltaTime;
uniform float _SimDeltaTimePrev;

// Compute current uv, and uv for the last frame to allow a sim to move around in the world but keep
// its data stationary, without smudged or blurred data.
void ComputeUVs(in float2 worldXZ, in float2 vertexXY, out float2 uv_lastframe, out float2 uv)
{
	// uv for target data - always simply 0-1 so take from geometry
	uv = vertexXY;
	uv.y = -uv.y;
	uv.xy = 0.5*uv.xy + 0.5;

	// uv for source data - use bound data to compute
	uv_lastframe = LD_0_WorldToUV(worldXZ);
}
