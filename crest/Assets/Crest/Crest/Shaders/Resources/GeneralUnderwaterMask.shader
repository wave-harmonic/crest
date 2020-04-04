// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)


Shader "Crest/Underwater/General Underwater Mask"
{
	SubShader
	{
		Pass
		{
			// We always disable culling when rendering ocean mask, as we only
			// use it for underwater rendering features.
			Cull Off
			CGPROGRAM

			#pragma vertex Vert
			#pragma fragment Frag
			// for VFACE
			#pragma target 3.0

			#include "UnityCG.cginc"
			#include "../OceanConstants.hlsl"

			struct Attributes
			{
				float4 vertex : POSITION;
			};

			struct Varyings
			{
				float4 vertex : SV_POSITION;
			};

			Varyings Vert (Attributes input)
			{
				Varyings output;
				output.vertex = UnityObjectToClipPos(input.vertex);
				return output;
			}

			fixed4 Frag (const Varyings input, const float facing : VFACE) : SV_Target
			{
				if(facing < 0.0)
				{
					return (half4)UNDERWATER_MASK_WATER_SURFACE_BELOW;
				}
				else
				{
					return (half4)UNDERWATER_MASK_WATER_SURFACE_ABOVE;
				}
			}
			ENDCG
		}
	}
}
