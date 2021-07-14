// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Generates waves from geometry that is rendered into the water simulation from a top down camera. Expects
// following data on verts:
//   - POSITION: Vert positions as normal.
//   - TEXCOORD0: Axis - direction for waves to travel. "Forward vector" for waves.
//   - TEXCOORD1: X - 0 at start of waves, 1 at end of waves
//
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ uv1.x = 0             |
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  |                    |  uv0 - wave direction vector
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  |                   \|/
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ uv1.x = 1
//  ------------------- shoreline --------------------
//

Shader "Hidden/Crest/Inputs/Flow/Spline Geometry"
{
    SubShader
    {
        // Additive blend everywhere
        Blend One One
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            // #pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"

            #include "../../OceanGlobals.hlsl"
            #include "../../OceanInputsDriven.hlsl"
            #include "../../OceanHelpersNew.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 axis : TEXCOORD0;
                float invNormDistToShoreline : TEXCOORD1;
                float speed : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 uv_slice : TEXCOORD1;
                float2 axis : TEXCOORD2;
                float invNormDistToShoreline : TEXCOORD4;
                float speed : TEXCOORD5;
            };

            CBUFFER_START(GerstnerPerMaterial)
            half _FeatherWaveStart;
            CBUFFER_END

            CBUFFER_START(CrestPerOceanInput)
            float _Weight;
            CBUFFER_END

            Varyings Vert(Attributes v)
            {
                Varyings o;

                const float3 positionOS = v.positionOS;
                o.positionCS = UnityObjectToClipPos(positionOS);
                const float3 worldPos = mul( unity_ObjectToWorld, float4(positionOS, 1.0) ).xyz;

                // UV coordinate into the cascade we are rendering into
                o.uv_slice.xyz = WorldToUV(worldPos.xz, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);

                o.invNormDistToShoreline = v.invNormDistToShoreline;

                // Rotate local-space sideays axis around y-axis, by 90deg, and by object to world to move into world space
                o.axis = v.axis.y * unity_ObjectToWorld._m00_m20 - v.axis.x * unity_ObjectToWorld._m02_m22;

                o.speed = v.speed;

                return o;
            }

            float2 Frag(Varyings input) : SV_Target
            {
                float wt = _Weight;

                // Feather at front/back
                if( input.invNormDistToShoreline > 0.5 ) input.invNormDistToShoreline = 1.0 - input.invNormDistToShoreline;
                wt *= min( input.invNormDistToShoreline / _FeatherWaveStart, 1.0 );

                return wt * input.speed * input.axis;
            }
            ENDCG
        }
    }
}
