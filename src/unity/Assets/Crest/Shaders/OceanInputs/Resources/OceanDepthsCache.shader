// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

// Draw cached depths into current frame ocean depth data
Shader "Ocean/Inputs/Depth/Cached Depths"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			// Min blending to take the min of all depths. Similar in spirit to zbuffer'd visibility when viewing from top down.
			// To confuse matters further, ocean depth is now more like 'sea floor altitude' - a height above a deep water value,
			// so values are increasing in Y and we need to take the MAX of all depths.
			BlendOp Max

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			half frag (v2f i) : SV_Target
			{
				return tex2D(_MainTex, i.uv).x;
			}
			ENDCG
		}
	}
}
