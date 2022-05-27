// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Examples/Mask Depth"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "UnityCG.cginc"

            #include "../../../Crest/Shaders/Helpers/BIRP/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D_X(_CrestOceanMaskTexture);

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                ZERO_INITIALIZE(Varyings, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                return output;
            }

            fixed4 Fragment(Varyings input, out float i_depth : SV_Depth) : SV_Target
            {
                // We need this when sampling a screenspace texture.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float mask = LOAD_TEXTURE2D_X(_CrestOceanMaskTexture, input.positionCS.xy);
                i_depth = mask == 0.0 ? 1.0 : 0.0;
                return 0.0;
            }
            ENDCG
        }
    }
}
