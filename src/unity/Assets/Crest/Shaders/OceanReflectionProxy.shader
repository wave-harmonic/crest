﻿// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Ocean Reflection Proxy"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			#include "../../Crest/Shaders/OceanLODData.cginc"
			
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

			// MeshScaleLerp, FarNormalsWeight, LODIndex (debug), unused
			uniform float4 _InstanceData;

			v2f vert (appdata v)
			{
				v2f o;
				// move to world
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 worldPosOrig = worldPos;
				worldPos.y = 0.;

				// vertex snapping and lod transition
				float lodAlpha = ComputeLodAlpha(worldPos, _InstanceData.x);

				// sample shape textures - always lerp between 2 scales, so sample two textures
				half3 n = half3(0., 1., 0.);
				// sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
				float wt_0 = (1. - lodAlpha) * _LD_Params_0.z;
				float wt_1 = (1. - wt_0) * _LD_Params_1.z;
				// sample displacement textures, add results to current world pos / normal / foam
				const float2 wxz = worldPos.xz;
				half foam = 0.;
				SampleDisplacements(_LD_Sampler_AnimatedWaves_0, LD_0_WorldToUV(wxz), wt_0, _LD_Params_0.w, _LD_Params_0.x, worldPos, n);
				SampleDisplacements(_LD_Sampler_AnimatedWaves_1, LD_1_WorldToUV(wxz), wt_1, _LD_Params_1.w, _LD_Params_1.x, worldPos, n);

				// want to clip object with clip plane at sea level. if waves positive, push object down into clip.
				worldPosOrig.y -= worldPos.y;

				o.vertex = mul(UNITY_MATRIX_VP, float4(worldPosOrig, 1.));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);

				UNITY_APPLY_FOG(i.fogCoord, col);

				col.xy = i.uv.xy;
				col.b = 0.;

				return col;
			}
			ENDCG
		}
	}
}
