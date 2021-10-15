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

			// Clipping the ocean surface for underwater volumes.
			#pragma multi_compile_local _UNDERWATER_GEOMETRY_EFFECT_NONE _UNDERWATER_GEOMETRY_EFFECT_2D _UNDERWATER_GEOMETRY_EFFECT_VOLUME

			#include "UnityCG.cginc"

			#include "../../Helpers/BIRP/Core.hlsl"
			#include "../../Helpers/BIRP/InputsDriven.hlsl"

#if defined(_UNDERWATER_GEOMETRY_EFFECT_2D) || defined(_UNDERWATER_GEOMETRY_EFFECT_VOLUME)
			#define _UNDERWATER_GEOMETRY_EFFECT 1
#endif

#if _UNDERWATER_GEOMETRY_EFFECT
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestWaterBoundaryGeometryOuterTexture);
#if _UNDERWATER_GEOMETRY_EFFECT_VOLUME
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestWaterBoundaryGeometryInnerTexture);
#endif // _UNDERWATER_GEOMETRY_EFFECT_VOLUME
#endif // _UNDERWATER_GEOMETRY_EFFECT

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

			#pragma multi_compile_local _UNDERWATER_GEOMETRY_EFFECT_NONE _UNDERWATER_GEOMETRY_EFFECT_2D _UNDERWATER_GEOMETRY_EFFECT_VOLUME

			#include "UnityCG.cginc"

			#include "../../Helpers/BIRP/Core.hlsl"
			#include "../../Helpers/BIRP/InputsDriven.hlsl"
			#include "../../FullScreenTriangle.hlsl"

#if defined(_UNDERWATER_GEOMETRY_EFFECT_2D) || defined(_UNDERWATER_GEOMETRY_EFFECT_VOLUME)
			#define _UNDERWATER_GEOMETRY_EFFECT 1
#endif

#if _UNDERWATER_GEOMETRY_EFFECT_VOLUME
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CrestWaterBoundaryGeometryInnerTexture);
#endif // _UNDERWATER_GEOMETRY_EFFECT_VOLUME

			#include "../UnderwaterMaskHorizonShared.hlsl"
			ENDCG
		}
	}
}
