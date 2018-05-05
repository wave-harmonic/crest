// ocean LOD data

// samplers and data associated with a LOD.
// _WD_Params: float3(texel size, texture resolution, shape weight multiplier)
#define SHAPE_LOD_PARAMS(LODNUM) \
	uniform sampler2D _WD_Sampler_##LODNUM; \
	uniform sampler2D _WD_OceanDepth_Sampler_##LODNUM; \
	uniform float3 _WD_Params_##LODNUM; \
	uniform float2 _WD_Pos_##LODNUM; \
	uniform int _WD_LodIdx_##LODNUM;

// create two sets of LOD data. we always need only 2 textures - we're always lerping between two LOD levels
SHAPE_LOD_PARAMS( 0 )
SHAPE_LOD_PARAMS( 1 )

float2 WD_worldToUV(in float2 i_samplePos, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return (i_samplePos - i_centerPos) / (i_texelSize*i_res) + 0.5;
}

float2 WD_uvToWorld(in float2 i_uv, in float2 i_centerPos, in float i_res, in float i_texelSize)
{
	return i_texelSize * i_res * (i_uv - 0.5) + i_centerPos;
}
