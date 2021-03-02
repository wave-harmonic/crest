// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Generates waves from geometry that is rendered into the water simulation from a top down camera. Expects
// following data on verts:
//   - POSITION: Vert positions as normal.
//   - TEXCOORD0: Axis - direction for waves to travel. "Forward vector" for waves.
//   - TEXCOORD1: X - distance from nearest side, needed if "feather from spline ends" is non-zero.
//                Y - 0 at start of waves, 1 at end of waves
//
//  uv1.x = 0 -------- uv1.x = 50m --------- uv1.x = 0
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ uv1.y = 0             |
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  |                    |  uv0 - wave direction vector
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  |                   \|/
//  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ uv1.y = 1
//  ------------------- shoreline --------------------
//

Shader "Hidden/Crest/Inputs/Flow/Spline Geometry"
{
    Properties
    {
        // Controls ramp distance over which waves grow/fade as they move forwards
        _FeatherWaveStart( "Feather wave start (0-1)", Range( 0.0, 0.5 ) ) = 0.1
        // Fade in waves from the sides
        _FeatherFromSplineEnds( "Feather from spline ends (m)", Range( 0.0, 100.0 ) ) = 0.0
        // Can be set to 0 to make waves ignore shallow water
        _RespectShallowWaterAttenuation( "Respect Shallow Water Attenuation", Range( 0, 1 ) ) = 1

		_Speed( "Speed", Float ) = 5.0
    }

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
            #pragma vertex vert
            #pragma fragment frag
            #pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"

            #include "../../OceanGlobals.hlsl"
            #include "../../OceanInputsDriven.hlsl"
            #include "../../OceanHelpersNew.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 axis : TEXCOORD0;
                float2 distToSplineEnd_invNormDistToShoreline : TEXCOORD1;
				float speed : TEXCOORD2;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 uv_slice : TEXCOORD1;
                float2 axis : TEXCOORD2;
                float2 distToSplineEnd_invNormDistToShoreline : TEXCOORD4;
				float speed : TEXCOORD5;
            };

            CBUFFER_START(GerstnerPerMaterial)
            half _FeatherWaveStart;
            half _FeatherFromSplineEnds;
            float _RespectShallowWaterAttenuation;
			float _Speed;
            CBUFFER_END

            CBUFFER_START(CrestPerOceanInput)
            float _AverageWavelength;
            float _AttenuationInShallows;
            float _Weight;
            float2 _AxisX;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;

                const float3 positionOS = v.vertex.xyz;
                o.vertex = UnityObjectToClipPos(positionOS);
                const float3 worldPos = mul( unity_ObjectToWorld, float4(positionOS, 1.0) ).xyz;

                // UV coordinate into the cascade we are rendering into
                o.uv_slice.xyz = WorldToUV(worldPos.xz, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);

                o.distToSplineEnd_invNormDistToShoreline = v.distToSplineEnd_invNormDistToShoreline;

                // Rotate local-space sideays axis around y-axis, by 90deg, and by object to world to move into world space
				o.axis = v.axis.y * unity_ObjectToWorld._m00_m20 - v.axis.x * unity_ObjectToWorld._m02_m22;

				o.speed = v.speed;

                return o;
            }

            float2 frag(v2f input) : SV_Target
            {
                float wt = _Weight;

                // Feature at front/back
				if( input.distToSplineEnd_invNormDistToShoreline.y > 0.5 ) input.distToSplineEnd_invNormDistToShoreline.y = 1.0 - input.distToSplineEnd_invNormDistToShoreline.y;
                wt *= min( input.distToSplineEnd_invNormDistToShoreline.y / _FeatherWaveStart, 1.0 );
                if( _FeatherFromSplineEnds > 0.0 ) wt *= saturate( input.distToSplineEnd_invNormDistToShoreline.x / _FeatherFromSplineEnds );

				return wt * _Speed * input.speed * input.axis;
            }
            ENDCG
        }
    }
}
