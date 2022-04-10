// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Examples/Mask Fill"
{
    SubShader
    {
        Pass
        {
            Name "Mask Fill"
            Cull Off

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "UnityCG.cginc"

            #include "../../../../Crest/Shaders/Helpers/BIRP/Core.hlsl"

            TEXTURE2D_X(_CrestWaterVolumeFrontFaceTexture);

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

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                ZERO_INITIALIZE(Varyings, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = UnityObjectToClipPos(input.positionOS);
                return output;
            }

            float4 Fragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                // We need this when sampling a screenspace texture.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                if (LOAD_TEXTURE2D_X(_CrestWaterVolumeFrontFaceTexture, input.positionCS.xy).r < input.positionCS.z)
                {
                    discard;
                }

                return isFrontFace ? 0.0 : 1.0;
            }
            ENDCG
        }
    }
}
