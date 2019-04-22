// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Ocean LOD data - data, samplers and functions associated with LODs


// Samplers and data associated with a LOD.
// _LD_Params: float4(world texel size, texture resolution, shape weight multiplier, 1 / texture resolution)
#define LOD_DATA(LODNUM) \
	uniform sampler2D _LD_Sampler_AnimatedWaves_##LODNUM; \
	uniform sampler2D _LD_Sampler_SeaFloorDepth_##LODNUM; \
	uniform sampler2D _LD_Sampler_Foam_##LODNUM; \
	uniform sampler2D _LD_Sampler_Flow_##LODNUM; \
	uniform sampler2D _LD_Sampler_DynamicWaves_##LODNUM; \
	uniform sampler2D _LD_Sampler_Shadow_##LODNUM; \
	uniform float4 _LD_Params_##LODNUM; \
	uniform float3 _LD_Pos_Scale_##LODNUM;

// Create two sets of LOD data, which have overloaded meaning depending on use:
// * the ocean surface geometry always lerps from a more detailed LOD (0) to a less detailed LOD (1)
// * simulations (persistent lod data) read last frame's data from slot 0, and any current frame data from slot 1
// * any other use that does not fall into the previous categories can use either slot and generally use slot 0
LOD_DATA( 0 )
LOD_DATA( 1 )

// Bias ocean floor depth so that default (0) values in texture are not interpreted as shallow and generating foam everywhere
#define CREST_OCEAN_DEPTH_BASELINE 1000.

// Conversions for world space from/to UV space
float2 LD_WorldToUV(in float2 i_samplePos, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return (i_samplePos - i_centerPos) / (i_texelSize * i_res) + 0.5;
}
float2 LD_0_WorldToUV(in float2 i_samplePos) { return LD_WorldToUV(i_samplePos, _LD_Pos_Scale_0.xy, _LD_Params_0.y, _LD_Params_0.x); }
float2 LD_1_WorldToUV(in float2 i_samplePos) { return LD_WorldToUV(i_samplePos, _LD_Pos_Scale_1.xy, _LD_Params_1.y, _LD_Params_1.x); }

float2 LD_UVToWorld(in float2 i_uv, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return i_texelSize * i_res * (i_uv - 0.5) + i_centerPos;
}
float2 LD_0_UVToWorld(in float2 i_uv) { return LD_UVToWorld(i_uv, _LD_Pos_Scale_0.xy, _LD_Params_0.y, _LD_Params_0.x); }
float2 LD_1_UVToWorld(in float2 i_uv) { return LD_UVToWorld(i_uv, _LD_Pos_Scale_1.xy, _LD_Params_1.y, _LD_Params_1.x); }

// Conversions for world space from/to id space (used by compute shaders)
float2 LD_WorldToID(in float2 i_samplePos, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return ((i_samplePos - i_centerPos) / i_texelSize) + (0.5 * i_res);
}
float2 LD_0_WorldToID(in float2 i_samplePos) { return LD_WorldToID(i_samplePos, _LD_Pos_Scale_0.xy, _LD_Params_0.y, _LD_Params_0.x); }
float2 LD_1_WorldToID(in float2 i_samplePos) { return LD_WorldToID(i_samplePos, _LD_Pos_Scale_1.xy, _LD_Params_1.y, _LD_Params_1.x); }

float2 LD_IDToWorld(in float2 i_id, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return i_texelSize * (i_id - (i_res * 0.5)) + i_centerPos;
}
float2 LD_0_IDToWorld(in float2 i_id) { return LD_IDToWorld(i_id, _LD_Pos_Scale_0.xy, _LD_Params_0.y, _LD_Params_0.x); }
float2 LD_1_IDToWorld(in float2 i_id) { return LD_IDToWorld(i_id, _LD_Pos_Scale_1.xy, _LD_Params_1.y, _LD_Params_1.x); }

float4 IDtoUV(in float2 i_id)
{
	return float4(float2(i_id) / float2(256, 256) + 0.5 / float2(256, 256), 0, 0);
}

// Sampling functions
void SampleDisplacements(in sampler2D i_dispSampler, in float2 i_uv, in float i_wt, inout float3 io_worldPos)
{
	const half3 disp = tex2Dlod(i_dispSampler, float4(i_uv, 0., 0.)).xyz;
	io_worldPos += i_wt * disp;
}

void SampleDisplacementsNormals(in sampler2D i_dispSampler, in float2 i_uv, in float i_wt, in float i_invRes, in float i_texelSize, inout float3 io_worldPos, inout half2 io_nxz)
{
	const float4 uv = float4(i_uv, 0., 0.);

	const half3 disp = tex2Dlod(i_dispSampler, uv).xyz;
	io_worldPos += i_wt * disp;

	float3 n; {
		float3 dd = float3(i_invRes, 0.0, i_texelSize);
		half3 disp_x = dd.zyy + tex2Dlod(i_dispSampler, uv + dd.xyyy).xyz;
		half3 disp_z = dd.yyz + tex2Dlod(i_dispSampler, uv + dd.yxyy).xyz;
		n = normalize(cross(disp_z - disp, disp_x - disp));
	}
	io_nxz += i_wt * n.xz;
}

void SampleFoam(in sampler2D i_oceanFoamSampler, float2 i_uv, in float i_wt, inout half io_foam)
{
	io_foam += i_wt * tex2Dlod(i_oceanFoamSampler, float4(i_uv, 0., 0.)).x;
}

void SampleFlow(in sampler2D i_oceanFlowSampler, float2 i_uv, in float i_wt, inout half2 io_flow)
{
	const float4 uv = float4(i_uv, 0., 0.);
	io_flow += i_wt * tex2Dlod(i_oceanFlowSampler, uv).xy;
}

void SampleSeaFloorHeightAboveBaseline(in sampler2D i_oceanDepthSampler, float2 i_uv, in float i_wt, inout half io_oceanDepth)
{
	io_oceanDepth += i_wt * (tex2Dlod(i_oceanDepthSampler, float4(i_uv, 0., 0.)).x);
}

void SampleShadow(in sampler2D i_oceanShadowSampler, float2 i_uv, in float i_wt, inout half2 io_shadow)
{
	io_shadow += i_wt * tex2Dlod(i_oceanShadowSampler, float4(i_uv, 0., 0.)).xy;
}

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
	float lodAlpha = taxicab_norm / _LD_Pos_Scale_0.z - 1.0;

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
