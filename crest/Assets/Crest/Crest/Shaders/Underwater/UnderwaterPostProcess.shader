Shader "Crest/Underwater Post Process"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"


			float _HorizonHeight;
			float _HorizonRoll;

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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _MaskTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				int mask = tex2D(_MaskTex, i.uv);
				bool isUnderwater = mask == 2 || (i.uv.y < _HorizonHeight && mask != 1);
				if(isUnderwater)
				{
					col.rgb -= fixed3(0.5, 0.5, 0.5);
				}
				return col;
			}
			ENDCG
		}
	}
}
