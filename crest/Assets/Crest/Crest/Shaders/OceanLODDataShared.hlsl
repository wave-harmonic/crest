// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Ocean LOD data - data, samplers and functions associated with LODs

// NOTE: This must match the value in LodDataMgr.cs, as it is used to allow the
// C# code to check if any parameters are within the MAX_LOD_COUNT limits
#define MAX_LOD_COUNT 16

// NOTE: these MUST match the values in PropertyWrapper.cs
#define THREAD_GROUP_SIZE_X 8
#define THREAD_GROUP_SIZE_Y 8

// Samplers and data associated with a LOD.
// _LD_Params: float4(world texel size, texture resolution, shape weight multiplier, 1 / texture resolution)
Texture2DArray _LD_TexArray_AnimatedWaves;
Texture2DArray _LD_TexArray_WaveBuffer;
Texture2DArray _LD_TexArray_SeaFloorDepth;
Texture2DArray _LD_TexArray_Foam;
Texture2DArray _LD_TexArray_Flow;
Texture2DArray _LD_TexArray_DynamicWaves;
Texture2DArray _LD_TexArray_Shadow;
uniform float4 _LD_Params[MAX_LOD_COUNT];
uniform float3 _LD_Pos_Scale[MAX_LOD_COUNT];
uniform const float _LD_SliceIndex;

Texture2DArray _LD_TexArray_AnimatedWaves_PrevFrame;
Texture2DArray _LD_TexArray_WaveBuffer_PrevFrame;
Texture2DArray _LD_TexArray_SeaFloorDepth_PrevFrame;
Texture2DArray _LD_TexArray_Foam_PrevFrame;
Texture2DArray _LD_TexArray_Flow_PrevFrame;
Texture2DArray _LD_TexArray_DynamicWaves_PrevFrame;
Texture2DArray _LD_TexArray_Shadow_PrevFrame;
uniform float4 _LD_Params_PrevFrame[MAX_LOD_COUNT];
uniform float3 _LD_Pos_Scale_PrevFrame[MAX_LOD_COUNT];
uniform const float _LD_SliceIndex_PrevFrame;

SamplerState LODData_linear_clamp_sampler;

// Bias ocean floor depth so that default (0) values in texture are not interpreted as shallow and generating foam everywhere
#define CREST_OCEAN_DEPTH_BASELINE -1000.0

// Conversions for world space from/to UV space. All these should *not* be clamped otherwise they'll break fullscreen triangles.
float2 LD_WorldToUV(in float2 i_samplePos, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return (i_samplePos - i_centerPos) / (i_texelSize * i_res) + 0.5;
}

float3 WorldToUV(in float2 i_samplePos, in float i_sliceIndex) {
	const float2 result = LD_WorldToUV(
		i_samplePos,
		_LD_Pos_Scale[i_sliceIndex].xy,
		_LD_Params[i_sliceIndex].y,
		_LD_Params[i_sliceIndex].x
	);
	return float3(result, i_sliceIndex);
}

float3 WorldToUV_NextLod(in float2 i_samplePos, in float i_sliceIndex_NextLod) {
	const float2 result = LD_WorldToUV(
		i_samplePos, _LD_Pos_Scale[i_sliceIndex_NextLod].xy,
		_LD_Params[i_sliceIndex_NextLod].y,
		_LD_Params[i_sliceIndex_NextLod].x
	);
	return float3(result, i_sliceIndex_NextLod);
}

float3 WorldToUV_PrevFrame(in float2 i_samplePos, in float i_sliceIndex_PrevFrame) {
	const float2 result = LD_WorldToUV(
		i_samplePos,
		_LD_Pos_Scale_PrevFrame[i_sliceIndex_PrevFrame].xy,
		_LD_Params_PrevFrame[i_sliceIndex_PrevFrame].y,
		_LD_Params_PrevFrame[i_sliceIndex_PrevFrame].x
	);
	return float3(result, i_sliceIndex_PrevFrame);
}


float2 LD_UVToWorld(in float2 i_uv, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return i_texelSize * i_res * (i_uv - 0.5) + i_centerPos;
}

