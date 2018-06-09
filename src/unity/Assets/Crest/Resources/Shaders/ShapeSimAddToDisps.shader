// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Ocean/Shape/Sim/Add To Disps"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Blend One One
		Cull Off Lighting Off ZWrite Off
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 screenPos : TEXCOORD0;
			};

			sampler2D _MainTex;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				// sim texture and displacement texture are assumed to be exactly aligned, so just copy texture to texture.
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				half4 simData = tex2D(_MainTex, i.screenPos);
				return half4(0., simData.x, 0., 0.);
			}
			ENDCG
		}
	}
}
