// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Hidden/Crest/Underwater/Ocean Mask"
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

			#pragma multi_compile_local __ _UNDERWATER_GEOMETRY_EFFECT

			#include "UnityCG.cginc"

			#include "../UnderwaterMaskShared.hlsl"
			ENDCG
		}
	}
}
