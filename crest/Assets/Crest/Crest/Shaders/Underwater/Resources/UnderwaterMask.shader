// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Underwater/Ocean Mask"
{
	SubShader
	{
		Pass
		{
			Name "Ocean Surface Mask"
			// We always disable culling when rendering ocean mask, as we only
			// use it for underwater rendering features.
			Cull Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			// for VFACE
			#pragma target 3.0

			#include "UnityCG.cginc"

			#include "../../Helpers/BIRP/Core.hlsl"
			#include "../../Helpers/BIRP/InputsDriven.hlsl"

			#include "../UnderwaterMaskShared.hlsl"
			ENDCG
		}

		Pass
		{
			Name "Ocean Horizon Mask"
			Cull Off
			ZWrite Off
			// Horizon must be rendered first or it will overwrite the mask with incorrect values. ZTest not needed.
			ZTest Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			#include "../../Helpers/BIRP/Core.hlsl"
			#include "../../Helpers/BIRP/InputsDriven.hlsl"
			#include "../../FullScreenTriangle.hlsl"

			#include "../UnderwaterMaskHorizonShared.hlsl"
			ENDCG
		}
	}
}
