// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Water Volume Geometry"
{
	SubShader
	{
		CGINCLUDE
		#pragma vertex Vert
		#pragma fragment Frag

		// #pragma enable_d3d11_debug_symbols

		#include "UnityCG.cginc"

		struct Attributes
		{
			float3 positionOS : POSITION;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct Varyings
		{
			float4 positionCS : SV_POSITION;
			UNITY_VERTEX_OUTPUT_STEREO
		};

		Varyings Vert(Attributes input)
		{
			// This will work for all pipelines.
			Varyings o = (Varyings)0;
			UNITY_SETUP_INSTANCE_ID(input);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			o.positionCS = UnityObjectToClipPos(input.positionOS);

			return o;
		}

		half4 Frag(Varyings input) : SV_Target
		{
			return 1.0;
		}
		ENDCG

		Pass
		{
			Name "Front Faces"
			Cull Back

			Stencil
			{
				// Must match k_StencilValueVolume in:
				// Scripts/Underwater/UnderwaterRenderer.Mask.cs
				Ref 5
				Pass Replace
			}

			CGPROGRAM
			ENDCG
		}

		Pass
		{
			Name "Back Faces"
			Cull Front

			Stencil
			{
				// Must match k_StencilValueVolume in:
				// Scripts/Underwater/UnderwaterRenderer.Mask.cs
				Ref 5
				Pass Replace
			}

			CGPROGRAM
			ENDCG
		}
	}
}
