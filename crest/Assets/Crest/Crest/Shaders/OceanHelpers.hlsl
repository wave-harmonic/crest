// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Ocean LOD data - data, samplers and functions associated with LODs

float ComputeLodAlpha(float3 i_worldPos, float i_meshScaleAlpha)
{
	// taxicab distance from ocean center drives LOD transitions
	float2 offsetFromCenter = abs(float2(i_worldPos.x - _OceanCenterPosWorld.x, i_worldPos.z - _OceanCenterPosWorld.z));
	float taxicab_norm = max(offsetFromCenter.x, offsetFromCenter.y);

	// interpolation factor to next lod (lower density / higher sampling period)
	const float scale = _CrestCascadeData[_LD_SliceIndex]._scale;
	float lodAlpha = taxicab_norm / scale - 1.0;

	// LOD alpha is remapped to ensure patches weld together properly. Patches can vary significantly in shape (with
	// strips added and removed), and this variance depends on the base vertex density of the mesh, as this defines the 
	// strip width.
	lodAlpha = max((lodAlpha - _CrestLodAlphaBlackPointFade) / _CrestLodAlphaBlackPointWhitePointFade, 0.);

	// blend out lod0 when viewpoint gains altitude
	lodAlpha = min(lodAlpha + i_meshScaleAlpha, 1.);

#if _DEBUGDISABLESMOOTHLOD_ON
	lodAlpha = 0.;
#endif

	return lodAlpha;
}

void SnapAndTransitionVertLayout(float i_meshScaleAlpha, const float gridSize, inout float3 io_worldPos, out float o_lodAlpha)
{
	// Grid includes small "epsilon" to solve numerical issues.
	// :OceanGridPrecisionErrors
	const float GRID_SIZE_2 = 2.000001 * gridSize, GRID_SIZE_4 = 4.0 * gridSize;

	// snap the verts to the grid
	// The snap size should be twice the original size to keep the shape of the eight triangles (otherwise the edge layout changes).
	io_worldPos.xz -= frac(unity_ObjectToWorld._m03_m23 / GRID_SIZE_2) * GRID_SIZE_2; // caution - sign of frac might change in non-hlsl shaders

	// compute lod transition alpha
	o_lodAlpha = ComputeLodAlpha(io_worldPos, i_meshScaleAlpha);

	// now smoothly transition vert layouts between lod levels - move interior verts inwards towards center
	float2 m = frac(io_worldPos.xz / GRID_SIZE_4); // this always returns positive
	float2 offset = m - 0.5;
	// check if vert is within one square from the center point which the verts move towards
	const float minRadius = 0.26; //0.26 is 0.25 plus a small "epsilon" - should solve numerical issues
	if (abs(offset.x) < minRadius) io_worldPos.x += offset.x * o_lodAlpha * GRID_SIZE_4;
	if (abs(offset.y) < minRadius) io_worldPos.z += offset.y * o_lodAlpha * GRID_SIZE_4;
}

// Clips using ocean surface clip data
void ApplyOceanClipSurface(in const float3 io_positionWS, in const float i_lodAlpha)
{
	// Sample shape textures - always lerp between 2 scales, so sample two textures
	// Sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
	const float2 worldXZ = io_positionWS.xz;
	float wt_smallerLod = (1. - i_lodAlpha) * _CrestCascadeData[_LD_SliceIndex]._weight;
	float wt_biggerLod = (1. - wt_smallerLod) * _CrestCascadeData[_LD_SliceIndex + 1]._weight;

	// Sample clip surface data
	half clipValue = 0.0;
	if (wt_smallerLod > 0.001)
	{
		const float3 uv = WorldToUV(worldXZ, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);
		SampleClip(_LD_TexArray_ClipSurface, uv, wt_smallerLod, clipValue);
	}
	if (wt_biggerLod > 0.001)
	{
		const uint si = _LD_SliceIndex + 1;
		const float3 uv = WorldToUV(worldXZ, _CrestCascadeData[si], si);
		SampleClip(_LD_TexArray_ClipSurface, uv, wt_biggerLod, clipValue);
	}

	// Add 0.5 bias for LOD blending and texel resolution correction. This will help to tighten and smooth clipped edges
	clip(-clipValue + 0.5);
}
