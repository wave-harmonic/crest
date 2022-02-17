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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                return output;
            }

            float4 Fragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
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
