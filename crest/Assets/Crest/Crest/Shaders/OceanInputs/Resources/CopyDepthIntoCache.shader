// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Takes the depth from a camera that has rendered the scene and copies it into a float render target
Shader "Crest/Copy Depth Buffer Into Cache"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            Name "CopyDepthBufferIntoCache"
            ZTest Always ZWrite Off Blend Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag

			float4 _HeightNearHeightFar;
			float4 _CustomZBufferParams;

			#include "UnityCG.cginc"
			#include "../../OceanLODData.hlsl"

			sampler2D _CamDepthBuffer;
			//SAMPLER(sampler_CamDepthBuffer);

			struct Attributes
			{
				float4 positionOS   : POSITION;
				float2 uv           : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS   : SV_POSITION;
				float2 uv           : TEXCOORD0;
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				output.uv = input.uv;
				output.positionCS = UnityObjectToClipPos(float4(input.positionOS.xyz, 1.0));
				return output;
			}

			float CustomLinear01Depth(float z)
			{
				z *= _CustomZBufferParams.x;
				return (1.0 - z) / _CustomZBufferParams.y;
			}

			float frag(Varyings input) : SV_Target
			{
				float deviceDepth = tex2D(_CamDepthBuffer, input.uv).r;
				float linear01Z = CustomLinear01Depth(deviceDepth);

				float altitude;
#if UNITY_REVERSED_Z
				altitude = lerp(_HeightNearHeightFar.y, _HeightNearHeightFar.x, linear01Z);
#else
				altitude = lerp(_HeightNearHeightFar.x, _HeightNearHeightFar.y, linear01Z);
#endif

				return altitude - _OceanCenterPosWorld.y;
			}

            ENDHLSL
        }
    }
}
