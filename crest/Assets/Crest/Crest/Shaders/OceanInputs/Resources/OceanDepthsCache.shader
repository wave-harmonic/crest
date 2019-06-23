// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)


// Draw cached depths into current frame ocean depth data
Shader "Crest/Inputs/Depth/Cached Depths Geometry"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			// Min blending to take the min of all depths. Similar in spirit to zbuffer'd visibility when viewing from top down.
			// To confuse matters further, ocean depth is now more like 'sea floor altitude' - a height above a deep water value,
			// so values are increasing in Y and we need to take the MAX of all depths.
			BlendOp Min

			CGPROGRAM
			#pragma multi_compile_local __ _ENABLE_GEOMETRY_SHADER
			#pragma vertex Vert
			#pragma fragment Frag

			#define SlicedVaryings Varyings
			#include "OceanDepthsCacheCommon.hlsl"

			float4 ObjectToPosition(float3 positionOS)
			{
				return UnityObjectToClipPos(positionOS);
			}
			ENDCG
		}
	}
}
