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


// Bias ocean floor depth so that default (0) values in texture are not interpreted as shallow and generating foam everywhere
#define DEPTH_BASELINE 1000.


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

void SampleShadow(in sampler2D i_oceanShadowSampler, float2 i_uv, in float i_wt, inout fixed2 io_shadow)
{
	io_shadow += i_wt * tex2Dlod(i_oceanShadowSampler, float4(i_uv, 0., 0.)).xy;
}

// Geometry data
// x: A square is formed by 2 triangles in the mesh. Here x is square size
// yz: normalScrollSpeed0, normalScrollSpeed1
uniform float3 _GeomData;
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

void SnapAndTransitionVertLayout(float i_meshScaleAlpha, inout float3 io_worldPos, out float o_lodAlpha)
{
	// see comments above on _GeomData
	const float SQUARE_SIZE_2 = 2.0*_GeomData.x, SQUARE_SIZE_4 = 4.0*_GeomData.x;

	// snap the verts to the grid
	// The snap size should be twice the original size to keep the shape of the eight triangles (otherwise the edge layout changes).
	io_worldPos.xz -= frac(unity_ObjectToWorld._m03_m23 / SQUARE_SIZE_2) * SQUARE_SIZE_2; // caution - sign of frac might change in non-hlsl shaders

	// compute lod transition alpha
	o_lodAlpha = ComputeLodAlpha(io_worldPos, i_meshScaleAlpha);

	// now smoothly transition vert layouts between lod levels - move interior verts inwards towards center
	float2 m = frac(io_worldPos.xz / SQUARE_SIZE_4); // this always returns positive
	float2 offset = m - 0.5;
	// check if vert is within one square from the center point which the verts move towards
	const float minRadius = 0.26; //0.26 is 0.25 plus a small "epsilon" - should solve numerical issues
	if (abs(offset.x) < minRadius) io_worldPos.x += offset.x * o_lodAlpha * SQUARE_SIZE_4;
	if (abs(offset.y) < minRadius) io_worldPos.z += offset.y * o_lodAlpha * SQUARE_SIZE_4;
}
