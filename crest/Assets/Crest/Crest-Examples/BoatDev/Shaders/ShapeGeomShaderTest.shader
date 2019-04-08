// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Custom/Geometry/Pyramids"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Factor ("Factor", Range(0., 0.5)) = 0.2
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom

			#include "UnityCG.cginc"

			struct v2g
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct g2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 col : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2g vert (appdata_base v)
			{
				v2g o;
				o.vertex = v.vertex;
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.normal = v.normal;
				return o;
			}

			float _Factor;

			[maxvertexcount(12)]
			void geom(triangle v2g IN[3], inout TriangleStream<g2f> tristream)
			{
				g2f o;

				float3 edgeA = IN[1].vertex - IN[0].vertex;
				float3 edgeB = IN[2].vertex - IN[0].vertex;
				float3 normalFace = normalize(cross(edgeA, edgeB));

				float3 centerPos = (IN[0].vertex + IN[1].vertex + IN[2].vertex) / 3;
				float2 centerTex = (IN[0].uv + IN[1].uv + IN[2].uv) / 3;
				centerPos += float4(normalFace, 0) * _Factor;

				for(int i = 0; i < 3; i++)
				{
					o.pos = UnityObjectToClipPos(IN[i].vertex);
					o.uv = IN[i].uv;
					o.col = fixed4(0., 0., 0., 1.);
					tristream.Append(o);

					int inext = (i+1)%3;
					o.pos = UnityObjectToClipPos(IN[inext].vertex);
					o.uv = IN[inext].uv;
					o.col = fixed4(0., 0., 0., 1.);
					tristream.Append(o);

					o.pos = UnityObjectToClipPos(float4(centerPos, 1));
					o.uv = centerTex;
					o.col = fixed4(1.0, 1.0, 1.0, 1.);
					tristream.Append(o);

					tristream.RestartStrip();
				}

				o.pos = UnityObjectToClipPos(IN[0].vertex);
				o.uv = IN[0].uv;
				o.col = fixed4(0., 0., 0., 1.);
				tristream.Append(o);

				o.pos = UnityObjectToClipPos(IN[1].vertex);
				o.uv = IN[1].uv;
				o.col = fixed4(0., 0., 0., 1.);
				tristream.Append(o);

				o.pos = UnityObjectToClipPos(IN[2].vertex);
				o.uv = IN[2].uv;
				o.col = fixed4(0., 0., 0., 1.);
				tristream.Append(o);

				tristream.RestartStrip();
			}
			
			fixed4 frag (g2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * i.col;
				return col;
			}
			ENDCG
		}
	}
}