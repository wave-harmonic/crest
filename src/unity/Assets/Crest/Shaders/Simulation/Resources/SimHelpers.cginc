// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Helpers / shared code for simulation shaders

uniform float _SimDeltaTime;
uniform float _SimDeltaTimePrev;

// Compute current uv, and uv for the last frame to allow a sim to move around in the world but keep
// its data stationary, without smudged or blurred data.
void ComputeUVs(in float3 world, in float2 vertexXY, out float2 uv_lastframe, out float2 uv, out float invRes)
{
	// uv for target data - always simply 0-1 so take from geometry
	uv = vertexXY;
	uv.y = -uv.y;
	uv.xy = 0.5*uv.xy + 0.5;

	// uv for source data - use bound data to compute
	uv_lastframe = LD_0_WorldToUV(world.xz);

	invRes = 1. / _ScreenParams.x;
}
