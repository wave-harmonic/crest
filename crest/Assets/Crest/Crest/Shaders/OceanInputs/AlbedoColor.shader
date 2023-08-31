// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Inputs/Albedo/Color"
{
    Properties
    {
        _Texture("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSource("Source Blend Mode", Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeTarget("Target Blend Mode", Int) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Blend [_BlendModeSource] [_BlendModeTarget]

            ZWrite Off

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            UNITY_DECLARE_TEX2D(_Texture);
            float4 _Texture_ST;

            half4 _Color;
            half _Cutoff;

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _Texture);
                output.color = input.color;
                return output;
            }

            fixed4 Fragment(Varyings i) : SV_Target
            {
                fixed4 color = UNITY_SAMPLE_TEX2D(_Texture, i.uv) * _Color;
                clip(color.a - _Cutoff + 0.0001);
                return color * i.color;
            }
            ENDCG
        }
    }
}
