// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Animated Waves/Spline Blending"
{
    Properties
    {
        // Controls ramp distance over which waves grow/fade as they move forwards
        _FeatherWaveStart("Feather wave start (0-1)", Range(0.0, 0.5)) = 0.1
    }

    SubShader
    {
        // Multiply
        Blend Zero SrcColor
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            // #pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"

            #include "../../OceanGlobals.hlsl"
            #include "../../OceanInputsDriven.hlsl"
            #include "../../OceanHelpersNew.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float invNormDistToShoreline : TEXCOORD1;
                float weight : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 invNormDistToShoreline_weight : TEXCOORD4;
            };

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
            half _MaximumAttenuationDepth;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                Varyings output;

                output.positionCS = UnityObjectToClipPos(input.positionOS.xyz);

                output.invNormDistToShoreline_weight.x = input.invNormDistToShoreline;
                output.invNormDistToShoreline_weight.y = input.weight * _Weight;

                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float weight = input.invNormDistToShoreline_weight.y;

                // Feather at front/back.
                weight *= min(input.invNormDistToShoreline_weight.x / _FeatherWaveStart, 1.0);

                return 1.0 - weight;
            }
            ENDCG
        }
    }
}
