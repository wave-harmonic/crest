// KinoVision - Frame visualization utility
// https://github.com/keijiro/KinoVision

#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
float4 _MainTex_ST;

// Common vertex shader

struct CommonAttributes
{
    float4 position : POSITION;
    float2 uv : TEXCOORD;
};

struct CommonVaryings
{
    float4 position : SV_POSITION;
    half2 uv0 : TEXCOORD0; // Screen space UV (supports stereo rendering)
    half2 uv1 : TEXCOORD1; // Alternative UV (supports v-flip case)
};

CommonVaryings CommonVertex(CommonAttributes input)
{
    float2 uv1 = input.uv;

#if UNITY_UV_STARTS_AT_TOP
    if (_MainTex_TexelSize.y < 0) uv1.y = 1 - uv1.y;
#endif

    CommonVaryings o;
    o.position = UnityObjectToClipPos(input.position);
    o.uv0 = UnityStereoScreenSpaceUVAdjust(input.uv, _MainTex_ST);
    o.uv1 = UnityStereoScreenSpaceUVAdjust(uv1, _MainTex_ST);
    return o;
}
