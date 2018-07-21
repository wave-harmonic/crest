// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Helpers / shared code for simulation shaders

uniform float3 _CameraPositionDelta;
uniform float _SimDeltaTime;

// Compute current uv, and a u for the last frame to allow a sim to move around in the world but keep
// its data stationary, without smudged or blurred data.
void ComputeUVs(in float2 vertexXY, out float2 uv_lastframe, out float2 uv, out float invRes)
{
	// compute uncompensated uv
	uv = vertexXY;
	uv.y = -uv.y;
	uv.xy = 0.5*uv.xy + 0.5;

	// compensate for camera motion - adjust lookup uv to get texel from last frame sim
	invRes = 1. / _ScreenParams.x;
	const float texelSize = 2. * unity_OrthoParams.x * invRes; // assumes square RT
	uv_lastframe = uv + invRes * _CameraPositionDelta.xz / texelSize;
}
