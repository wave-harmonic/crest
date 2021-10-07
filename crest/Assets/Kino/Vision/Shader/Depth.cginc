// KinoVision - Frame visualization utility
// https://github.com/keijiro/KinoVision

#include "Common.cginc"

half _Blend;
half _Repeat;

sampler2D_float _CameraDepthTexture;
sampler2D _CameraDepthNormalsTexture;

float LinearizeDepth(float z)
{
    float isOrtho = unity_OrthoParams.w;
    float isPers = 1 - unity_OrthoParams.w;
    z *= _ZBufferParams.x;
    return (1 - isOrtho * z) / (isPers * z + _ZBufferParams.y);
}

half4 DepthFragment(CommonVaryings input) : SV_Target
{
    half4 src = tex2D(_MainTex, input.uv0);

#ifdef USE_CAMERA_DEPTH
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.uv1);
    depth = LinearizeDepth(depth);
#else // USE_CAMERA_DEPTH_NORMALS
    float4 cdn = tex2D(_CameraDepthNormalsTexture, input.uv1);
    float depth = DecodeFloatRG(cdn.zw);
#endif

    float dr = frac(depth * _Repeat);
    float d1 = 1 - dr;
    float d2 = 1 / (1 + dr * 100);
    half3 rgb = half3(d1, d2, d2);

#if !UNITY_COLORSPACE_GAMMA
    rgb = GammaToLinearSpace(rgb);
#endif

    rgb = lerp(src.rgb, rgb, _Blend);

    return half4(rgb, src.a);
}
