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

	#include "../../FullScreenTriangle.hlsl"

	#include "../../OceanShaderHelpers.hlsl"

	struct Attributes
	{
		uint id : SV_VertexID;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct Varyings
	{
		float4 positionCS : SV_POSITION;
		UNITY_VERTEX_OUTPUT_STEREO
	};

	Varyings Vertex(Attributes input)
	{
		// This will work for all pipelines.
		Varyings output = (Varyings)0;
		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

		output.positionCS = GetFullScreenTriangleVertexPosition(input.id);
		return output;
	}
	ENDHLSL

	SubShader
	{
		Cull Off ZWrite On ZTest Always

		Pass
		{
			// Copies the color texture.
			Name "Copy Color"

			ZWrite Off
			ZTest Always
			Cull Off

			HLSLPROGRAM
			TEXTURE2D_X(_CameraColorTexture);

			float4 Fragment(Varyings input) : SV_Target
			{
				return LOAD_TEXTURE2D_X(_CameraColorTexture, input.positionCS.xy);
			}
			ENDHLSL
		}

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
			TEXTURE2D_X(_CameraDepthTexture);
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
			Blend Zero One

			Stencil
			{
				Ref 0
				Comp Always
				Pass Replace
			}

			HLSLPROGRAM
			float Fragment(Varyings input) : SV_Target
			{
				return 0.0;
			}
			ENDHLSL
		}
	}
}
