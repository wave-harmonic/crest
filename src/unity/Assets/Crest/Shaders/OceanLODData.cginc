// ocean LOD data

// samplers and data associated with a LOD.
// _WD_Params: float4(world texel size, texture resolution, shape weight multiplier, 1 / texture resolution)
#define SHAPE_LOD_PARAMS(LODNUM) \
	uniform sampler2D _WD_Sampler_##LODNUM; \
	uniform sampler2D _WD_OceanDepth_Sampler_##LODNUM; \
	uniform float4 _WD_Params_##LODNUM; \
	uniform float2 _WD_Pos_##LODNUM; \
	uniform int _WD_LodIdx_##LODNUM;

// create two sets of LOD data. we always need only 2 textures - we're always lerping between two LOD levels
SHAPE_LOD_PARAMS( 0 )
SHAPE_LOD_PARAMS( 1 )

float2 WD_worldToUV(in float2 i_samplePos, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return (i_samplePos - i_centerPos) / (i_texelSize * i_res) + 0.5;
}

float2 WD_uvToWorld(in float2 i_uv, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return i_texelSize * i_res * (i_uv - 0.5) + i_centerPos;
}

#define DEPTH_BIAS 100.
uniform half _ShorelineFoamMaxDepth;

// sample wave or terrain height, with smooth blend towards edges.
// would equally apply to heights instead of displacements.
// this could be optimized further.
void SampleDisplacements(in sampler2D i_dispSampler, in sampler2D i_oceanDepthSampler, in float2 i_centerPos, in float i_res, in float i_invRes, in float i_texelSize, in float2 i_samplePos, in float wt, inout float3 io_worldPos, inout float3 io_n, inout float io_determinant, inout half io_shorelineFoam)
{
	if (wt < 0.001)
		return;

	float4 uv = float4(WD_worldToUV(i_samplePos, i_centerPos, i_res, i_texelSize), 0., 0.);

	// do computations for hi-res
	float3 dd = float3(i_invRes, 0.0, i_texelSize);
	half4 s = tex2Dlod(i_dispSampler, uv);
	half3 sx = tex2Dlod(i_dispSampler, uv + dd.xyyy).xyz;
	half3 sz = tex2Dlod(i_dispSampler, uv + dd.yxyy).xyz;
	half3 disp = s.xyz;
	half3 disp_x = dd.zyy + sx;
	half3 disp_z = dd.yyz + sz;
	io_worldPos += wt * disp;

	float3 n = normalize(cross(disp_z - disp, disp_x - disp));
	io_n.xz += wt * n.xz;

	// The determinant of the displacement Jacobian is a good measure for turbulence:
	// > 1: Stretch
	// < 1: Squash
	// < 0: Overlap
	float4 du = float4(disp_x.xz, disp_z.xz) - disp.xzxz;
	float det = (du.x * du.w - du.y * du.z) / (dd.z * dd.z);
	// actually store 1-determinant. This means that when far lod is faded out to 0, this tends to make foam and scatter color etc fade out, instead of getting stronger.
	det = 1. - det;
	io_determinant += wt * det;

	// foam from shallow water - signed depth is depth compared to sea level, plus wave height. depth bias is an optimisation
	// which allows the depth data to be initialised once to 0 without generating foam everywhere.
	half signedDepth = (tex2Dlod(i_oceanDepthSampler, uv).x + DEPTH_BIAS) + disp.y;
	io_shorelineFoam += wt * clamp(1. - signedDepth / _ShorelineFoamMaxDepth, 0., 1.);
}

// Geometry data
// x: A square is formed by 2 triangles in the mesh. Here x is square size
// yz: normalScrollSpeed0, normalScrollSpeed1
// w: Geometry density - side length of patch measured in squares
uniform float4 _GeomData;
uniform float3 _OceanCenterPosWorld;

void SnapAndTransitionVertLayout(float meshScaleAlpha, inout float3 io_worldPos, out float o_lodAlpha)
{
	// see comments above on _GeomData
	const float SQUARE_SIZE = _GeomData.x, SQUARE_SIZE_2 = 2.0*_GeomData.x, SQUARE_SIZE_4 = 4.0*_GeomData.x;
	const float BASE_DENSITY = _GeomData.w;

	// snap the verts to the grid
	// The snap size should be twice the original size to keep the shape of the eight triangles (otherwise the edge layout changes).
	io_worldPos.xz -= frac(unity_ObjectToWorld._m03_m23 / SQUARE_SIZE_2) * SQUARE_SIZE_2; // caution - sign of frac might change in non-hlsl shaders

	// how far are we into the current LOD? compute by comparing the desired square size with the actual square size
	float2 offsetFromCenter = float2(abs(io_worldPos.x - _OceanCenterPosWorld.x), abs(io_worldPos.z - _OceanCenterPosWorld.z));
	float taxicab_norm = max(offsetFromCenter.x, offsetFromCenter.y);
	float idealSquareSize = taxicab_norm / BASE_DENSITY;
	// interpolation factor to next lod (lower density / higher sampling period)
	o_lodAlpha = idealSquareSize / SQUARE_SIZE - 1.0;
	// lod alpha is remapped to ensure patches weld together properly. patches can vary significantly in shape (with
	// strips added and removed), and this variance depends on the base density of the mesh, as this defines the strip width.
	// using .15 as black and .85 as white should work for base mesh density as low as 16. TODO - make this automatic?
	const float BLACK_POINT = 0.15, WHITE_POINT = 0.85;
	o_lodAlpha = max((o_lodAlpha - BLACK_POINT) / (WHITE_POINT - BLACK_POINT), 0.);
	// blend out lod0 when viewpoint gains altitude
	o_lodAlpha = min(o_lodAlpha + meshScaleAlpha, 1.);
	#if _DEBUGDISABLESMOOTHLOD_ON
	o_lodAlpha = 0.;
	#endif

	// now smoothly transition vert layouts between lod levels - move interior verts inwards towards center
	float2 m = frac(io_worldPos.xz / SQUARE_SIZE_4); // this always returns positive
	float2 offset = m - 0.5;
	// check if vert is within one square from the center point which the verts move towards
	const float minRadius = 0.26; //0.26 is 0.25 plus a small "epsilon" - should solve numerical issues
	if (abs(offset.x) < minRadius) io_worldPos.x += offset.x * o_lodAlpha * SQUARE_SIZE_4;
	if (abs(offset.y) < minRadius) io_worldPos.z += offset.y * o_lodAlpha * SQUARE_SIZE_4;
}
