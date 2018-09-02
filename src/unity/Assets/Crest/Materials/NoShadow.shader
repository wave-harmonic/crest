Shader "Unlit/No Shadows"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags {  "LightMode" = "ForwardBase" }
		//Blend One One

		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
#include "AutoLight.cginc"
		UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);

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
				//float4 grabPos : TEXCOORD3;
				//unityShadowCoord4 _ShadowCoord : TEXCOORD6;
				SHADOW_COORDS(6)

			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				
				//o._ShadowCoord = mul(unity_WorldToShadow[0], mul(unity_ObjectToWorld, v.vertex));
				TRANSFER_SHADOW(o)

				//o.grabPos = ComputeGrabScreenPos(o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col;
			/*= tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				col *= .2;*/

				//col = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, i._ShadowCoord) / 2.;

				col = SHADOW_ATTENUATION(i);

				//col = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, float3(i.grabPos.xy/i.grabPos.w, 0.15))/2.;

				return col;
			}
			ENDCG
		}
	}
}
