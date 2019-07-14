Shader "Crest/Underwater Post Process"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			#include "../OceanLODData.hlsl"
			#include "UnderwaterShared.hlsl"


			float _HorizonHeight;
			float _HorizonRoll;

			struct Attributes
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			Varyings Vert (Attributes v)
			{
				Varyings o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _MaskTex;

			fixed3 ApplyUnderwaterEffect(fixed3 prevCol)
			{
				// TODO(UPP): apply some kind of decent underwater effect
				return fixed3(prevCol.r, 0.0, 0.0);
			}

			fixed4 Frag (Varyings i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				bool isBelowHorizon;
				{
					// TODO(UPP): Create a cheap and accurate equation for
					// determining if we are below the horizon that can work
					// with any camera orientation
					isBelowHorizon = i.uv.y < _HorizonHeight;
				}

				bool isUnderwater;
				{
					int mask = tex2D(_MaskTex, i.uv);
					isUnderwater = mask == 2 || (isBelowHorizon && mask != 1);
				}

				if(isUnderwater)
				{
					col.rgb = ApplyUnderwaterEffect(col.rgb);
				}

				return col;
			}
			ENDCG
		}
	}
}
