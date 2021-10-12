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

Shader "Crest/Inputs/Animated Waves/Gerstner Geometry"
{
    Properties
    {
        // Controls ramp distance over which waves grow/fade as they move forwards
        _FeatherWaveStart( "Feather wave start (0-1)", Range( 0.0, 0.5 ) ) = 0.1
        // Can be set to 0 to make waves ignore shallow water
        _RespectShallowWaterAttenuation( "Respect Shallow Water Attenuation", Range( 0, 1 ) ) = 1
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
            // #pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"

            #include "../../OceanGlobals.hlsl"
            #include "../../OceanInputsDriven.hlsl"
            #include "../../OceanHelpersNew.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 axis : TEXCOORD0;
                float invNormDistToShoreline : TEXCOORD1;
				float weight : TEXCOORD2;
	};

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 uv_slice : TEXCOORD1;
                float2 axis : TEXCOORD2;
                float3 worldPosScaled : TEXCOORD3;
                float2 invNormDistToShoreline_weight : TEXCOORD4;
            };

            Texture2DArray _WaveBuffer;

            CBUFFER_START(GerstnerPerMaterial)
            half _FeatherWaveStart;
            float _RespectShallowWaterAttenuation;
            CBUFFER_END

            CBUFFER_START(CrestPerOceanInput)
            int _WaveBufferSliceIndex;
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
                o.uv_slice = WorldToUV(worldPos.xz, _CrestCascadeData[_LD_SliceIndex], _LD_SliceIndex);

                // World pos prescaled by wave buffer size, suitable for using as UVs in fragment shader
                const float waveBufferSize = 0.5f * (1 << _WaveBufferSliceIndex);
                o.worldPosScaled = worldPos / waveBufferSize;

                o.invNormDistToShoreline_weight.x = v.invNormDistToShoreline;
				o.invNormDistToShoreline_weight.y = v.weight * _Weight;

                // Rotate forward axis around y-axis into world space
                o.axis = dot( v.axis, _AxisX ) * unity_ObjectToWorld._m00_m20 + dot( v.axis, float2(-_AxisX.y, _AxisX.x) ) * unity_ObjectToWorld._m02_m22;

                return o;
            }

            float4 frag(v2f input) : SV_Target
            {
                float wt = input.invNormDistToShoreline_weight.y;

                // Attenuate if depth is less than half of the average wavelength
                const half2 terrainHeight_seaLevelOffset = _LD_TexArray_SeaFloorDepth.SampleLevel(LODData_linear_clamp_sampler, input.uv_slice, 0.0).xy;
                const half depth = _OceanCenterPosWorld.y - terrainHeight_seaLevelOffset.x + terrainHeight_seaLevelOffset.y;
                const half depth_wt = saturate(2.0 * depth / _AverageWavelength);
                const float attenuationAmount = _AttenuationInShallows * _RespectShallowWaterAttenuation;
                wt *= attenuationAmount * depth_wt + (1.0 - attenuationAmount);

                // Feature at front/back
                wt *= min( input.invNormDistToShoreline_weight.x / _FeatherWaveStart, 1.0 );

                // Quantize wave direction and interpolate waves
                float axisHeading = atan2( input.axis.y, input.axis.x ) + 2.0 * 3.141592654;
                const float dTheta = 0.5*0.314159265;
                float angle0 = axisHeading;
                const float rem = fmod( angle0, dTheta );
                angle0 -= rem;
                const float angle1 = angle0 + dTheta;

                float2 axisX0; sincos( angle0, axisX0.y, axisX0.x );
                float2 axisX1; sincos( angle1, axisX1.y, axisX1.x );
                float2 axisZ0; axisZ0.x = -axisX0.y; axisZ0.y = axisX0.x;
                float2 axisZ1; axisZ1.x = -axisX1.y; axisZ1.y = axisX1.x;

                const float2 uv0 = float2(dot( input.worldPosScaled.xz, axisX0 ), dot( input.worldPosScaled.xz, axisZ0 ));
                const float2 uv1 = float2(dot( input.worldPosScaled.xz, axisX1 ), dot( input.worldPosScaled.xz, axisZ1 ));

                // Sample displacement, rotate into frame
                float4 disp_variance0 = _WaveBuffer.SampleLevel( sampler_Crest_linear_repeat, float3(uv0, _WaveBufferSliceIndex), 0 );
                float4 disp_variance1 = _WaveBuffer.SampleLevel( sampler_Crest_linear_repeat, float3(uv1, _WaveBufferSliceIndex), 0 );
                disp_variance0.xz = disp_variance0.x * axisX0 + disp_variance0.z * axisZ0;
                disp_variance1.xz = disp_variance1.x * axisX1 + disp_variance1.z * axisZ1;
                const float alpha = rem / dTheta;
                float4 disp_variance = lerp( disp_variance0, disp_variance1, alpha );

                // The large waves are added to the last two lods. Don't write cumulative variances for these - cumulative variance
                // for the last fitting wave cascade captures everything needed.
                const float minWavelength = _AverageWavelength / 1.5;
                if( minWavelength > _CrestCascadeData[_LD_SliceIndex]._maxWavelength )
                {
                    disp_variance.w = 0.0;
                }

                return wt * disp_variance;
            }
            ENDCG
        }
    }
}
