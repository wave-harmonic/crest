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

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture);

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            fixed4 Fragment(Varyings input, out float i_depth : SV_Depth) : SV_Target
            {
                float2 positionNDC = input.screenPos.xy / input.screenPos.w;
                float mask = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestOceanMaskTexture, positionNDC);
                i_depth = mask == 0.0 ? 1.0 : 0.0;
                return 0.0;
            }
            ENDCG
        }
    }
}
