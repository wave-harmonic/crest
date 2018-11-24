// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Inputs/Foam/Add From Texture"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Strength( "Strength", float ) = 1
	}

	SubShader
	{
		// base simulation runs on the Geometry queue, before this shader.
		// this shader adds interaction forces on top of the simulation result.
		Tags { "Queue" = "Transparent" }
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform float _Radius;

			v2f vert( appdata_t v )
			{
				v2f o;
				o.vertex = UnityObjectToClipPos( v.vertex );
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			uniform float _Amplitude;

			float4 frag (v2f i) : SV_Target
			{
				return tex2D(_MainTex, i.uv);
			}

			ENDCG
		}
	}
}