float2 UVToWorld(in float2 i_uv, in float i_sliceIndex) { return LD_UVToWorld(i_uv, _LD_Pos_Scale[i_sliceIndex].xy, _LD_Params[i_sliceIndex].y, _LD_Params[i_sliceIndex].x); }

float3 WorldToUV(in float2 i_samplePos) { return WorldToUV(i_samplePos, _LD_SliceIndex); }
float3 WorldToUV_NextLod(in float2 i_samplePos) { return WorldToUV_NextLod(i_samplePos, _LD_SliceIndex + 1); }
float3 WorldToUV_PrevFrame(in float2 i_samplePos) { return WorldToUV_PrevFrame(i_samplePos, _LD_SliceIndex_PrevFrame); }
float2 UVToWorld(in float2 i_uv) { return UVToWorld(i_uv, _LD_SliceIndex); }

// Convert compute shader id to uv texture coordinates
float2 IDtoUV(in float2 i_id)
{
	return float2(float2(i_id) / float2(256, 256) + 0.5 / float2(256, 256));
}
float2 UVToID(in float2 i_uv)
{
	return float2((i_uv.xy * float2(256, 256)) - 0.5);
}

// Sampling functions
void SampleDisplacements(in Texture2DArray i_dispSampler, in float3 i_uv_slice, in float i_wt, inout float3 io_worldPos)
{
	const half3 disp = i_dispSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, 0).xyz;
	io_worldPos += i_wt * disp;
}

void SampleDisplacementsNormals(in Texture2DArray i_dispSampler, in float3 i_uv_slice, in float i_wt, in float i_invRes, in float i_texelSize, inout float3 io_worldPos, inout half2 io_nxz)
{
	const half3 disp = i_dispSampler.Sample(LODData_linear_clamp_sampler, i_uv_slice).xyz;
	io_worldPos += i_wt * disp;

	float3 n; {
		float3 dd = float3(i_invRes, 0.0, i_texelSize);
		half3 disp_x = dd.zyy + i_dispSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice + float3(dd.xy, 0), dd.y).xyz;
		half3 disp_z = dd.yyz + i_dispSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice + float3(dd.yx, 0), dd.y).xyz;
		n = normalize(cross(disp_z - disp, disp_x - disp));
	}
	io_nxz += i_wt * n.xz;
}

void SampleFoam(in Texture2DArray i_oceanFoamSampler, in float3 i_uv_slice, in float i_wt, inout half io_foam)
{
	io_foam += i_wt * i_oceanFoamSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, 0).x;
}

void SampleFlow(in Texture2DArray i_oceanFlowSampler, in float3 i_uv_slice, in float i_wt, inout half2 io_flow)
{
	io_flow += i_wt * i_oceanFlowSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, 0).xy;
}

void SampleSeaDepth(in Texture2DArray i_oceanDepthSampler, in float3 i_uv_slice, in float i_wt, inout half io_oceanDepth)
{
	io_oceanDepth += i_wt * (i_oceanDepthSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, 0).x - CREST_OCEAN_DEPTH_BASELINE);
}

void SampleShadow(in Texture2DArray i_oceanShadowSampler, in float3 i_uv_slice, in float i_wt, inout half2 io_shadow)
{
	io_shadow += i_wt * i_oceanShadowSampler.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, 0).xy;
}

// TODO(MRT): replace these with something better in code that uses them.
// Used #defines to account for different return types based on texture format.
// Check if #defines are even that bad, I tend to not be a big fan of them, but
// maybe this is the best solution.
#define SampleLod(i_lodTextureArray, i_uv_slice) (i_lodTextureArray.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, 0))
#define SampleLodLevel(i_lodTextureArray, i_uv_slice, mips) (i_lodTextureArray.SampleLevel(LODData_linear_clamp_sampler, i_uv_slice, mips))

// Geometry data
// x: Grid size of lod data - size of lod data texel in world space.
// y: Grid size of geometry - distance between verts in mesh.
// zw: normalScrollSpeed0, normalScrollSpeed1
uniform float4 _GeomData;
uniform float3 _OceanCenterPosWorld;
