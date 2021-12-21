// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Provides utility passes for rendering like clearing the stencil buffer.

Shader "Hidden/Crest/Helpers/Utility"
{
	HLSLINCLUDE
	#pragma vertex Vertex
	#pragma fragment Fragment

	#include "UnityCG.cginc"

	#include "../BIRP/Core.hlsl"
	#include "../BIRP/InputsDriven.hlsl"

	#include "../../OceanShaderHelpers.hlsl"

	struct Attributes
	{
		float4 positionOS : POSITION;
	};

	struct Varyings
	{
		float4 positionCS : SV_POSITION;
	};

	TEXTURE2D_X(_CameraDepthTexture);

	Varyings Vertex(Attributes input)
	{
		Varyings output;
		output.positionCS = UnityObjectToClipPos(input.positionOS);
		return output;
	}
	ENDHLSL

	SubShader
	{
		Cull Off ZWrite On ZTest Always

		Pass
		{
			// Copies the depth from the camera depth texture. Clears the stencil for convenience.
			Name "Copy Depth / Clear Stencil"

			ZWrite On
			ZTest Always
			Cull Off

			Stencil
			{
				Ref 0
				Comp Always
				Pass Replace
			}

			HLSLPROGRAM
			float Fragment(Varyings input) : SV_Depth
			{
				return LOAD_DEPTH_TEXTURE_X(_CameraDepthTexture, input.positionCS.xy);
			}
			ENDHLSL
		}

		Pass
		{
			// Clears the depth buffer without clearing the stencil.
			Name "Clear Depth"

			ZWrite On
			ZTest Always
			Cull Off

			HLSLPROGRAM
			float Fragment(Varyings input) : SV_Depth
			{
				return 0.0;
			}
			ENDHLSL
		}

		Pass
		{
			// Clears the stencil buffer without clearing depth
			Name "Clear Stencil"

			ZWrite Off
			ZTest Always
			Cull Off

			Stencil
			{
				Ref 0
				Comp Always
				Pass Replace
			}
		}
	}
}
