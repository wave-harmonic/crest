// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Renders alpha geometry overlaid on ocean surface. Samples the ocean shape texture in the vertex shader to track
// the surface. Requires the right texture to be assigned (see RenderAlphaOnSurface script).
Shader "Ocean/Ocean Surface Alpha"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Alpha("Alpha Multiplier", Range(0.0, 1.0)) = 1.0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc("Src Blend Mode", Int) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendModeTgt("Tgt Blend Mode", Int) = 10
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100

		Pass
		{
			Blend [_BlendModeSrc] [_BlendModeTgt]

			ZWrite Off
			// Depth offset to stop intersection with water. "Factor" and "Units". typical seems to be (-1,-1). (-0.5,0) gives
			// pretty good results for me when alpha geometry is fairly well matched but fails when alpha geo is too low res.
			// the ludicrously large value below seems to work in most of my tests.
			Offset 0, -1000000

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "../../Crest/Shaders/OceanLODData.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			half _Alpha;

			// MeshScaleLerp, FarNormalsWeight, LODIndex (debug), unused
			uniform float4 _InstanceData;

			v2f vert (appdata v)
			{
				v2f o;

				// move to world
				float3 worldPos;
				worldPos.xz = mul(unity_ObjectToWorld, v.vertex).xz;
				worldPos.y = 0.;

				// vertex snapping and lod transition
				float lodAlpha = ComputeLodAlpha(worldPos, _InstanceData.x);

				// sample shape textures - always lerp between 2 scales, so sample two textures

				// sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
				float wt_0 = (1. - lodAlpha) * _LD_Params_0.z;
				float wt_1 = (1. - wt_0) * _LD_Params_1.z;
				// sample displacement textures, add results to current world pos / normal / foam
				const float2 wxz = worldPos.xz;
				half foam = 0.;
				SampleDisplacements(_LD_Sampler_AnimatedWaves_0, LD_0_WorldToUV(wxz), wt_0, worldPos);
				SampleDisplacements(_LD_Sampler_AnimatedWaves_1, LD_1_WorldToUV(wxz), wt_1, worldPos);

				// move to sea level
				worldPos.y += _OceanCenterPosWorld.y;

				// view-projection
				o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.));

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);

				UNITY_APPLY_FOG(i.fogCoord, col);

				col.a *= _Alpha;

				return col;
			}
			ENDCG
		}
	}
}
