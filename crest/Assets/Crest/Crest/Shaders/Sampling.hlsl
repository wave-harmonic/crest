// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Ocean LOD data - data, samplers and functions associated with LODs

SamplerState Sampling_linear_clamp_sampler;
SamplerState Sampling_point_clamp_sampler;

// Hardware
float4 SampleManualLerp0(in Texture2DArray i_texture, in float3 i_uv_slice, in float i_resolution)
{
	return i_texture.SampleLevel(Sampling_linear_clamp_sampler, i_uv_slice, 0.0);
}

// Manual Linear: https://iquilezles.org/articles/texture/
float4 SampleManualLerp1(in Texture2DArray i_texture, float3 i_uv_slice, in float i_resolution)
{
	float2 p = i_uv_slice.xy;
    p = p * i_resolution + 0.5;

    float2 i = floor(p);
    float2 f = p - i;
    f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    p = i + f;

    p = (p - 0.5 ) / i_resolution;
    return i_texture.SampleLevel(Sampling_point_clamp_sampler, float3(p, i_uv_slice.z), 0.0);
}

// Manual Bilinear: https://iquilezles.org/articles/hwinterpolation/
float4 SampleManualLerp2(in Texture2DArray i_texture, in float3 i_uv_slice, in float i_resolution)
{
	float2 resolution = float2(i_resolution, i_resolution);

    float2 st = i_uv_slice.xy * resolution - 0.5;

    float2 iuv = floor(st);
    float2 fuv = frac(st);

    float4 a = i_texture.SampleLevel(Sampling_point_clamp_sampler, float3((iuv + float2(0.5, 0.5)) / resolution, i_uv_slice.z), 0.0);
    float4 b = i_texture.SampleLevel(Sampling_point_clamp_sampler, float3((iuv + float2(1.5, 0.5)) / resolution, i_uv_slice.z), 0.0);
    float4 c = i_texture.SampleLevel(Sampling_point_clamp_sampler, float3((iuv + float2(0.5, 1.5)) / resolution, i_uv_slice.z), 0.0);
    float4 d = i_texture.SampleLevel(Sampling_point_clamp_sampler, float3((iuv + float2(1.5, 1.5)) / resolution, i_uv_slice.z), 0.0);

    return lerp(lerp(a, b, fuv.x), lerp(c, d, fuv.x), fuv.y);
}

// Hardware
float4 SampleManualLerp0(in Texture2D i_texture, in float2 i_uv, in float i_resolution)
{
	return i_texture.SampleLevel(Sampling_linear_clamp_sampler, i_uv, 0.0);
}

// Manual Linear: https://iquilezles.org/articles/texture/
float4 SampleManualLerp1(in Texture2D i_texture, float2 i_uv, in float i_resolution)
{
	float2 p = i_uv.xy;
    p = p * i_resolution + 0.5;

    float2 i = floor(p);
    float2 f = p - i;
    f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    p = i + f;

    p = (p - 0.5 ) / i_resolution;
    return i_texture.SampleLevel(Sampling_point_clamp_sampler, p, 0.0);
}

// Manual Bilinear: https://iquilezles.org/articles/hwinterpolation/
float4 SampleManualLerp2(in Texture2D i_texture, in float3 i_uv, in float i_resolution)
{
	float2 resolution = float2(i_resolution, i_resolution);

    float2 st = i_uv.xy * resolution - 0.5;

    float2 iuv = floor(st);
    float2 fuv = frac(st);

    float4 a = i_texture.SampleLevel(Sampling_point_clamp_sampler, (iuv + float2(0.5, 0.5)) / resolution, 0.0);
    float4 b = i_texture.SampleLevel(Sampling_point_clamp_sampler, (iuv + float2(1.5, 0.5)) / resolution, 0.0);
    float4 c = i_texture.SampleLevel(Sampling_point_clamp_sampler, (iuv + float2(0.5, 1.5)) / resolution, 0.0);
    float4 d = i_texture.SampleLevel(Sampling_point_clamp_sampler, (iuv + float2(1.5, 1.5)) / resolution, 0.0);

    return lerp(lerp(a, b, fuv.x), lerp(c, d, fuv.x), fuv.y);
}

// Hardware
float SampleFloatManualLerp0(in Texture2D<float> i_texture, in float2 i_uv, in float i_resolution)
{
	return i_texture.SampleLevel(Sampling_linear_clamp_sampler, i_uv, 0.0);
}

// Manual Linear: https://iquilezles.org/articles/texture/
float SampleFloatManualLerp1(in Texture2D<float> i_texture, float2 i_uv, in float i_resolution)
{
	float2 p = i_uv.xy;
    p = p * i_resolution + 0.5;

    float2 i = floor(p);
    float2 f = p - i;
    f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    p = i + f;

    p = (p - 0.5 ) / i_resolution;
    return i_texture.SampleLevel(Sampling_point_clamp_sampler, p, 0.0);
}

// Manual Bilinear: https://iquilezles.org/articles/hwinterpolation/
float SampleFloatManualLerp2(in Texture2D<float> i_texture, in float2 i_uv, in float i_resolution)
{
	float2 resolution = float2(i_resolution, i_resolution);

    float2 st = i_uv.xy * resolution - 0.5;

    float2 iuv = floor(st);
    float2 fuv = frac(st);

    float a = i_texture.SampleLevel(Sampling_point_clamp_sampler, (iuv + float2(0.5, 0.5)) / resolution, 0.0);
    float b = i_texture.SampleLevel(Sampling_point_clamp_sampler, (iuv + float2(1.5, 0.5)) / resolution, 0.0);
    float c = i_texture.SampleLevel(Sampling_point_clamp_sampler, (iuv + float2(0.5, 1.5)) / resolution, 0.0);
    float d = i_texture.SampleLevel(Sampling_point_clamp_sampler, (iuv + float2(1.5, 1.5)) / resolution, 0.0);

    return lerp(lerp(a, b, fuv.x), lerp(c, d, fuv.x), fuv.y);
}
