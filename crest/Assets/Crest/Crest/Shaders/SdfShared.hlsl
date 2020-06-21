
// Convert compute shader id to uv texture coordinates
float2 IDtoUV(in float2 i_id, in float i_width, in float i_height)
{
	return (((i_id + 0.5) / float2(i_width, i_height)) - float2(0.5, 0.5)) * 2;
}

float2 IDtoWorld(in uint2 id, in float textureResolution, in float4x4 projectionToWorld)
{
	float2 uv = IDtoUV(id, textureResolution, textureResolution);
	return mul(projectionToWorld, float4(uv, 0, 1)).xz;
}
