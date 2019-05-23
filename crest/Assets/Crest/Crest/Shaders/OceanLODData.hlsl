// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Ocean LOD data - data, samplers and functions associated with LODs

#define SLICE_COUNT 16

// Samplers and data associated with a LOD.
// _LD_Params: float4(world texel size, texture resolution, shape weight multiplier, 1 / texture resolution)
#define LOD_DATA(FRAMENUM) \
	Texture2DArray _LD_TexArray_AnimatedWaves_##FRAMENUM; \
	Texture2DArray _LD_TexArray_SeaFloorDepth_##FRAMENUM; \
	Texture2DArray _LD_TexArray_Foam_##FRAMENUM; \
	Texture2DArray _LD_TexArray_Flow_##FRAMENUM; \
	Texture2DArray _LD_TexArray_DynamicWaves_##FRAMENUM; \
	Texture2DArray _LD_TexArray_Shadow_##FRAMENUM; \
	uniform float4 _LD_Params_##FRAMENUM[SLICE_COUNT]; \
	uniform float3 _LD_Pos_Scale_##FRAMENUM[SLICE_COUNT];

uniform float _LD_SLICE_Index_ThisLod;

SamplerState LODData_linear_clamp_sampler;

// Create two sets of LOD data, which have overloaded meaning depending on use:
// * simulations (persistent lod data) read last frame's data from slot 0, and any current frame data from slot 1
// * any other use that does not fall into the previous categories can use either slot and generally use slot 0
LOD_DATA( PrevFrame )
LOD_DATA( ThisFrame )

// Bias ocean floor depth so that default (0) values in texture are not interpreted as shallow and generating foam everywhere
#define CREST_OCEAN_DEPTH_BASELINE -1000.0

float3 ADD_SLICE_THIS_LOD_TO_UV(in float2 i_uv)
{
	return float3(i_uv, _LD_SLICE_Index_ThisLod);
}

float3 ADD_SLICE_NEXT_LOD_TO_UV(in float2 i_uv)
{
	return float3(i_uv, _LD_SLICE_Index_ThisLod + 1);
}

// TODO: Temp wrapper function to help speed port to MRT and GS along
float4 _LD_Params_ThisLod()
{
	return _LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod];
}

float4 _LD_Params_NextLod()
{
	return _LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod + 1];
}

// Conversions for world space from/to UV space
float2 LD_WorldToUV(in float2 i_samplePos, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return (i_samplePos - i_centerPos) / (i_texelSize * i_res) + 0.5;
}

float3 WorldToUV_ThisLod(in float2 i_samplePos) {
	const float2 result = LD_WorldToUV(
		i_samplePos,
		_LD_Pos_Scale_ThisFrame[_LD_SLICE_Index_ThisLod].xy,
		_LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod].y,
		_LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod].x
	);
	return ADD_SLICE_THIS_LOD_TO_UV(result);
}

float3 WorldToUV_NextLod(in float2 i_samplePos) {
	const uint _LD_SLICE_Index_NextLod = _LD_SLICE_Index_ThisLod + 1;
	const float2 result = LD_WorldToUV(
		i_samplePos, _LD_Pos_Scale_ThisFrame[_LD_SLICE_Index_NextLod].xy,
		_LD_Params_ThisFrame[_LD_SLICE_Index_NextLod].y,
		_LD_Params_ThisFrame[_LD_SLICE_Index_NextLod].x
	);
	return ADD_SLICE_NEXT_LOD_TO_UV(result);
}

float3 WorldToUV_ThisFrame(in float2 i_samplePos) {
	const float2 result = LD_WorldToUV(
		i_samplePos,
		_LD_Pos_Scale_ThisFrame[_LD_SLICE_Index_ThisLod].xy,
		_LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod].y,
		_LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod].x
	);
	return ADD_SLICE_THIS_LOD_TO_UV(result);
}

float3 WorldToUV_PrevFrame(in float2 i_samplePos) {
	const float2 result = LD_WorldToUV(
		i_samplePos,
		_LD_Pos_Scale_PrevFrame[_LD_SLICE_Index_ThisLod].xy,
		_LD_Params_PrevFrame[_LD_SLICE_Index_ThisLod].y,
		_LD_Params_PrevFrame[_LD_SLICE_Index_ThisLod].x
	);
	return ADD_SLICE_THIS_LOD_TO_UV(result);
}


float2 LD_UVToWorld(in float2 i_uv, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return i_texelSize * i_res * (i_uv - 0.5) + i_centerPos;
}

float2 UVToWorld_ThisFrame(in float2 i_uv) { return LD_UVToWorld(i_uv, _LD_Pos_Scale_ThisFrame[_LD_SLICE_Index_ThisLod].xy, _LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod].y, _LD_Params_ThisFrame[_LD_SLICE_Index_ThisLod].x); }

//  UNITY_DECLARE_TEX2DARRAY(i_dispSampler);

