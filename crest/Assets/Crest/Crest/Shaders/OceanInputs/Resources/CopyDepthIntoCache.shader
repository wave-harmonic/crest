// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Copies the depth buffer into the cache as object-space height. Object-space is used instead of world-space to allow
// relative movement of baked depth caches afterwards. It is converted to world-space in another shader before writing
// into the LOD data.

Shader "Crest/Copy Depth Buffer Into Cache"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" }

		Pass
		{
			Name "CopyDepthBufferIntoCache"
			ZTest Always ZWrite Off Blend Off

			CGPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			#include "../../OceanGlobals.hlsl"

			CBUFFER_START(CrestPerOceanInput)
			float4 _HeightNearHeightFar;
			float4 _CustomZBufferParams;
			CBUFFER_END

			Texture2D _CamDepthBuffer;
			SamplerState sampler_CamDepthBuffer;
			float _HeightOffset;

			struct Attributes
			{
				float3 positionOS   : POSITION;
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
				output.positionCS = UnityObjectToClipPos(input.positionOS);
				return output;
			}

			float CustomLinear01Depth(float z)
			{
				z *= _CustomZBufferParams.x;
				return (1.0 - z) / _CustomZBufferParams.y;
			}

			float4 frag(Varyings input) : SV_Target
			{
				float deviceDepth = _CamDepthBuffer.Sample(sampler_CamDepthBuffer, input.uv).x;
				float linear01Z = CustomLinear01Depth(deviceDepth);

				float altitude;
#if UNITY_REVERSED_Z
				altitude = lerp(_HeightNearHeightFar.y, _HeightNearHeightFar.x, linear01Z);
#else
				altitude = lerp(_HeightNearHeightFar.x, _HeightNearHeightFar.y, linear01Z);
#endif

				return float4(altitude - _HeightOffset, 0.0, 0.0, 1.0);
			}

			ENDCG
		}
	}
}
