// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders the ocean horizon line into the mask.

Shader "Hidden/Crest/Underwater/Horizon"
{
	SubShader
	{
		Pass
		{
			// Quads will face towards the camera if given the camera's rotation.
			Cull Back
			// Could be enabled if needed.
			ZWrite Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			#include "../../OceanConstants.hlsl"
			#include "../../OceanGlobals.hlsl"

			struct Attributes
			{
				float3 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings Vert(Attributes input)
			{
				// This will work for all pipelines.
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.positionWS = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return (half4) input.positionWS.y > _OceanCenterPosWorld.y
					? UNDERWATER_MASK_ABOVE_SURFACE
					: UNDERWATER_MASK_BELOW_SURFACE;
			}
			ENDCG
		}
	}
}
