Shader "Lit/Shadows Work"
{
	Properties
	{
		[NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			Tags{ "LightMode" = "ForwardBase" } //"RenderType" = "Transparent" "Queue" = "Transparent" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			//#pragma multi_compile_fog

			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct v2f
			{
				float2 uv : TEXCOORD0;
				//fixed3 ambient : COLOR1;
				float4 pos : SV_POSITION;

				SHADOW_COORDS(6) // put shadows data into TEXCOORD1

			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;
				//half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				//half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				//o.diff = nl * _LightColor0.rgb;
				//o.ambient = ShadeSH9(half4(worldNormal,1));
				// compute shadows data
				TRANSFER_SHADOW(o)
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed shadow = SHADOW_ATTENUATION(i);
				return col * shadow;
			}
			ENDCG
		}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
