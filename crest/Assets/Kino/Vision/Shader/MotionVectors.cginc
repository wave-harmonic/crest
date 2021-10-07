// KinoVision - Frame visualization utility
// https://github.com/keijiro/KinoVision

#include "Common.cginc"

half _Blend;
half _Amplitude;
int _ColumnCount;
int _RowCount;

sampler2D_half _CameraMotionVectorsTexture;

// Convert a motion vector into RGBA color.
half4 VectorToColor(float2 mv)
{
    half phi = atan2(mv.x, mv.y);
    half hue = (phi / UNITY_PI + 1) * 0.5;

    half r = abs(hue * 6 - 3) - 1;
    half g = 2 - abs(hue * 6 - 2);
    half b = 2 - abs(hue * 6 - 4);
    half a = length(mv);

    return saturate(half4(r, g, b, a));
}

// Motion vectors overlay shader
half4 OverlayFragment(CommonVaryings input) : SV_Target
{
    half4 src = tex2D(_MainTex, input.uv0);

    half2 mv = tex2D(_CameraMotionVectorsTexture, input.uv1).rg * _Amplitude;
    half4 mc = VectorToColor(mv);

    half3 rgb = mc.rgb;
#if !UNITY_COLORSPACE_GAMMA
    rgb = GammaToLinearSpace(rgb);
#endif

    half src_ratio = saturate(2 - _Blend * 2);
    half mc_ratio = saturate(_Blend * 2);
    rgb = lerp(src.rgb * src_ratio, rgb, mc.a * mc_ratio);

    return half4(rgb, src.a);
}

// Motion vector arrow shader
struct ArrowVaryings
{
    float4 position : SV_POSITION;
    float2 scoord : TEXCOORD;
    half4 color : COLOR;
};

ArrowVaryings ArrowVertex(uint vertex_id : SV_VertexID)
{
    // Screen aspect ratio
    float aspect = _ScreenParams.x * (_ScreenParams.w - 1);
    float inv_aspect = _ScreenParams.y * (_ScreenParams.z - 1);

    // Vertex IDs
    uint arrow_id = vertex_id / 6;
    uint point_id = vertex_id - arrow_id * 6;

    // Column/Row number of the arrow
    uint row = arrow_id / _ColumnCount;
    uint col = arrow_id - row * _ColumnCount;

    // Texture coordinate of the reference point
    float2 uv = float2((col + 0.5) / _ColumnCount, (row + 0.5) / _RowCount);

    // Retrieve the motion vector.
    half2 mv = tex2Dlod(_CameraMotionVectorsTexture, float4(uv, 0, 0)).rg * _Amplitude;

    // Arrow color
    half4 color = VectorToColor(mv);

    // Arrow vertex position parameter (0=origin, 1=head)
    float arrow_l = point_id > 0;

    // Rotation matrix for the arrow head
    float2 head_dir = normalize(mv * float2(aspect, 1));
    float2x2 head_rot = float2x2(head_dir.y, head_dir.x, -head_dir.x, head_dir.y);

    // Offset for arrow head vertices
    float head_x = point_id == 3 ? -1 : (point_id == 5 ? 1 : 0);
    head_x *= arrow_l * 0.3 * saturate(length(mv) * _RowCount);

    float2 head_offs = float2(head_x, -abs(head_x));
    head_offs = mul(head_rot, head_offs) * float2(inv_aspect, 1);

    // Vertex position in the clip space
    float2 vp = mv * arrow_l + head_offs * 2 / _RowCount + uv * 2 - 1;

    // Convert to the screen coordinates.
    float2 scoord = (vp + 1) * 0.5 * _ScreenParams.xy;

    // Snap to a pixel-perfect position.
    scoord = round(scoord);

    // Bring back to the clip space.
    vp = (scoord + 0.5) * (_ScreenParams.zw - 1) * 2 - 1;
    vp.y *= _ProjectionParams.x;

    // Color tweaks
    color.rgb = GammaToLinearSpace(lerp(color.rgb, 1, 0.5));
    color.a = _Blend;

    // Output
    ArrowVaryings o;
    o.position = float4(vp, 0, 1);
    o.scoord = scoord;
    o.color = color;
    return o;
}

half4 ArrowFragment(ArrowVaryings input) : SV_Target
{
    // Pseudo anti-aliasing
    float aa = length(frac(input.scoord) - 0.5) / 0.707;
    aa *= (aa * (aa * 0.305306011 + 0.682171111) + 0.012522878); // gamma
    return half4(input.color.rgb, input.color.a * aa);
}
