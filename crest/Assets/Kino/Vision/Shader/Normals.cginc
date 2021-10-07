// KinoVision - Frame visualization utility
// https://github.com/keijiro/KinoVision

#include "Common.cginc"

half _Blend;
half _Validate;

sampler2D _CameraGBufferTexture2;
sampler2D _CameraDepthNormalsTexture;

half4 NormalsFragment(CommonVaryings input) : SV_Target
{
    half4 src = tex2D(_MainTex, input.uv0);

#ifdef USE_CAMERA_DEPTH_NORMALS
    float4 cdn = tex2D(_CameraDepthNormalsTexture, input.uv1);
    float3 n = DecodeViewNormalStereo(cdn);
    float isZero = (dot(n, 1) == 0);
#else // USE_GBUFFER
    float3 n = tex2D(_CameraGBufferTexture2, input.uv1).xyz;
    float isZero = (dot(n, 1) == 0);
    n = mul((float3x3)unity_WorldToCamera, n * 2 - 1);
    n.z = -n.z;
#endif

    float l = length(n);
    float invalid = max((float)(l < 0.99), (float)(l > 1.01)) - isZero;

    n = (n + 1) * 0.5;
#if !UNITY_COLORSPACE_GAMMA
    n = GammaToLinearSpace(n);
#endif

    half3 rgb = lerp(n, half3(1, 0, 0), invalid * _Validate);
    rgb = lerp(src.rgb, rgb, _Blend);

    return half4(rgb, src.a);
}
