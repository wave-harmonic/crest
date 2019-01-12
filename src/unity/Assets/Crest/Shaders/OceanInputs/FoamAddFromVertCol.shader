// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Inputs/Foam/Add From Vert Colours"
{
	Properties
	{
		_Strength("Strength", float) = 1
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" }
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 col : COLOR0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 col : COLOR0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.col = v.col;
				return o;
			}
			
			uniform float _Strength;

			half4 frag (v2f i) : SV_Target
			{
				return _Strength * i.col.x;
			}
			ENDCG
		}
	}
}
