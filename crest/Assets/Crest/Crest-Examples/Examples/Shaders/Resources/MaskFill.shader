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

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestWaterVolumeFrontFaceTexture)

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            float4 Fragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float2 positionNDC = input.screenPos.xy / input.screenPos.w;
                float deviceZ = input.screenPos.z / input.screenPos.w;

                float frontFaceZ = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CrestWaterVolumeFrontFaceTexture, positionNDC);
                if (frontFaceZ == 0.0 || frontFaceZ < deviceZ)
                {
                    discard;
                }

                return isFrontFace ? 0.0 : 1.0;
            }
            ENDCG
        }
    }
}
