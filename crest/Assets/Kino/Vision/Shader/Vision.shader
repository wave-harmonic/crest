// KinoVision - Frame visualization utility
// https://github.com/keijiro/KinoVision

Shader "Hidden/Kino/Vision"
{
    Properties
    {
        _MainTex("", 2D) = ""{}
    }
    Subshader
    {
        // Depth with camera depth texture
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #define USE_CAMERA_DEPTH
            #include "Depth.cginc"
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma vertex CommonVertex
            #pragma fragment DepthFragment
            #pragma target 3.0
            ENDCG
        }
        // Depth with camera depth normals texture
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #define USE_CAMERA_DEPTH_NORMALS
            #include "Depth.cginc"
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma vertex CommonVertex
            #pragma fragment DepthFragment
            #pragma target 3.0
            ENDCG
        }
        // Depth with camera depth normals texture
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #define USE_CAMERA_DEPTH_NORMALS
            #include "Normals.cginc"
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma vertex CommonVertex
            #pragma fragment NormalsFragment
            #pragma target 3.0
            ENDCG
        }
        // Depth with G buffer
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #define USE_GBUFFER
            #include "Normals.cginc"
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma vertex CommonVertex
            #pragma fragment NormalsFragment
            #pragma target 3.0
            ENDCG
        }
        // Motion vectors overlay
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #include "MotionVectors.cginc"
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma vertex CommonVertex
            #pragma fragment OverlayFragment
            #pragma target 3.0
            ENDCG
        }
        // Motion vector arrows
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #include "MotionVectors.cginc"
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma vertex ArrowVertex
            #pragma fragment ArrowFragment
            #pragma target 3.0
            ENDCG
        }
    }
}
