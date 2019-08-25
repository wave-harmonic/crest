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
			#pragma vertex Vert
			#pragma geometry Geometry
			#pragma fragment Frag

			#define CREST_OCEAN_DEPTHS_GEOM_SHADER_ON

			#include "../../OceanConstants.hlsl"
			#include "../../OceanLODData.hlsl"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			int _CurrentLodCount;
			float4x4 _SliceViewProjMatrices[MAX_LOD_COUNT];

			#include "UnityCG.cginc"

			#include "OceanDepthsCacheCommon.hlsl"
			ENDCG
		}
	}
}