// Sampling functions
void SampleDisplacements(in Texture2DArray i_dispSampler, in float3 i_uv_slice, in float i_wt, inout float3 io_worldPos)
{
	const half3 disp = i_dispSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, float2(0, 0)).xyz;
	io_worldPos += i_wt * disp;
}

void SampleDisplacementsNormals(in Texture2DArray i_dispSampler, in float3 i_uv_slice, in float i_wt, in float i_invRes, in float i_texelSize, inout float3 io_worldPos, inout half2 io_nxz)
{
	const half3 disp = i_dispSampler.Sample(LODData_linear_clamp_sampler, i_uv_slice).xyz;
	io_worldPos += i_wt * disp;

	float3 n; {
		float3 dd = float3(i_invRes, 0.0, i_texelSize);
		half3 disp_x = dd.zyy + i_dispSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice + float3(dd.xy, 0), dd.yy).xyz;
		half3 disp_z = dd.yyz + i_dispSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice + float3(dd.yx, 0), dd.yy).xyz;
		n = normalize(cross(disp_z - disp, disp_x - disp));
	}
	io_nxz += i_wt * n.xz;
}

void SampleFoam(in Texture2DArray i_oceanFoamSampler, in float3 i_uv_slice, in float i_wt, inout half io_foam)
{
	io_foam += i_wt * i_oceanFoamSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, float2(0, 0)).x;
}

void SampleFlow(in Texture2DArray i_oceanFlowSampler, in float3 i_uv_slice, in float i_wt, inout half2 io_flow)
{
	io_flow += i_wt * i_oceanFlowSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, float2(0, 0)).xy;
}

void SampleSeaDepth(in Texture2DArray i_oceanDepthSampler, in float3 i_uv_slice, in float i_wt, inout half io_oceanDepth)
{
	io_oceanDepth += i_wt * (i_oceanDepthSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, float2(0.0, 0.0)).x - CREST_OCEAN_DEPTH_BASELINE);
}

void SampleShadow(in Texture2DArray i_oceanShadowSampler, in float3 i_uv_slice, in float i_wt, inout half2 io_shadow)
{
	io_shadow += i_wt * i_oceanShadowSampler.Sample(LODData_linear_clamp_sampler, i_uv_slice).xy;
}


#define SampleLod(i_lodTextureArray, i_uv_slice) (i_lodTextureArray.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, float2(0, 0)))
#define SampleLodLevel(i_lodTextureArray, i_uv_slice, mips) (i_lodTextureArray.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, mips))

// void SampleLod(in Texture2DArray i_lodTextureArray, in float3 i_uv_slice)
// {
// 	return i_lodTextureArray.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, float2(0, 0));
// }

// void SampleLod(in Texture2DArray i_lodTextureArray, in float3 i_uv_slice, in float2 mips)
// {
// 	return i_lodTextureArray.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, mips);
// }

// Geometry data
// x: Grid size of lod data - size of lod data texel in world space.
// y: Grid size of geometry - distance between verts in mesh.
// zw: normalScrollSpeed0, normalScrollSpeed1
uniform float4 _GeomData;
uniform float3 _OceanCenterPosWorld;

float ComputeLodAlpha(float3 i_worldPos, float i_meshScaleAlpha)
{
	// taxicab distance from ocean center drives LOD transitions
	float2 offsetFromCenter = float2(abs(i_worldPos.x - _OceanCenterPosWorld.x), abs(i_worldPos.z - _OceanCenterPosWorld.z));
	float taxicab_norm = max(offsetFromCenter.x, offsetFromCenter.y);

	// interpolation factor to next lod (lower density / higher sampling period)
	float lodAlpha = taxicab_norm / _LD_Pos_Scale_ThisFrame[_LD_SLICE_Index_ThisLod].z - 1.0;

	// lod alpha is remapped to ensure patches weld together properly. patches can vary significantly in shape (with
	// strips added and removed), and this variance depends on the base density of the mesh, as this defines the strip width.
	// using .15 as black and .85 as white should work for base mesh density as low as 16.
	const float BLACK_POINT = 0.15, WHITE_POINT = 0.85;
	lodAlpha = max((lodAlpha - BLACK_POINT) / (WHITE_POINT - BLACK_POINT), 0.);

	// blend out lod0 when viewpoint gains altitude
	lodAlpha = min(lodAlpha + i_meshScaleAlpha, 1.);

#if _DEBUGDISABLESMOOTHLOD_ON
	lodAlpha = 0.;
#endif

	return lodAlpha;
}

void SnapAndTransitionVertLayout(float i_meshScaleAlpha, inout float3 io_worldPos, out float o_lodAlpha)
{
	// see comments above on _GeomData
	const float GRID_SIZE_2 = 2.0*_GeomData.y, GRID_SIZE_4 = 4.0*_GeomData.y;

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
